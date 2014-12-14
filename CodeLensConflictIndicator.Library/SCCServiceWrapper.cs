using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    public class SCCServiceWrapper<T> : ISCCServiceWrapper
        where T : ISCCService
    {
        private static SCCServiceWrapper<T> s_instance = new SCCServiceWrapper<T>();

        private IServiceProvider serviceProvider;

#pragma warning disable 0649
        [Import]
        private Lazy<T> realService;
#pragma warning restore 0649

        private SCCServiceWrapper()
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

        public static SCCServiceWrapper<T> Instance { get { return s_instance; } }

        public bool RealServiceReady()
        {
            if (this.serviceProvider == null)
                return false;

            if (this.realService == null)
                this.Initialize();

            return this.realService != null && this.realService.Value != null;
        }

        public bool IsFileInSourceControl(string localPath)
        {
            if (this.RealServiceReady())
                return this.realService.Value.IsFileInSourceControl(localPath);

            return false;
        }

        public bool HasPendingChanges(string localItemPath)
        {
            if (this.RealServiceReady())
                return this.realService.Value.HasPendingChanges(localItemPath);

            return false;
        }

        public Task DownloadFileAtVersion(string localItemPath, object version, string outputPath)
        {
            if (this.RealServiceReady())
                return this.realService.Value.DownloadFileAtVersion(localItemPath, version, outputPath);

            return null;
        }

        public Task<VersionInfo> GetLatestVersion(string localItemPath)
        {
            if (this.RealServiceReady())
                return this.realService.Value.GetLatestVersion(localItemPath);

            return null;
        }

        public Task<object> GetLocalVersion(string localItemPath)
        {
            if (this.RealServiceReady())
                return this.realService.Value.GetLocalVersion(localItemPath);

            return null;
        }

        public void NavigateToVersion(object changesetId)
        {
            if (this.RealServiceReady())
                this.realService.Value.NavigateToVersion(changesetId);
        }

        public void CompareWithWorkspace(string item)
        {
            if (this.RealServiceReady())
                this.realService.Value.CompareWithWorkspace(item);
        }

        public void CompareWithLatest(string item)
        {
            if (this.RealServiceReady())
                this.realService.Value.CompareWithLatest(item);
        }

        public void GetLatest(string item)
        {
            if (this.RealServiceReady())
                this.realService.Value.GetLatest(item);
        }

        public string GetEmail(string accountUniqueName)
        {
            if (this.RealServiceReady())
                return this.realService.Value.GetEmail(accountUniqueName);

            return null;
        }
    }
}
