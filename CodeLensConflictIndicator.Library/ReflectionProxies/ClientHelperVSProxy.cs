using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TeamFoundation.VersionControl.DiffMerge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeLens.ConflictIndicator
{
    public class ClientHelperVSProxy : ReflectionProxy
    {
        private static ClientHelperVSProxy s_instance;

        private ClientHelperVSProxy()
            : base(typeof(DifferenceProvider).Assembly, "Microsoft.VisualStudio.TeamFoundation.VersionControl.PendingChanges.PendingChangesPageViewModelUtilsVS")
        {
        }

        private ClientHelperVSProxy(object privateObject)
            : base(privateObject)
        {
        }

        public static ClientHelperVSProxy Instance
        {
            get
            {
                if (s_instance == null)
                {
                    ClientHelperVSProxy typeProxy = new ClientHelperVSProxy();
                    object privateInstance = new ClientHelperVSProxy().InvokeGetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic, typeProxy.Type.BaseType);
                    s_instance = new ClientHelperVSProxy(privateInstance);
                }

                return s_instance;
            }
        }

        public void CompareWithLatestVersion(IList<PendingChange> changes)
        {
            this.InvokeMethod(BindingFlags.Instance | BindingFlags.NonPublic, "CompareWithLatestVersion", new object[] { changes }, new Type[] { typeof(IList<PendingChange>) });
        }

        public void CompareWithWorkspaceVersion(IList<PendingChange> changes, Workspace workspace)
        {
            this.InvokeMethod(BindingFlags.Instance | BindingFlags.NonPublic, "CompareWithWorkspaceVersion", new object[] { workspace, changes }, new Type[] { typeof(Workspace), typeof(IList<PendingChange>) });
        }
    }
}
