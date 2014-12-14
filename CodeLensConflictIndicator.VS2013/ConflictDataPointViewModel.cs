//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Alm.MVVM;
using Microsoft.VisualStudio.CodeSense.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using System.Windows.Media;

namespace CodeLens.ConflictIndicator
{
    [CLSCompliant(false)]
    public class ConflictDataPointViewModel : GlyphDataPointViewModel
    {
        public ConflictDataPointViewModel(ConflictDataPoint dataPoint)
            : base(dataPoint)
        {
            this.PropertyChanged += this.OnPropertyChanged;
            this.ConflictViewModel = new ConflictViewModel();
        }

        public override ImageSource GlyphSource
        {
            get
            {
                return Icons.IndicatorIcon;
            }
        }

        public ConflictViewModel ConflictViewModel
        {
            get;
            private set;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == "Data")
            {
                ConflictInfoCollection data = (ConflictInfoCollection)this.Data;
                ConflictDataPoint dataPoint = (ConflictDataPoint)this.DataPoint;
                if (data != null)
                {
                    this.HasDetails = true;
                    this.ConflictViewModel.LatestVersion = data.LatestVersion;
                    this.ConflictViewModel.FilePath = dataPoint.MethodIdentifier.FilePath;
                    if (data.Count > 1)
                    {
                        this.Descriptor = string.Format(CultureInfo.CurrentCulture, Strings.MultipleConflictsDetected, data.Count);
                    }
                    else if (data.Count == 1)
                    {
                        this.Descriptor = Strings.SingleConflictDetected;
                    }
                }
                else
                {
                    this.HasDetails = false;
                    this.Descriptor = string.Empty;
                    this.ConflictViewModel.LatestVersion = null;
                }
            }
        }
    }
}