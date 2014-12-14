using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    interface ITFSService
    {
        bool IsFileInTFSSourceControl(string localPath);

        bool HasPendingChanges(string localItemPath);

        Task DownloadFileAtVersion(string localItemPath, int version, string outputPath);

        Task<ChangesetInfo> GetLatestVersion(string localItemPath);

        Task<int> GetLocalVersion(string localItemPath);

        void NavigateToChangeset(int changesetId);

        void CompareWithWorkspace(string item);

        void CompareWithLatest(string item);

        void GetLatest(string item);

        string GetEmail(string accountUniqueName);
    }
}
