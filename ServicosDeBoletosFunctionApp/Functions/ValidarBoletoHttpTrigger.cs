using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using ServicosDeBoletos.Models; 
using ServicosDeBoletos.Infra.ServiceBus; 
using System.ComponentModel.DataAnnotations; 
using System.Linq; 

namespace ServicosDeBoletos.Functions
{
    public class ValidarBoletoHttpTrigger
    {
        private readonly ILogger<ValidarBoletoHttpTrigger> _logger;
        private readonly IServiceBusQueueService _serviceBusQueueService;

        // Injeção de dependência para o logger e nosso serviço de fila
        public ValidarBoletoHttpTrigger(ILogger<ValidarBoletoHttpTrigger> logger, IServiceBusQueueService serviceBusQueueService)
        {
            _logger = logger;
            _serviceBusQueueService = serviceBusQueueService;
        }

        [Function("ValidarBoleto")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "validar-boletos")] HttpRequestData req)
        {
            _logger.LogInformation("Requisição HTTP recebida para validação de boleto.");

            // 1. Deserializar o corpo da requisição
            BoletoValidationRequest? validationRequest;
            try
            {
                validationRequest = await JsonSerializer.DeserializeAsync<BoletoValidationRequest>(req.Body);
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Erro ao desserializar o JSON: {ex.Message}");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Corpo da requisição JSON inválido.");
                return badRequestResponse;
            }

            if (validationRequest == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Corpo da requisição vazio ou inválido.");
                return badRequestResponse;
            }

            // 2. Validar o modelo (data annotations)
            var validationContext = new ValidationContext(validationRequest, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(validationRequest, validationContext, validationResults, true))
            {
                var errors = string.Join(" | ", validationResults.Select(v => v.ErrorMessage));
                _logger.LogWarning($"Dados de requisição inválidos para validação: {errors}");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync($"Erro de validação: {errors}");
                return badRequestResponse;
            }

            // 3. Simular a lógica de validação (simplificada)
            bool isValid = true;
            string validationMessage = "Boleto validado com sucesso (validação simulada).";

            // Exemplo de validação adicional (além das Data Annotations)
            // Em um cenário real, você decodificaria a linha digitável e verificaria o DV, valor, vencimento, etc.
            if (validationRequest.LinhaDigitavel.Length != 47 && validationRequest.LinhaDigitavel.Length != 48)
            {
                isValid = false;
                validationMessage = "A linha digitável deve ter 47 ou 48 caracteres.";
            }
            else if (!validationRequest.LinhaDigitavel.All(char.IsDigit))
            {
                isValid = false;
                validationMessage = "A linha digitável deve conter apenas números.";
            }
            // Adicione aqui lógica de validação de DV (Dígito Verificador) real, se fosse o caso.
            // Ex: if (!BoletoValidator.IsValidDv(validationRequest.LinhaDigitavel)) { isValid = false; validationMessage = "DV inválido."; }

            // 4. Preparar o resultado da validação para envio à fila
            var validationResultData = new
            {
                validationRequest.LinhaDigitavel,
                IsValid = isValid,
                ValidationMessage = validationMessage,
                TimestampValidacao = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString() // Para rastreamento
            };

            const string nomeFilaValidacoes = "fila-boletos-validados"; // Nova fila para resultados de validação
            try
            {
                await _serviceBusQueueService.SendMessageAsync(nomeFilaValidacoes, validationResultData);
                _logger.LogInformation($"Resultado de validação enviado para a fila '{nomeFilaValidacoes}' com CorrelationId: {validationResultData.CorrelationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar resultado de validação para o Service Bus: {ex.Message}");
                var internalErrorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await internalErrorResponse.WriteStringAsync("Ocorreu um erro interno ao processar a solicitação de validação.");
                return internalErrorResponse;
            }

            // 5. Retornar a resposta HTTP com base no resultado da validação
            if (isValid)
            {
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                await okResponse.WriteStringAsync(validationMessage);
                okResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return okResponse;
            }
            else
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync(validationMessage);
                badRequestResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return badRequestResponse;
            }
        }
    }
}