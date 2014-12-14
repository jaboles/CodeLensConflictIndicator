using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    public sealed class EditingSession : EditorLifetimeObject, IDisposable
    {
        public static readonly TimeSpan RecalculateTimerInterval = TimeSpan.FromMilliseconds(500);

        private readonly string filePath;
        private readonly object lockObj = new object();
        private readonly SemaphoreSlim recalculateLock = new SemaphoreSlim(1, 1);
        private readonly System.Timers.Timer recalculateTimer = new System.Timers.Timer(RecalculateTimerInterval.TotalMilliseconds);
        private IEnumerable<ConflictInfo> allConflictInfo = null;

        private readonly FileSystemWatcher fileWatcher;
        
        // Info about the user's local version
        private object localVersion;
        private string localVersionContent;

        // Info about the latest version on teh server
        private VersionInfo latestVersion;
        private string latestVersionContent;

        private IHierarchicalDifferenceCollection latestComparedToLocalDifferences;
        private IHierarchicalDifferenceCollection editBufferComparedToLocalDifferences;

        public EditingSession(string filePath, ITextBuffer textBuffer, ITextView textView)
            : base(textBuffer, textView)
        {
            this.filePath = filePath;
            this.fileWatcher = new FileSystemWatcher();
            this.fileWatcher.Path = Path.GetDirectoryName(filePath);
            this.fileWatcher.Changed += fileWatcher_ChangedOrRenamed;
            this.fileWatcher.Renamed += fileWatcher_ChangedOrRenamed;
            this.fileWatcher.EnableRaisingEvents = true;

            this.recalculateTimer.AutoReset = true;
            this.recalculateTimer.Elapsed += recalculateTimer_Elapsed;
        }

        public void Dispose()
        {
            this.recalculateTimer.Enabled = false;
            this.recalculateTimer.Elapsed -= recalculateTimer_Elapsed;
            
            this.recalculateTimer.Dispose();
            this.fileWatcher.Dispose();
            this.recalculateLock.Dispose();
        }

        public event EventHandler<ConflictDataChangedEventArgs> ConflictDataChanged;

        public string FilePath
        {
            get { return this.filePath; }
        }

        public VersionInfo LatestVersion
        {
            get { return this.latestVersion; }
        }

        public bool SCCServiceReady
        {
            get { return SCCService.RealServiceReady(); }
        }

        public ISCCServiceWrapper SCCService
        {
            get { return SCCServiceWrapper<TFSService>.Instance; }
        }

        public IEnumerable<ConflictInfo> GetConflicts()
        {
            // Lock here is to prevent conflicts being recalculated for
            // every conflict datapoint in the editor viewport e.g. when a file
            // is opened. allConflictsInfo starts off as null and once it is set
            // to a value, it is never null again. Put the null check inside the lock
            // so that only one call to GetConflicts() causes the first-time calculation,
            // but also put the null check outside of the lock so that in the future
            // no time is wasted acquiring a useless lock.
            if (this.allConflictInfo == null)
            {
                lock (lockObj)
                {
                    if (this.allConflictInfo == null)
                    {
                        RecalculateConflicts().Wait();
                    }
                }
            }

            return this.allConflictInfo;
        }

        protected override async void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            base.OnTextBufferChanged(sender, e);

            // If recalculate timer is running (because user is doing a quick succession of edits e.g. typing), don't
            // do anything. When the timer next fires (within 500ms from now) is when recalculation will take place.
            // If the timer is not running, enable the timer and do a recalculation right now.
            if (!this.recalculateTimer.Enabled)
            {
                this.recalculateTimer.Enabled = true;
                await this.RecalculateConflicts();
            }
        }

        private async Task<bool> UpdateLocalVersionContent()
        {
            // Check if the local version changed.
            object newLocalVersion = SCCService.GetLocalVersion(filePath).Result;
            if (!newLocalVersion.Equals(0) && !string.IsNullOrEmpty(newLocalVersion as string) && !newLocalVersion.Equals(this.localVersion))
            {
                // Get the content of the new local version
                string downloadedLocalVersion = Path.GetTempFileName();
                Debug.WriteLine(string.Format("Downloading latest from server to file://{0}", downloadedLocalVersion));
                await SCCService.DownloadFileAtVersion(this.filePath, newLocalVersion, downloadedLocalVersion);
                this.localVersionContent = File.ReadAllText(downloadedLocalVersion);
                this.localVersion = newLocalVersion;

                File.Delete(downloadedLocalVersion);
                return true;
            }

            return false;
        }

        private async Task<bool> UpdateLatestVersionContent()
        {
            // TODO: put this in an event handler that calls this method.
            var newLatestVersion = await SCCService.GetLatestVersion(this.filePath);
            Debug.WriteLine("Latest version: {0}", localVersion);

            if (newLatestVersion != null && (this.latestVersion == null || !this.latestVersion.Id.Equals(newLatestVersion.Id)))
            {
                // Get the content of the latest version
                string downloadedLatest = Path.GetTempFileName();
                Debug.WriteLine(string.Format("Downloading latest from server to file://{0}", downloadedLatest));
                await SCCService.DownloadFileAtVersion(this.filePath, newLatestVersion.Id, downloadedLatest);
                this.latestVersionContent = File.ReadAllText(downloadedLatest);
                this.latestVersion = newLatestVersion;

                File.Delete(downloadedLatest);
                return true;
            }

            return false;
        }

        private void UpdateLatestComparedToLocalVersionDifferences()
        {
            // Recalculate differences between server version and local version.
            // This can be done now, because it is independent of the user's editing
            // session.
            if (this.latestVersion != null && this.latestVersionContent != null)
            {
                if (this.latestVersion.Id.Equals(this.localVersion))
                {
                    // If user's local workspace is now at the latest, there are no differences.
                    this.latestComparedToLocalDifferences = null;
                }
                else
                {
                    // Otherwise, calculate the differences.
                    this.latestComparedToLocalDifferences = FileComparerService.Instance.GetDifferences(this.TextBuffer.ContentType, this.localVersionContent, this.latestVersionContent);
                }
            }
        }

        private void UpdateEditBufferComparedToLocalVersionDifferences()
        {
            string editBufferContents = this.TextBuffer.CurrentSnapshot.GetText().ToString();
            this.editBufferComparedToLocalDifferences = FileComparerService.Instance.GetDifferences(this.TextBuffer.ContentType, this.localVersionContent, editBufferContents);
        }

        public async Task RecalculateConflicts(ForceCheckForNewVersion forceCheckForNewVersion = ForceCheckForNewVersion.None)
        {
            // Hasty exit if the file is not even under source control.
            if (!SCCService.IsFileInSourceControl(this.filePath))
            {
                return;
            }

            try
            {
                await recalculateLock.WaitAsync();
                bool versionUpdated = false;

                // Fetch the local version (base) if it hasn't yet been.
                // This is only the first-time update, subsequent updates are handled
                // by an event fired by a FileSystemWatcher that monitors the file.
                if (forceCheckForNewVersion == ForceCheckForNewVersion.LocalVersion || this.localVersionContent == null || this.localVersion == null)
                {
                    versionUpdated |= await UpdateLocalVersionContent();
                }

                // Fetch the latest version (server) if it hasn't yet been.
                if (forceCheckForNewVersion == ForceCheckForNewVersion.LatestVersion || this.latestVersionContent == null || this.latestVersion == null)
                {
                    versionUpdated |= await UpdateLatestVersionContent();
                }

                if (versionUpdated)
                {
                    UpdateLatestComparedToLocalVersionDifferences();
                }

                // If both versions were fetched successfully and are different (indicating
                // the user's local version is not at the latest), calculate conflicts.s
                IEnumerable<ConflictInfo> conflicts = null;
                if (this.latestVersion != null &&
                    this.localVersion != null &&
                    !this.latestVersion.Id.Equals(this.localVersion) &&
                    this.localVersionContent != null &&
                    this.latestVersionContent != null)
                {
                    await Task.Run(() =>
                    {
                        UpdateEditBufferComparedToLocalVersionDifferences();

                        Debug.WriteLine("Found {0} diffs between current and local, {1} diffs between server and local",
                            this.editBufferComparedToLocalDifferences.Count(),
                            this.latestComparedToLocalDifferences.Count());

                        // Where the 'left' (i.e. last-synced local version) side of the diffs intersect is where there will be conflicts
                        // if the user were to sync at this time.
                        conflicts = this.editBufferComparedToLocalDifferences.SelectMany(ld =>
                        {
                            Debug.WriteLine("Examining local {0} diff at span L [{1},{2}) : R [{3}, {4})", ld.DifferenceType, ld.Left.Start, ld.Left.End, ld.Right.Start, ld.Right.End);
                            // The local change conflicts with the server change if there is any intersection between them
                            IEnumerable<Difference> conflictingServerLatestDifferences = this.latestComparedToLocalDifferences.Where(sd =>
                            {
                                bool intersects = ld.Left.IntersectsWith(sd.Left);
                                return intersects;
                            });
                            Debug.WriteLine(" Conflicts with latest on server: {0}", conflictingServerLatestDifferences.Count() > 0);

                            return conflictingServerLatestDifferences.Select(sd => new ConflictInfo(ld, sd));
                        }).ToArray();
                    });
                }
                else
                {
                    conflicts = ConflictInfo.None;
                }

                if (conflicts != null)
                {
                    bool changed = false;
                    if (this.allConflictInfo == null)
                    {
                        // Conflict info hasn't been calculated at all yet.
                        changed = true;
                    }
                    else if (conflicts.Count() != this.allConflictInfo.Count())
                    {
                        // Number of conflicts has changed.
                        changed = true;
                    }
                    else if (versionUpdated)
                    {
                        changed = true;
                    }
                    else // conflicts.Count() is equal to this.allConflictInfo.Count()
                    {
                        // Number of conflicts is the same. Do a more detailed check by
                        // comparing each conflict with the old one.
                        foreach (var c in conflicts.Zip(this.allConflictInfo, (newConflict, oldConflict) => Tuple.Create<ConflictInfo, ConflictInfo>(newConflict, oldConflict)))
                        {
                            if (!c.Item1.MineToBase.Equals(c.Item2.MineToBase))
                            {
                                changed = true;
                                break;
                            }
                        }
                    }

                    // If any changes in the current list of all conflicts were detected,
                    // fire event to get the data points to update.
                    if (changed)
                    {
                        this.allConflictInfo = conflicts;
                        EventHandler<ConflictDataChangedEventArgs> eh = this.ConflictDataChanged;
                        if (eh != null)
                        {
                            eh(this, new ConflictDataChangedEventArgs(this.allConflictInfo, this.LatestVersion));
                        }
                    }
                }
            }
            finally
            {
                recalculateLock.Release();
            }
        }

        protected override void OnTextViewClosed(object sender, EventArgs e)
        {
            base.OnTextViewClosed(sender, e);
            this.fileWatcher.Changed -= fileWatcher_ChangedOrRenamed;
            this.fileWatcher.Renamed -= fileWatcher_ChangedOrRenamed;
            this.Dispose();
        }

        private async void fileWatcher_ChangedOrRenamed(object sender, FileSystemEventArgs e)
        {
            if ((e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Renamed) &&
                e.FullPath.Equals(this.filePath, StringComparison.InvariantCultureIgnoreCase))
            {
                // Occurred because the user saved the file, or TFS updated it.
                await RecalculateConflicts(ForceCheckForNewVersion.LocalVersion);
            }
        }

        private async void recalculateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.recalculateTimer.Enabled = false;
            await RecalculateConflicts();
        }
    }

    public class ConflictDataChangedEventArgs : EventArgs
    {
        public ConflictDataChangedEventArgs(IEnumerable<ConflictInfo> newConflicts, VersionInfo latestVersion)
            : base()
        {
            this.NewConflicts = newConflicts;
            this.LatestVersion = latestVersion;
        }

        public IEnumerable<ConflictInfo> NewConflicts { get; private set; }
        public VersionInfo LatestVersion { get; private set; }
    }
}
