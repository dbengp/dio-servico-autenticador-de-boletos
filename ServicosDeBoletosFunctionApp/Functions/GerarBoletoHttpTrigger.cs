using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using ServicosDeBoletos.Models; 
using ServicosDeBoletos.Infra.ServiceBus; 
using System.ComponentModel.DataAnnotations; 

namespace ServicosDeBoletos.Functions
{
    public class GerarBoletoHttpTrigger
    {
        private readonly ILogger<GerarBoletoHttpTrigger> _logger;
        private readonly IServiceBusQueueService _serviceBusQueueService;

        // Injeção de dependência para o logger e nosso serviço de fila
        public GerarBoletoHttpTrigger(ILogger<GerarBoletoHttpTrigger> logger, IServiceBusQueueService serviceBusQueueService)
        {
            _logger = logger;
            _serviceBusQueueService = serviceBusQueueService;
        }

        [Function("GerarBoleto")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "boletos")] HttpRequestData req)
        {
            _logger.LogInformation("Requisição HTTP recebida para geração de boleto.");

            // 1. Deserializar o corpo da requisição
            BoletoRequest? boletoRequest;
            try
            {
                boletoRequest = await JsonSerializer.DeserializeAsync<BoletoRequest>(req.Body);
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Erro ao desserializar o JSON: {ex.Message}");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Corpo da requisição JSON inválido.");
                return badRequestResponse;
            }

            if (boletoRequest == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Corpo da requisição vazio ou inválido.");
                return badRequestResponse;
            }

            // 2. Validar o modelo (data annotations)
            var validationContext = new ValidationContext(boletoRequest, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(boletoRequest, validationContext, validationResults, true))
            {
                var errors = string.Join(" | ", validationResults.Select(v => v.ErrorMessage));
                _logger.LogWarning($"Dados de requisição inválidos: {errors}");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync($"Erro de validação: {errors}");
                return badRequestResponse;
            }

            // 3. Simular o envio para uma fila do Service Bus para processamento assíncrono
            // Crie um objeto que represente os dados para a fila
            var dadosParaGeracao = new
            {
                boletoRequest.CpfCnpjSacado,
                boletoRequest.NomeSacado,
                boletoRequest.Valor,
                boletoRequest.DataVencimento,
                boletoRequest.DescricaoServico,
                TimestampSolicitacao = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString() // Para rastreamento
            };

            const string nomeFilaBoletos = "fila-solicitacoes-boletos"; // Nome da fila do Service Bus
            try
            {
                await _serviceBusQueueService.SendMessageAsync(nomeFilaBoletos, dadosParaGeracao);
                _logger.LogInformation($"Mensagem enviada para a fila '{nomeFilaBoletos}' com CorrelationId: {dadosParaGeracao.CorrelationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar mensagem para o Service Bus: {ex.Message}");
                var internalErrorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await internalErrorResponse.WriteStringAsync("Ocorreu um erro interno ao processar a solicitação.");
                return internalErrorResponse;
            }

            // 4. Retornar uma resposta de sucesso aceita
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteStringAsync("Solicitação de geração de boleto recebida e encaminhada para processamento.");
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            return response;
        }
    }
}