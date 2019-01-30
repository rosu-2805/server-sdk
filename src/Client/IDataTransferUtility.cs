using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Client
{
    public interface IDataTransferUtility
    {
        Task SpaceUploadFileAsync(string localFilePath, string destFolderPath, CancellationToken cancellationToken, bool overwriteExistingFile = false);
    }
}