using Azure.Health.DataServices.Channels;
using Azure.Health.DataServices.Clients;
using Azure.Health.DataServices.Json;
using Azure.Health.DataServices.Security;
using Azure.Health.DataServices.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace src
{
    public class IntegrationService : IHostedService
    {
        private readonly ServiceConfig config;
        private readonly IOptions<ServiceBusChannelOptions> serviceBusOptions;

        private IChannel channel;
        private readonly StorageLake storage;
        private readonly IAuthenticator authenticator;
        private string accessToken;
        private readonly ILogger logger;

        public IntegrationService(ServiceConfig config, IAuthenticator authenticator = null, ILogger<IntegrationService> logger = null)
        {
            this.config = config;
            this.authenticator = authenticator;
            this.logger = logger;
            serviceBusOptions = Options.Create<ServiceBusChannelOptions>(new ServiceBusChannelOptions()
            {
                ConnectionString = config.ServiceBusConnectionString,
                FallbackStorageConnectionString = config.BlobStorageConnectionString,
                FallbackStorageContainer = config.BlobStorageContainerName,
                ExecutionStatusType = Azure.Health.DataServices.Pipelines.StatusType.Any,
                Queue = config.QueueName,
                Sku = config.ServiceBusSku,
            });

            storage = new StorageLake(config.DataLakeConnectionString);


        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {

            //ensure data lake file system is configured
            try{
                await EnsureDataLakeFileSystem();
                }
            catch(Exception ex)
            {logger.LogError(ex, "Error ensuring data lake file system");}


            channel = new ServiceBusChannel(serviceBusOptions);

            //set the receive event for the channel, open the channel, and start receiving
            channel.OnReceive += Channel_OnReceive;
            channel.OnError += Channel_OnError;
            channel.OnStateChange += Channel_OnStateChange;
            await channel.OpenAsync();
            channel.ReceiveAsync().GetAwaiter();
        }



        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if(channel.State == ChannelState.Open)
            {
                await channel.CloseAsync();
            }

            channel.Dispose();
        }

        private async Task EnsureDataLakeFileSystem()
        {
            if (!await storage.FileSystemExistsAsync(config.DataLakeFileSystemName))
            {
                await storage.CreateFileSystemAsync(config.DataLakeFileSystemName);
            }

            if (!await storage.DirectoryExistsAsync(config.DataLakeFileSystemName, config.DataLakeDirectoryName))
            {
                await storage.CreateDirectoryAsync(config.DataLakeFileSystemName, config.DataLakeDirectoryName);
            }
        }

        private async void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            //message from service bus
            Console.WriteLine("message received");
            string json = Encoding.UTF8.GetString(e.Message);
            string patientId = GetPatientId(json);

            if(patientId == null)
            {
                logger?.LogWarning("Patient id does not exist.");
                return;
            }

            //get an access token is configured
            accessToken = authenticator != null ? await authenticator.AcquireTokenForClientAsync(config.FhirServerUrl) : "";

            //get the encounter from the fhir server
            string content = await GetEncountersAsync(patientId, accessToken);

            //write the content to data lake
            await WriteToDataLakeAsync(content, patientId);

        }

        private string GetPatientId(string json)
        {
            JToken jtoken = JToken.Parse(json);
            return jtoken.Exists(config.JPath) ? jtoken.GetToken(config.JPath).GetValue<string>() : null;
        }

        private async Task WriteToDataLakeAsync(string content, string patientId)
        {
            if (string.IsNullOrEmpty(content))
            {
                logger?.LogWarning("Patient Id {id} encounter content is null or empty.", patientId);
            }
            else
            {
                await storage.WriteFileAsync(config.DataLakeFileSystemName, config.DataLakeDirectoryName, $"{patientId}.json", Encoding.UTF8.GetBytes(content));
            }
        }
        private async Task<string> GetEncountersAsync(string patientId, string accessToken)
        {
            //get the encounter from the fhir server
            RestRequestBuilder builder = new("GET", config.FhirServerUrl, accessToken, "Encounter", $"id={patientId}", null, null);
            RestRequest request = new(builder);
            string content = null;
            try
            {
                HttpResponseMessage fhirMessage = await request.SendAsync();
                if (!fhirMessage.IsSuccessStatusCode)
                {
                    logger?.LogWarning("Encounter search fault {statusCode} returning patient {id}. ", fhirMessage.StatusCode, patientId);
                    return null;
                }
                else
                {
                    content = await fhirMessage.Content.ReadAsStringAsync();
                }
            }
            catch(Exception ex)
            {
                logger?.LogError(ex, "Rest request fault.");
            }

            return content;
        }

        private async void Channel_OnStateChange(object sender, ChannelStateEventArgs e)
        {
            if(e.State == ChannelState.Aborted || e.State == ChannelState.Closed)
            {
                await StopAsync(default);
                await StartAsync(default);
            }
        }

        private async void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            if (channel.State != ChannelState.Closed)
            {
                await channel.CloseAsync();
            }
        }
    }
}
