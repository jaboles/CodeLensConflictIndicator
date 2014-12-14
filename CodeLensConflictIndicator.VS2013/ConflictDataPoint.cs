//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.VisualStudio.CodeSense;
using Microsoft.VisualStudio.CodeSense.Roslyn;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    [CLSCompliant(false)]
    public class ConflictDataPoint : DataPoint<ConflictInfoCollection>
    {
        private bool disposed;
        private object disposeLock = new object();

        public ConflictDataPoint(EditingSession editingSession, ICodeElementDescriptor methodIdentifier)
        {
            if (editingSession == null) throw new ArgumentNullException("editingSession");

            this.MethodIdentifier = methodIdentifier;
            this.EditingSession = editingSession;

            EditingSession.ConflictDataChanged += this.OnConflictDataChanged;
        }

        public EditingSession EditingSession
        {
            get;
            private set;
        }

        public ICodeElementDescriptor MethodIdentifier
        {
            get;
            private set;
        }

        protected override void Dispose(bool disposing)
        {
            lock (this.disposeLock)
            {
                if (!this.disposed)
                {
                    if (disposed)
                    {
                        this.MethodIdentifier.SyntaxNodeChanged -= this.OnSyntaxNodeChanged;
                        this.EditingSession.ConflictDataChanged -= this.OnConflictDataChanged;
                    }

                    this.disposed = true;
                }
            }

            base.Dispose(disposing);
        }

        public override Task<ConflictInfoCollection> GetDataAsync()
        {
            // TFS binaries not loaded. Hasty exit.
            if (!this.EditingSession.SCCServiceReady)
            {
                return null;
            }

            CommonSyntaxNode syntaxNode = this.MethodIdentifier.SyntaxNode;

            this.MethodIdentifier.SyntaxNodeChanged += this.OnSyntaxNodeChanged;

            return this.Run<ConflictInfoCollection>(token =>
            {
                return Task.Run<ConflictInfoCollection>(() =>
                {
                    IEnumerable<ConflictInfo> conflicts = this.EditingSession.GetConflicts();

                    if (conflicts != null)
                    {
                        // Get the conflicts that intersect with this method.
                        FileLinePositionSpan syntaxNodeFileLineSpan = syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.FullSpan, false);
                        Span syntaxNodeLineSpan = new Span(syntaxNodeFileLineSpan.StartLinePosition.Line, syntaxNodeFileLineSpan.EndLinePosition.Line - syntaxNodeFileLineSpan.StartLinePosition.Line);
                        IEnumerable<ConflictInfo> conflictsWithinNode = conflicts.Where(c => c.IntersectsWith(syntaxNodeLineSpan));

                        if (conflictsWithinNode.Any())
                        {
                            return new ConflictInfoCollection(conflictsWithinNode, this.EditingSession.LatestVersion);
                        }
                    }

                    return null;
                });
            });
        }

        public void OnConflictDataChanged(object sender, EventArgs e)
        {
            lock (this.disposeLock)
            {
                if (!this.disposed)
                {
                    this.Invalidate();
                }
            }
        }

        public void OnSyntaxNodeChanged(object sender, EventArgs e)
        {
            lock (this.disposeLock)
            {
                if (!this.disposed)
                {
                    this.Invalidate();
                }
            }
        }
    }
}