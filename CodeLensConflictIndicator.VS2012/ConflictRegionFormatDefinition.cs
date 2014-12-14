using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CodeLens.ConflictIndicator
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(ConflictRegionFormatDefinition.Name)]
    [DisplayName("Conflict in Source Control")]
    [UserVisible(true)]
    public class ConflictRegionFormatDefinition : ClassificationFormatDefinition
    {
        public const string Name = "MarkerFormatDefinition/ConflictRegionFormatDefinition";

        public ConflictRegionFormatDefinition()
        {
            this.ForegroundColor = Colors.Red;
            this.BackgroundColor = Colors.LightBlue;
        }
    }
}
