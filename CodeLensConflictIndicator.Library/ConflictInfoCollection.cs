using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CodeLens.ConflictIndicator
{
    public class ConflictInfoCollection : ObservableCollection<ConflictInfo>
    {
        public ConflictInfoCollection(IEnumerable<ConflictInfo> conflictInfo, VersionInfo latestVersion)
            : base(conflictInfo)
        {
            this.LatestVersion = latestVersion;
        }

        public VersionInfo LatestVersion { get; private set; }
    }
}
