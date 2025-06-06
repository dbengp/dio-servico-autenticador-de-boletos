# dio-servico-autenticador-de-boletos

## projeto de demonstração de um Serviço Autenticador de Boletos com Function e Service Bus

### Os conceitos e resumo foram retirados da leitura das documentações oficiais:
- <https://learn.microsoft.com/pt-br/azure/azure-functions/>
- <https://learn.microsoft.com/pt-br/azure/azure-functions/functions-triggers-bindings?tabs=isolated-process%2Cnode-v4%2Cpython-v2&pivots=programming-language-csharp>
- <https://learn.microsoft.com/pt-br/azure/service-bus-messaging/service-bus-messaging-overview>
- <https://learn.microsoft.com/pt-br/azure/service-bus-messaging/service-bus-queues-topics-subscriptions>

### Functions no Function App do Azure
- O Azure Functions é uma plataforma como serviço (PaaS) que oferece recursos de computação escaláveis e serverless para projetos de código em diversas linguagens, são a base do modelo de computação serverless da plataforma. As funções são unidades de código que podem ser usadas para criar APIs web, responder a alterações em bancos de dados, processar fluxos de IoT e gerenciar filas de mensagens.Os bindings são opcionais, mas uma função pode ter múltiplos bindings de entrada e/ou saída. O Azure Functions suporta uma ampla gama de tipos de bindings para diversos serviços, incluindo Armazenamento de Blobs, Azure Cosmos DB, Azure Data Explorer, Azure SQL, Event Grid, Hubs de Eventos, HTTP, Hub IoT, Armazenamento de Filas e muitos outros. Para funções de biblioteca de classes C#, os triggers e bindings são configurados através de atributos C#, que podem variar dependendo do modelo de tempo de execução C#, incluindo o modelo de worker isolado.
- Triggers e Bindings: as funções do Azure utilizam triggers e bindings para se integrar com outros serviços e reagir a eventos:
  * Triggers: São os elementos que iniciam a execução de uma função. Toda função deve ter exatamente um trigger, que também pode fornecer dados para a função. Exemplos incluem triggers HTTP, que respondem a solicitações web, ou triggers de armazenamento de Blob, que reagem a arquivos adicionados ou modificados no armazenamento.
  * Bindings: Oferecem uma forma declarativa de conectar funções a outros recursos. Eles simplificam a entrada e saída de dados da função, eliminando a necessidade de escrever código complexo para acessar esses recursos. Existem dois tipos principais de bindings:
    * Bindings de Entrada: Permitem que a função receba dados de outros serviços. Por exemplo, um binding de entrada pode ler o conteúdo de um arquivo em um armazenamento de Blob ou recuperar um documento de um banco de dados Azure Cosmos DB.
    * Bindings de Saída: Permitem que a função escreva dados para outros serviços. Por exemplo, um binding de saída pode salvar dados em uma fila de armazenamento, enviar uma mensagem para um Event Hub ou gravar um documento em um banco de dados.

### O Barramento de Serviço (Service Bus) do Azure
- O Azure Service Bus é uma solução robusta e gerenciada para a troca de mensagens em ambientes empresariais, fornecendo ferramentas essenciais para a construção de arquiteturas de microserviços, integração de sistemas e processamento assíncrono, tudo com alta fiabilidade e escalabilidade. Ele é um intermediário de mensagens empresarial totalmente gerido, que oferece filas de mensagens e tópicos de publicação/subscrição. Ele facilita a transferência de dados entre aplicações e serviços usando mensagens, que são contentores com metadados e dados em formatos como JSON ou XML.
- Principais Conceitos e Funcionalidades:
  * Entidades de Mensagens:
    * Filas: Proporcionam entrega de mensagens FIFO (first-in, first-out) para um ou mais consumidores concorrentes. Os recetores processam as mensagens na ordem em que foram adicionadas à fila, com apenas um consumidor a processar cada mensagem. As filas são ideais para desacoplamento temporal de componentes de aplicação e nivelamento de carga.
    * Tópicos e Subscrições: Permitem a comunicação um-para-muitos (publish-subscribe). Cada mensagem publicada num tópico fica disponível para cada subscrição registada. Os subscritores podem usar regras de subscrição (condições de filtro e ações opcionais) para definir quais mensagens desejam receber.
- Benefícios do Azure Service Bus:
  * Equilíbrio de Carga: Ajuda a distribuir cargas de trabalho entre funções de trabalho concorrentes.
  * Encaminhamento Seguro de Dados: Roteia e transfere dados de forma segura entre serviços e aplicações.
  * Coordenação Transacional: Suporta trabalho transacional que exige alta fiabilidade.
  * Desacoplamento de Aplicações: Melhora a fiabilidade e escalabilidade das aplicações ao desacoplá-las, permitindo que funcionem de forma independente.
- Casos de Uso Comuns:
  * Transferência de dados empresariais (por exemplo, ordens de compra, inventários).
  * Melhoria da fiabilidade e escalabilidade de aplicações.
  * Balanceamento de carga.
- Recursos Adicionais:
  * Sessões de Mensagens: Permitem o processamento FIFO de mensagens relacionadas.
  * Encaminhamento Automático: Capacidade de encadear filas.
  * Filas de Mensagens Mortas (Dead-letter Queues): Armazenam mensagens que não puderam ser entregues ou processadas.
  * Entrega de Mensagens Agendada e Diferida: Permite o envio de mensagens para serem processadas em momentos futuros.
  * Operações Transacionais: Garante a atomicidade das operações de envio e recebimento de mensagens.
- Segurança e Conformidade:
  * Suporta protocolos de segurança como SAS (Shared Access Signature) e RBAC (Role-Based Access Control).
  * Está em conformidade com padrões como AMQP 1.0 e JMS.
- Diferenciação: diferente de alguns outros serviços de mensagens, o Azure Service Bus é uma oferta PaaS (Platform as a Service), o que significa que a Microsoft é responsável por lidar com falhas de hardware, atualizações do sistema, registo, cópias de segurança e failover, aliviando essa carga operacional dos utilizadores.

### Solução de demonstração do uso desses dois recursos: Cenário de uma arquitetura serveless com 2 functions (uma que gera boleto e outra que valida) e tendo com canal de integração filas no servicebus. Para tanto faço uso de alguns conceitos.
- Essa solução é simplificada para fins de demonstração. Uma solução de geração de boletos em produção envolveria: validação rigorosa de todos os dados de entrada, integração com uma API ou SDK de banco para gerar o boleto bancário real (que envolve cálculos de linha digitável, validação de convênios, etc.), geração de PDF do boleto, armazenamento seguro do boleto gerado (ex: Azure Blob Storage), mecanismos robustos de tratamento de erros e retries, segurança adicional, como uso de Azure Key Vault para credenciais, além de uma propositura de uma arquitetura exaustivamente discutida, testada e validada.
- Boleto como meio de pagamento: os boletos são amplamente utilizados para pagamentos de contas de consumo (água, luz, telefone), mensalidades, compras online, faturas de serviços, etc. O código de barras presente no boleto é uma representação visual das informações contidas na linha digitável. Ele é composto por uma sequência de barras escuras e claras de diferentes larguras, que seguem um padrão específico (no caso do Brasil, geralmente o padrão Intercalado 2 de 5 - ITF-2/5 para boletos bancários).
- O mecanismo de leitura funciona da seguinte forma:
  * Emissão de luz: Um leitor de código de barras (seja um scanner em um caixa, um leitor de celular ou um aplicativo de banco) emite um feixe de luz (geralmente laser ou LED) sobre o código.
  * Reflexão e absorção: As barras escuras absorvem a luz, enquanto as barras claras a refletem.
  * Conversão em sinal elétrico: A luz refletida é captada por um sensor no leitor. O sensor converte essa variação de luz em um sinal elétrico. As barras escuras geram um sinal de baixa intensidade e as claras, um sinal de alta intensidade.
  * Decodificação: O leitor possui um decodificador que interpreta esses sinais elétricos. Ele identifica o padrão de larguras das barras e espaços, convertendo-os em dados numéricos ou alfanuméricos.
  * Transmissão dos dados: Os dados decodificados são transmitidos para o sistema onde o pagamento está sendo processado (por exemplo, o sistema do banco ou da loja), que então utiliza essas informações para identificar o boleto e realizar a operação.
  * O Sistema que foi feito é para demonstração, para projetos mais consistentes consulte <https://portal.febraban.org.br/FebrabanTech>.

### Explicações do projeto
- Program.cs:
  * É o ponto de entrada do seu Worker de Funções .NET 8 Isolated Process.
  * ConfigureFunctionsWorkerDefaults(): Configura o Worker com os padrões para Azure Functions.
  * ConfigureServices(): É onde você registra suas dependências via Injeção de Dependência (DI). Aqui, registramos ServiceBusQueueService como Transient, o que significa que uma nova instância será criada para cada solicitação.
- GerarBoletoHttpTrigger.cs:
  * [Function("GerarBoleto")]: Define o nome da sua Azure Function. Este será o nome visível no portal do Azure.
  * [HttpTrigger(AuthorizationLevel.Function, "post", Route = "boletos")]:
    * AuthorizationLevel.Function: Autorização em nível de função. Isso significa que a requisição HTTP deve incluir uma chave de função no cabeçalho x-functions-key ou na string de consulta code. Esta é a segurança recomendada para a maioria dos casos.
    * "post": A função aceitará apenas requisições HTTP POST.
    * Route = "boletos": Define a rota da API.
  * Injeção de Dependência: O construtor da classe GerarBoletoHttpTrigger recebe ILogger e IServiceBusQueueService. O runtime do .NET lida com a criação dessas instâncias e as injeta quando a função é invocada, graças à configuração em Program.cs.
  * Deserialização do Request Body: A função tenta desserializar o corpo da requisição JSON para o objeto BoletoRequest.
  * Validação de Dados (System.ComponentModel.DataAnnotations): Usei atributos como [Required], [StringLength], [Range] no modelo BoletoRequest e chamei Validator.TryValidateObject para validar os dados de entrada. Isso garante que você receba dados esperados antes de prosseguir.
  * Envio para o Service Bus:
    * Cria um objeto anônimo (dadosParaGeracao) com as informações necessárias para a geração do boleto. Isso é o que será enviado para a fila.
    * _serviceBusQueueService.SendMessageAsync(nomeFilaBoletos, dadosParaGeracao): Usa o serviço injetado para enviar a mensagem para a fila do Azure Service Bus, cujo nome é "fila-solicitacoes-boletos".
    * Esta é a parte crucial do design: a geração real do boleto (que pode ser demorada) é desacoplada da requisição HTTP e será feita por outra função (ou serviço) que consome esta fila. Isso torna a API responsiva e escalável.
  * Resposta HTTP:
    * HttpStatusCode.Accepted (202): Indica que a solicitação foi aceita para processamento, mas o processamento real ainda não foi concluído. Isso é ideal para operações assíncronas.
    * WriteStringAsync: Escreve uma mensagem de texto na resposta.
  * ServiceBusQueueService.cs:
    * Encapsula a lógica de envio de mensagens para o Azure Service Bus.
    * Obtém a string de conexão do Service Bus das configurações (via IConfiguration).
    * Serializa o objeto da mensagem para JSON antes de enviá-lo como um ServiceBusMessage.



















