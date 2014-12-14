//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CodeSense.Roslyn;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Timers;

namespace CodeLens.ConflictIndicator
{
    [Export(typeof(ICodeLensDataPointProvider))]
    [Name(ConflictDataPointProvider.IndicatorName)]
    [LocalizedName(typeof(Strings), "ProviderName")]
    public sealed class ConflictDataPointProvider : ICodeLensDataPointProvider
    {
        public const string IndicatorName = "Conflicts";
        private static IServiceProvider serviceProvider;
    
        public ConflictDataPointProvider()
        {
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            ConflictDataPointProvider.serviceProvider = serviceProvider;
        }

        public bool CanCreateDataPoint(ICodeLensDescriptor descriptor)
        {
            if (serviceProvider == null)
            {
                return false;
            }

            var codeElementDescriptor = descriptor as ICodeElementDescriptor;
            if (codeElementDescriptor != null &&
                (codeElementDescriptor.Kind == SyntaxNodeKind.Method ||
                 codeElementDescriptor.Kind == SyntaxNodeKind.Type ||
                 codeElementDescriptor.Kind == SyntaxNodeKind.Property))
            {
                return true;
            }

            return false;
        }

        public ICodeLensDataPoint CreateDataPoint(ICodeLensDescriptor descriptor)
        {
            if (serviceProvider == null)
            {
                return null;
            }

            ICodeElementDescriptor codeElement = descriptor as ICodeElementDescriptor;
            string filePath = codeElement.FilePath;
            EditingSession editingSession = EditingSessionFactory.WaitForSession(filePath);

            return new ConflictDataPoint(editingSession, codeElement);
        }
    }
}