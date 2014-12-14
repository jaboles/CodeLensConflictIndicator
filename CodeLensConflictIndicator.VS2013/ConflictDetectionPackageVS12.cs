using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace CodeLens.ConflictIndicator
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid("423B29A3-0BA9-4417-AE88-AD6071A8162A")]
    [ProvideAutoLoad("E13EEDEF-B531-4afe-9725-28A69FA4F896")] // Load when Microsoft.VisualStudio.TeamFoundation.dll loads.
    [CLSCompliant(false)]
    public sealed class ConflictDetectionPackageVS12 : Package
    {
        protected override void Initialize()
        {
            base.Initialize();

            ConflictDataPointProvider.Initialize(this);
        }
    }
}
