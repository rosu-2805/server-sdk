namespace Morph.Server.Sdk.Client
{
    /// <summary>
    /// Transfer file from/to server to/from local file
    /// </summary>
    public class DataTransferUtility
    {
        private readonly IMorphServerApiClient morphServerApiClient;

        public DataTransferUtility(IMorphServerApiClient morphServerApiClient)
        {
            this.morphServerApiClient = morphServerApiClient;
        }

    }

}


