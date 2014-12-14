using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Timers;

namespace CodeLens.ConflictIndicator
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class EditingSessionFactory : IWpfTextViewCreationListener
    {
        private static readonly TimeSpan TfsQueryHistoryUpdateInterval = TimeSpan.FromMinutes(2);

        private static readonly IDictionary<string, EditingSession> sessions = new Dictionary<string, EditingSession>();
        private static readonly System.Timers.Timer tfsQueryHistoryTimer = InitializeTimer();
        private static IServiceProvider serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            EditingSessionFactory.serviceProvider = serviceProvider;
        }

        public static EditingSession WaitForSession(string filePath)
        {
            lock (sessions)
            {
                if (sessions.ContainsKey(filePath))
                {
                    return sessions[filePath];
                }

                System.Threading.SpinWait.SpinUntil(() => sessions.ContainsKey(filePath));
            }

            return sessions[filePath];
        }

        private static Timer InitializeTimer()
        {
            Timer t = new Timer(TfsQueryHistoryUpdateInterval.TotalMilliseconds);
            t.Elapsed += tfsQueryHistoryTimer_Elapsed;
            t.Enabled = true;
            return t;
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            lock (sessions)
            {
                string filePath = textView.GetDocumentFullPath();
                if (filePath != null)
                {
                    ITextBuffer textBuffer = textView.TextBuffer;
                    if (!sessions.ContainsKey(filePath))
                    {
                        EditingSession editingSession = new EditingSession(filePath, textBuffer, textView);
                        sessions.Add(filePath, editingSession);

                        EventHandler textViewClosed = null;
                        textViewClosed = (o, e) =>
                        {
                            textView.Closed -= textViewClosed;
                            lock (sessions)
                            {
                                sessions.Remove(filePath);
                            }
                        };

                        textView.Closed += textViewClosed;
                    }
                }
            }
        }

        private static async void tfsQueryHistoryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            IVsWindowFrame vsWindowFrame = ShellHelper.GetActiveWindowFrame(serviceProvider);
            if (vsWindowFrame != null)
            {
                object pvar = null;
                vsWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out pvar);
                string fullPath = pvar as string;
                if (fullPath != null)
                {
                    EditingSession activeSession = EditingSessionFactory.WaitForSession(fullPath);
                    if (activeSession != null)
                    {
                        tfsQueryHistoryTimer.Enabled = false;
                        await activeSession.RecalculateConflicts(ForceCheckForNewVersion.LatestVersion);
                        tfsQueryHistoryTimer.Enabled = true;
                    }
                }
            }
        }
    }
}
