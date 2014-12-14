using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    public interface ISCCServiceWrapper : ISCCService
    {
        bool RealServiceReady();
    }

    public interface ISCCService
    {
        bool IsFileInSourceControl(string localPath);

        bool HasPendingChanges(string localItemPath);

        Task DownloadFileAtVersion(string localItemPath, object version, string outputPath);

        Task<VersionInfo> GetLatestVersion(string localItemPath);

        Task<object> GetLocalVersion(string localItemPath);

        void NavigateToVersion(object version);

        void CompareWithWorkspace(string item);

        void CompareWithLatest(string item);

        void GetLatest(string item);

        string GetEmail(string accountUniqueName);
    }
}
