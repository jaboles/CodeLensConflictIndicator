using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeLens.ConflictIndicator
{
    public class ConflictInfo
    {
        public ConflictInfo(Difference mineToBase, Difference serverToBase)
        {
            this.MineToBase = mineToBase;
            this.ServerToBase = serverToBase;
        }

        public static IEnumerable<ConflictInfo> None = Enumerable.Empty<ConflictInfo>();

        public Difference MineToBase
        {
            get;
            private set;
        }

        public Difference ServerToBase
        {
            get;
            private set;
        }

        public Span LineSpan
        {
            get
            {
                return this.MineToBase.Right;
            }
        }

        public bool IntersectsWith(Span span)
        {
            bool intersects = this.LineSpan.IntersectsWith(span);
            Debug.WriteLine(" Intersects: {0}", intersects);
            return intersects;
        }
    }
}
