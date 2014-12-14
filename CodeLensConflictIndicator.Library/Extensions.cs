using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    public static class Extensions
    {
        public static string GetDocumentFullPath(this IWpfTextView @this)
        {
            ITextBuffer textBuffer = @this.TextBuffer;
            if (textBuffer.Properties.ContainsProperty(typeof(ITextDocument)))
            {
                ITextDocument textDocument = textBuffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
                string filePath = textDocument.FilePath;
                return filePath;
            }
            else
            {
                return null;
            }
        }
    }
}
