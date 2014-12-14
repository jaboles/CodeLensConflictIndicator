using Microsoft.TeamFoundation.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CodeLens.ConflictIndicator
{
    public class ConflictPopupViewModel : ViewModelBase
    {
        private ICommand expandCommand;
        private Visibility expanded = Visibility.Collapsed;

        public ConflictPopupViewModel()
        {
            this.ConflictViewModel = new ConflictViewModel();
        }

        public ConflictViewModel ConflictViewModel
        {
            get;
            private set;
        }

        public Visibility Expanded
        {
            get
            {
                return expanded;
            }

            set
            {
                this.expanded = value;
                this.RaisePropertyChanged("Expanded");
                this.RaisePropertyChanged("ExpandButtonVisibility");
            }
        }

        public Visibility ExpandButtonVisibility
        {
            get
            {
                return Expanded == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public ICommand ExpandCommand
        {
            get
            {
                if (this.expandCommand == null)
                {
                    this.expandCommand = new RelayCommand((param) => this.Expanded = Visibility.Visible);
                }

                return this.expandCommand;
            }
        }
    }
}
