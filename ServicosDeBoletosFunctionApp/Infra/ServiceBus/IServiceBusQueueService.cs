using System.Threading.Tasks;

namespace ServicosDeBoletos.Infra.ServiceBus
{
    public interface IServiceBusQueueService
    {
        Task SendMessageAsync<T>(string queueName, T message);
    }
}