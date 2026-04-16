using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Infrastructure;

namespace EhrClinical.AzureFunction
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    services.AddInfrastructure();
                    services.AddSingleton<PhiScrubber>();
                })
                .Build();

            host.Run();
        }
    }
}
