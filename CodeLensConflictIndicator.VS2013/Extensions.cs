using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System;

namespace CodeLens.ConflictIndicator
{
    [CLSCompliant(false)]
    public static class Extensions
    {
        public static TextSpan ToTextSpan(this Span @this)
        {
            return new TextSpan(@this.Start, @this.Length);
        }

        public static Span ToSpan(this TextSpan @this)
        {
            return new Span(@this.Start, @this.Length);
        }
    }
}
