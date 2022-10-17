

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace src
{
    public class Program
    {

        private static ServiceConfig config;
        private async static Task Main(string[] args)
        {

            // configuration
            IConfigurationBuilder cbuilder = new ConfigurationBuilder()
                    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                    .AddEnvironmentVariables();

            IConfigurationRoot root = cbuilder.Build();
            config = new();
            root.Bind(config);

            // webjob
            var builder = new HostBuilder();
            // builder.UseEnvironment("development");
            builder.ConfigureWebJobs(b =>
                    {
                        b.AddAzureStorageCoreServices();
                        b.AddAzureStorage();
                    });
            // dependency injection
            builder.ConfigureServices(s => {

            s.AddSingleton<ServiceConfig>(config);
            s.AddScoped<IntegrationService>();

            s.AddScoped<DoWork>();
            s.AddSingleton<IJobActivator, BackGroundActivator>();
            s.AddSingleton<BackgroundJob>();
            }

            );

            // host build and run
            var host = builder.Build();
              using (host)
            {



                var jobHost = host.Services.GetService(typeof(IJobHost)) as JobHost;
                await host.StartAsync();
                var inputs = new Dictionary<string, object> { { "value", "Hello from DevOps Deploy!" }};




              await jobHost.CallAsync(typeof(BackgroundJob).GetMethod("StartAsyncFunction"), inputs);






        }
    }
}
}
