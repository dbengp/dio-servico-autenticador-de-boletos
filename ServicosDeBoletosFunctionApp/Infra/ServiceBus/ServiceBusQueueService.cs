using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO; 

namespace ServicosDeBoletos.Infra.ServiceBus
{
    public class ServiceBusQueueService : IServiceBusQueueService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IConfiguration _configuration;

        public ServiceBusQueueService(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration["ServiceBusConnection"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("ServiceBusConnection setting is missing.");
            }
            _serviceBusClient = new ServiceBusClient(connectionString);
        }

        public async Task SendMessageAsync<T>(string queueName, T message)
        {
            ServiceBusSender sender = _serviceBusClient.CreateSender(queueName);
            var jsonMessage = JsonSerializer.Serialize(message);
            ServiceBusMessage sbMessage = new ServiceBusMessage(jsonMessage);

            await sender.SendMessageAsync(sbMessage);
            await sender.DisposeAsync();
        }
    }
}