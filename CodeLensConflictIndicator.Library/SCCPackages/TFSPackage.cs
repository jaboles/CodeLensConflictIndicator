using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace CodeLens.ConflictIndicator
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid("9289DFDD-543A-4F12-A2DD-6E681719BF49")]
    [ProvideAutoLoad("E13EEDEF-B531-4afe-9725-28A69FA4F896")] // Load when Microsoft.VisualStudio.TeamFoundation.dll loads.
    [CLSCompliant(false)]
    public sealed class TFSPackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();

            SCCServiceWrapper<TFSService>.Instance.Initialize(this);
        }
    }
}
