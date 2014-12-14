//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeSense.Editor;
using System;

namespace CodeLens.ConflictIndicator
{
    /// <summary>
    /// PurpleHippo TemplateProvider class
    /// </summary>
    [CLSCompliant(false)]
    [DetailsTemplateProvider(typeof(ConflictDataPointViewModel))]
    public sealed partial class ConflictTemplateProvider : DetailsTemplateProvider
    {
        public ConflictTemplateProvider()
        {
            this.InitializeComponent();
        }
    }
}