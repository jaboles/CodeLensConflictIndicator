using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    public class TFSServiceWrapper : ITFSService
    {
        private static TFSServiceWrapper s_instance = new TFSServiceWrapper();

        private IServiceProvider serviceProvider;

#pragma warning disable 0649
        [Import]
        private Lazy<ITFSService> realService;
#pragma warning restore 0649

        private TFSServiceWrapper()
        {
        }

        /// <summary>
        /// Initialize called by the package once MS.VS.TF is loaded
        /// </summary>
        /// <param name="serviceProvider"></param>
        public void Initialize(IServiceProvider sp)
        {
            this.serviceProvider = sp;
        }

        /// <summary>
        /// Initialize called by ourselves when it's time to get some data
        /// </summary>
        public void Initialize()
        {
            if (this.realService == null)
            {
                // satisfied imports
                IComponentModel componentModel = this.serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
                if (componentModel == null)
                {
                    return;
                }

                componentModel.DefaultCompositionService.SatisfyImportsOnce(this);
            }
        }

        public static TFSServiceWrapper Instance { get { return s_instance; } }

        public bool RealTFSServiceReady()
        {
            if (this.serviceProvider == null)
                return false;

            if (this.realService == null)
                this.Initialize();

            return this.realService != null && this.realService.Value != null;
        }

        public bool IsFileInTFSSourceControl(string localPath)
        {
            if (this.RealTFSServiceReady())
                return this.realService.Value.IsFileInTFSSourceControl(localPath);

            return false;
        }

        public bool HasPendingChanges(string localItemPath)
        {
            if (this.RealTFSServiceReady())
                return this.realService.Value.HasPendingChanges(localItemPath);

            return false;
        }

        public Task DownloadFileAtVersion(string localItemPath, int version, string outputPath)
        {
            if (this.RealTFSServiceReady())
                return this.realService.Value.DownloadFileAtVersion(localItemPath, version, outputPath);

            return null;
        }

        public Task<ChangesetInfo> GetLatestVersion(string localItemPath)
        {
            if (this.RealTFSServiceReady())
                return this.realService.Value.GetLatestVersion(localItemPath);

            return null;
        }

        public Task<int> GetLocalVersion(string localItemPath)
        {
            if (this.RealTFSServiceReady())
                return this.realService.Value.GetLocalVersion(localItemPath);

            return null;
        }

        public void NavigateToChangeset(int changesetId)
        {
            if (this.RealTFSServiceReady())
                this.realService.Value.NavigateToChangeset(changesetId);
        }

        public void CompareWithWorkspace(string item)
        {
            if (this.RealTFSServiceReady())
                this.realService.Value.CompareWithWorkspace(item);
        }

        public void CompareWithLatest(string item)
        {
            if (this.RealTFSServiceReady())
                this.realService.Value.CompareWithLatest(item);
        }

        public void GetLatest(string item)
        {
            if (this.RealTFSServiceReady())
                this.realService.Value.GetLatest(item);
        }

        public string GetEmail(string accountUniqueName)
        {
            if (this.RealTFSServiceReady())
                return this.realService.Value.GetEmail(accountUniqueName);

            return null;
        }
    }
}
