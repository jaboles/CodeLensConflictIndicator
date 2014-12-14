using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace CodeLens.ConflictIndicator
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid("7603F33D-F57C-4B7E-8CE5-29EC88C23AE2")]
    [CLSCompliant(false)]
    public sealed class ConflictIndicatorPackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();

            FileComparerService.Instance.Initialize(this);
            EditingSessionFactory.Initialize(this);
        }
    }
}
