using Azure.Health.DataServices.Channels;

namespace src
{

    public class ServiceConfig
    {
      

        public string ServiceBusConnectionString { get; set; }

        public string DataLakeConnectionString { get; set; }

        public string BlobStorageConnectionString { get; set; }

        public string BlobStorageContainerName { get; set; }

        public string QueueName { get; set; }

        public ServiceBusSkuType ServiceBusSku { get; set; }

        public string DataLakeFileSystemName { get; set; }

        public string DataLakeDirectoryName { get; set; }

        public string JPath { get; set; }

        public string FhirServerUrl { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
        public string TenantId { get; set; }


    }

}