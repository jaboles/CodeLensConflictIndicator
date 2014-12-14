using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITFSService))]
    class TFSService : ITFSService
    {
        private IServiceProvider serviceProvider;
        private EnvDTE.DTE dte;

        [ImportingConstructor]
        public TFSService([Import(typeof(Microsoft.VisualStudio.Shell.SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            Debug.Assert(serviceProvider != null);

            this.dte = this.serviceProvider.GetService(typeof(DTE)) as DTE;
            Debug.Assert(this.dte != null);
        }

        public bool IsFileInTFSSourceControl(string localPath)
        {
            var workspace = this.GetWorkspace(localPath);
            return workspace != null;
        }

        public bool HasPendingChanges(string localPath)
        {
            Debug.WriteLine("TFSService.HasPendingChanges: Checking pending changes for: " + localPath);

            var workspace = this.GetWorkspace(localPath);
            if (workspace != null)
            {
                PendingChange[] itemPendingChanges = workspace.GetPendingChanges(localPath);
                if (itemPendingChanges.Any())
                {
                    Debug.WriteLine("TFSService.HasPendingChanges: Item has the following pending change: " + itemPendingChanges.First().ChangeType);
                    return itemPendingChanges.First().ChangeType.HasFlag(ChangeType.Edit);
                }
            }

            return false;
        }

        public async Task DownloadFileAtVersion(string localItemPath, int version, string outputPath)
        {
            await Task.Run(() =>
            {
                var vcs = GetVersionControl();

                Item item = vcs.GetItem(localItemPath, new ChangesetVersionSpec(version), DeletedState.NonDeleted, true);
                Debug.WriteLine("TFSService.DownloadFileAtVersion: Starting download of " + localItemPath);
                item.DownloadFile(outputPath);
                Debug.WriteLine("TFSService.DownloadFileAtVersion: Completed download of " + localItemPath);
            });
        }

        public async Task<ChangesetInfo> GetLatestVersion(string localItemPath)
        {
            var vcs = this.GetVersionControl();

            if (vcs == null)
            {
                return null;
            }

            var workspace = vcs.TryGetWorkspace(localItemPath);
            if (workspace == null)
            {
                return null;
            }

            string serverItem = workspace.TryGetServerItemForLocalItem(localItemPath);
            Changeset latestChangeset = await Task.Run(() => vcs.QueryHistory(new ItemSpec(serverItem, RecursionType.None), 1).SingleOrDefault());
            if (latestChangeset != null)
            {
                int latestChangesetId = latestChangeset.ChangesetId;
                return new ChangesetInfo(latestChangeset.ChangesetId,
                    latestChangeset.Owner,
                    latestChangeset.OwnerDisplayName,
                    latestChangeset.CreationDate,
                    latestChangeset.Comment);
            }
            else
            {
                return null;
            }
        }

        public Task<int> GetLocalVersion(string localItemPath)
        {
            var workspace = this.GetWorkspace(localItemPath);
            LocalVersion[][] localVersions = workspace.GetLocalVersions(new ItemSpec[] { new ItemSpec(localItemPath, RecursionType.None) }, false);

            if (localVersions.Length > 0)
            {
                if (localVersions[0].Length > 0)
                {
                    return Task.FromResult(localVersions[0][0].Version);
                }
            }

            return Task.FromResult(0);
        }

        public void NavigateToChangeset(int changesetId)
        {
            TeamExplorerUtils.Instance.TryNavigateToChangesetDetails(this.serviceProvider, changesetId, TeamExplorerUtils.NavigateOptions.AlwaysNavigate);
        }

        public void CompareWithWorkspace(string item)
        {
            Workspace w = this.GetWorkspace(item);
            if (w != null)
            {
                PendingChange pc = w.GetPendingChanges(item).SingleOrDefault();
                if (pc != null)
                {
                    ClientHelperVSProxy.Instance.CompareWithWorkspaceVersion(new[] { pc }, w);
                }
            }
        }

        public void CompareWithLatest(string item)
        {
            Workspace w = this.GetWorkspace(item);
            if (w != null)
            {
                PendingChange pc = w.GetPendingChanges(item).SingleOrDefault();
                if (pc != null)
                {
                    ClientHelperVSProxy.Instance.CompareWithLatestVersion(new[] { pc });
                }
            }
        }

        public void GetLatest(string item)
        {
            Workspace workspace = this.GetWorkspace(item);
            if (workspace == null)
            {
                return;
            }

            string serverPath = workspace.TryGetServerItemForLocalItem(item);
            if (serverPath != null)
            {
                using (UIHost.GetWaitCursor())
                {
                    IVsFileChangeEx fce = serviceProvider.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
                    int hr = 0;

                    if (fce != null)
                    {
                        try
                        {
                            // Ignore file changes, so there is no Reload dialog
                            hr = fce.IgnoreFile(VSConstants.VSCOOKIE_NIL, item, 1);
                            Debug.Assert(hr == VSConstants.S_OK);
                            GetRequest getRequest = new GetRequest(serverPath, RecursionType.None, VersionSpec.Latest);
                            GetStatus status = workspace.Get(getRequest, GetOptions.None, null, null);
                            if (status != null && status.NumConflicts > 0)
                            {
                                // Trigger resolve conflicts tool window if there are any, to be consistent with solution explorer
                                this.dte.ExecuteCommand("File.TfsResumeConflictResolution");
                            }
                        }
                        finally
                        {
                            // Sync file so that file changes do not trigger events later when we un-ignore
                            hr = fce.SyncFile(item);
                            Debug.Assert(hr == VSConstants.S_OK);
                            hr = fce.IgnoreFile(VSConstants.VSCOOKIE_NIL, item, 0);
                            Debug.Assert(hr == VSConstants.S_OK);
                        }
                    }
                }
            }
        }

        public string GetEmail(string accountUniqueName)
        {
            var tfs = this.dte.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt") as TeamFoundationServerExt;
            if (tfs == null || tfs.ActiveProjectContext == null)
            {
                return null;
            }

            string activeUri = tfs.ActiveProjectContext.DomainUri;
            if (string.IsNullOrEmpty(activeUri))
            {
                return null;
            }

            TfsTeamProjectCollection collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(activeUri));
            if (collection == null)
            {
                return null;
            }

            IIdentityManagementService ims = collection.GetService(typeof(IIdentityManagementService)) as IIdentityManagementService;
            if (ims == null)
            {
                return null;
            }

            TeamFoundationIdentity identity = ims.ReadIdentity(IdentitySearchFactor.AccountName, accountUniqueName, MembershipQuery.Direct, ReadIdentityOptions.None);
            if (identity == null)
            {
                return null;
            }

            string email = identity.GetProperty("Mail") as string;
            return email;
        }

        private VersionControlServer GetVersionControl()
        {
            // IndicatorCommands won't be initialized if the TFS realted assemblies was not been loaded, so this call won't load extra assemblies.
            var tfs = this.dte.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt") as TeamFoundationServerExt;
            if (tfs == null || tfs.ActiveProjectContext == null)
            {
                return null;
            }

            string activeUri = tfs.ActiveProjectContext.DomainUri;
            if (string.IsNullOrEmpty(activeUri))
            {
                return null;
            }

            TfsTeamProjectCollection collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(activeUri));
            if (collection == null)
            {
                return null;
            }

            VersionControlServer versionControlServer = collection.GetService<VersionControlServer>();
            return versionControlServer;
        }

        private Workspace GetWorkspace(string localPath)
        {
            VersionControlServer vcs = GetVersionControl();
            if (vcs == null)
            {
                return null;
            }

            var workspace = vcs.TryGetWorkspace(localPath);
            return workspace;
        }
    }
}
