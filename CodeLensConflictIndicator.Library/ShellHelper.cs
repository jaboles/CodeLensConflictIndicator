using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Diagnostics;

namespace CodeLens.ConflictIndicator
{
    [CLSCompliant(false)]
    public static class ShellHelper
    {
        public static IVsWindowFrame GetActiveWindowFrame(IServiceProvider serviceProvider)
        {
            Debug.Assert(serviceProvider != null);

            IVsMonitorSelection monitorSelection = (IVsMonitorSelection)serviceProvider.GetService(typeof(SVsShellMonitorSelection));

            if (monitorSelection != null)
            {
                object pvar = null;
                if (ErrorHandler.Succeeded(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out pvar)))
                {
                    IVsWindowFrame activeFrame = pvar as IVsWindowFrame;

                    if (activeFrame != null)
                    {
                        return activeFrame;
                    }
                }
            }

            return null;
        }

        public static ITextBuffer GetTextBuffer(IServiceProvider serviceProvider, IVsTextView textView)
        {
            IComponentModel componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            IVsTextLines textLines;
            if (textView.GetBuffer(out textLines) == VSConstants.S_OK)
            {
                IVsTextBuffer buffer = textLines as IVsTextBuffer;
                if (buffer != null)
                {
                    ITextBuffer textBuffer = editorAdaptersFactoryService.GetDataBuffer(buffer);
                    return textBuffer;
                }
            }

            return null;
        }

        public static IWpfTextView GetWpfTextView(IVsTextView vTextView)
        {
            IWpfTextView view = null;
            IVsUserData userData = vTextView as IVsUserData;

            if (null != userData)
            {
                IWpfTextViewHost viewHost;
                object holder;
                Guid guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
                if (userData.GetData(ref guidViewHost, out holder) == VSConstants.S_OK)
                {
                    viewHost = (IWpfTextViewHost)holder;
                    view = viewHost.TextView;
                }
            }

            return view;
        }
    }
}
