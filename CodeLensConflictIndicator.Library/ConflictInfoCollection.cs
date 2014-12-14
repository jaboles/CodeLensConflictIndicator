using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CodeLens.ConflictIndicator
{
    public class ConflictInfoCollection : ObservableCollection<ConflictInfo>
    {
        public ConflictInfoCollection(IEnumerable<ConflictInfo> conflictInfo, ChangesetInfo latestVersion)
            : base(conflictInfo)
        {
            this.LatestVersion = latestVersion;
        }

        public ChangesetInfo LatestVersion { get; private set; }
    }
}
