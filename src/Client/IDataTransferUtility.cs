using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Client
{
    public interface IDataTransferUtility
    {        
        Task SpaceUploadFileAsync(string localFilePath, string serverFolder, CancellationToken cancellationToken, bool overwriteExistingFile = false);
        Task SpaceUploadFileAsync(string localFilePath, string serverFolder, string destFileName, CancellationToken cancellationToken, bool overwriteExistingFile = false);
        Task SpaceDownloadFileIntoFileAsync(string remoteFilePath, string targetLocalFilePath, CancellationToken cancellationToken, bool overwriteExistingFile = false);
        Task SpaceDownloadFileIntoFolderAsync(string remoteFilePath, string targetLocalFolder, CancellationToken cancellationToken, bool overwriteExistingFile = false);
    }

}