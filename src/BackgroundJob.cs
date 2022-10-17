
using System.Threading;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


using Microsoft.Extensions.Logging;

namespace src
{
    public class BackgroundJob
    {

        private ServiceConfig _serviceConfig;
        private DoWork _doWork;
        private IntegrationService _intergrationservice;

        public BackgroundJob(ServiceConfig serviceConfig, IntegrationService intergrationservice, DoWork
        dowork)
{
    _serviceConfig = serviceConfig;
    _intergrationservice = intergrationservice;
    _doWork = dowork;
}






        [NoAutomaticTrigger]
        public async Task StartAsyncFunction(
       ILogger logger,
       string value,
       CancellationToken cancellationToken )
        {



             // await _intergrationservice.StartAsync(cancellationToken);
              //await _doWork.StartAsync(cancellationToken);
              await _doWork.DoWorkAsync(cancellationToken);
              var   message = value;
              Console.WriteLine(message);

              while(true){

              }






        }


    }
}
