//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeSense.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using System;

namespace CodeLens.ConflictIndicator
{
    [CLSCompliant(false)]
    [DataPointViewModelProvider(typeof(ConflictDataPoint))]
    public class ConflictDataPointViewModelProvider : GlyphDataPointViewModelProvider<ConflictDataPointViewModel>
    {
        protected override ConflictDataPointViewModel GetViewModel(ICodeLensDataPoint dataPoint)
        {
            var ConflictDataPoint = dataPoint as ConflictDataPoint;
            return new ConflictDataPointViewModel(ConflictDataPoint);
        }
    }
}