using Microsoft.TeamFoundation.MVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;

namespace CodeLens.ConflictIndicator
{
    public class ConflictViewModel : ViewModelBase
    {
        private ICommand viewChangesetCommand;
        private ICommand emailOwnerCommand;
        private ICommand compareLatestCommand;
        private ICommand compareWithWorkspaceCommand;
        private ICommand getLatestCommand;

        private VersionInfo latestVersion;

        private ISCCService sccService;

        public ConflictViewModel(ISCCService sccService)
        {
            this.sccService = sccService;
            this.PropertyChanged += this.OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
        }

        public VersionInfo LatestVersion
        {
            get
            {
                return this.latestVersion;
            }

            set
            {
                this.latestVersion = value;
                this.RaisePropertyChanged("LatestVersion");
            }
        }

        public string FilePath
        {
            get;
            set;
        }

        public string LatestVersionComment
        {
            get
            {
                if (this.LatestVersion != null)
                {
                    if (!string.IsNullOrEmpty(this.LatestVersion.Comment))
                    {
                        return this.LatestVersion.Comment;
                    }
                }

                return Strings.EmptyComment;
            }
        }

        public ICommand ViewChangesetCommand
        {
            get
            {
                if (this.viewChangesetCommand == null)
                {
                    this.viewChangesetCommand = new RelayCommand((param) =>
                    {
                        if (this.LatestVersion != null)
                        {
                            sccService.NavigateToVersion(this.LatestVersion.Id);
                        }
                    });
                }

                return this.viewChangesetCommand;
            }
        }

        public ICommand EmailOwnerCommand
        {
            get
            {
                if (this.emailOwnerCommand == null)
                {
                    this.emailOwnerCommand = new RelayCommand(async (param) =>
                    {
                        if (this.LatestVersion != null)
                        {
                            await Task.Run(() =>
                            {
                                string email = sccService.GetEmail(this.LatestVersion.OwnerUniqueName);
                                email = HttpUtility.UrlEncode(email);
                                string emailUrl = string.Format("mailto:{0}", email);
                                Process.Start(emailUrl);
                            });

                            // TODO CodeLensIndicatorCommands.HideDetails.Execute(null);
                            CommandManager.InvalidateRequerySuggested();
                        }
                    });
                }

                return this.emailOwnerCommand;
            }
        }

        public ICommand CompareLatestCommand
        {
            get
            {
                if (this.compareLatestCommand == null)
                {
                    this.compareLatestCommand = new RelayCommand((param) =>
                    {
                        string item = this.FilePath;
                        sccService.CompareWithLatest(item);
                    });
                }

                return this.compareLatestCommand;
            }
        }

        public ICommand CompareWithWorkspaceCommand
        {
            get
            {
                if (this.compareWithWorkspaceCommand == null)
                {
                    this.compareWithWorkspaceCommand = new RelayCommand((param) =>
                    {
                        string item = this.FilePath;
                        sccService.CompareWithWorkspace(item);
                    });
                }

                return this.compareWithWorkspaceCommand;
            }
        }

        public ICommand GetLatestCommand
        {
            get
            {
                if (this.getLatestCommand == null)
                {
                    this.getLatestCommand = new RelayCommand((param) =>
                    {
                        string item = this.FilePath;
                        sccService.GetLatest(item);
                    });
                }

                return this.getLatestCommand;
            }
        }

        /*private static string GenerateEmailString(string type, object id, string email, string hyperlink, string title)
        {
            email = HttpUtility.UrlPathEncode(email);
            string body = HttpUtility.UrlEncode(hyperlink);
            string subject;
            const int MAX_COMMENT_LENGTH_IN_SUBJECT = 60;
            // change all multiple white spaces into single white space
            string comment = new SingleLineTextConverter().Convert(title, typeof(string), null, CultureInfo.CurrentCulture) as string;

            if (!string.IsNullOrWhiteSpace(comment))
            {
                string ellipsis, displayComment;
                if (comment.Length > MAX_COMMENT_LENGTH_IN_SUBJECT)
                {
                    ellipsis = Strings.EllipsisString;
                    if (char.IsWhiteSpace(comment[MAX_COMMENT_LENGTH_IN_SUBJECT]))
                    {
                        // the max length ends on a space, so we can just cut it there
                        displayComment = comment.Substring(0, MAX_COMMENT_LENGTH_IN_SUBJECT);
                    }
                    else
                    {
                        // make length does not end on space, we'll need to find the last space then
                        comment = comment.Substring(0, MAX_COMMENT_LENGTH_IN_SUBJECT);
                        int lastSpace = comment.LastIndexOf(' ');
                        if (lastSpace <= 0)
                        {
                            // no space found, just take the entire length then
                            lastSpace = MAX_COMMENT_LENGTH_IN_SUBJECT;
                        }

                        displayComment = comment.Substring(0, Math.Min(lastSpace, MAX_COMMENT_LENGTH_IN_SUBJECT));
                    }
                }
                else
                {
                    ellipsis = string.Empty;
                    displayComment = comment;
                }

                subject = HttpUtility.UrlEncode(string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings.MailToStringWithComments, type, id, displayComment, ellipsis)).Replace("+", "%20");
            }
            else
            {
                subject = string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings.MailToStringNoComments, type, id);
            }

            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Strings.BaseMailToString, email, body, subject);
        }*/

    }
}
