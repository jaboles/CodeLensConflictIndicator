using System;

namespace CodeLens.ConflictIndicator
{
    public class VersionInfo
    {
        object id;

        public VersionInfo(object id, string ownerUniqueName, string ownerDisplayName, DateTime creationDate, string comment)
        {
            this.id = id;
            this.OwnerUniqueName = ownerUniqueName;
            this.OwnerDisplayName = ownerDisplayName;
            this.CreationDate = creationDate;
            this.Comment = comment;
        }

        public object Id { get; private set; }

        public string OwnerUniqueName { get; private set; }

        public string OwnerDisplayName { get; private set; }

        public DateTime CreationDate { get; private set; }

        public string Comment { get; private set; }
    }
}
