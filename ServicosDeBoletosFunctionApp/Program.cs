using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GeracaoBoletos.Infra.ServiceBus; 

namespace ServicosDeBoletos.Functions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                
                    s.AddTransient<IServiceBusQueueService, ServiceBusQueueService>();
                })
                .Build();

            host.Run();
        }
    }
}