using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;
using System.Linq;
using System.Windows.Threading;
using System.Collections.Generic;

namespace CodeLens.ConflictIndicator
{
    /// <summary>
    /// Adornment class that draws a square box in the top right hand corner of the viewport
    /// </summary>
    class ConflictPopupAdornment
    {
        private IWpfTextView _view;
        private IAdornmentLayer _adornmentLayer;
        private EditingSession editingSession;
        private ConflictPopup control;

        /// <summary>
        /// Creates a square image and attaches an event handler to the layout changed event that
        /// adds the the square in the upper right-hand corner of the TextView via the adornment layer
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        public ConflictPopupAdornment(IWpfTextView view)
        {
            _view = view;
            _adornmentLayer = view.GetAdornmentLayer("ConflictPopupAdornment");

            string filePath = _view.GetDocumentFullPath();
            if (filePath != null)
            {
                this.editingSession = EditingSessionFactory.WaitForSession(_view.GetDocumentFullPath());
                editingSession.ConflictDataChanged += editingSession_ConflictDataChanged;
            }

            _view.ViewportHeightChanged += delegate { this.onSizeChange(); };
            _view.ViewportWidthChanged += delegate { this.onSizeChange(); };
        }

        public void onSizeChange()
        {
            //clear the adornment layer of previous adornments
            /*_adornmentLayer.RemoveAllAdornments();

            //Place the image in the top right hand corner of the Viewport
            Canvas.SetLeft(_image, _view.ViewportRight - 60);
            Canvas.SetTop(_image, _view.ViewportTop + 30);

            //add the image to the adornment layer and make it relative to the viewport
            _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _image, null);*/
        }

        void editingSession_ConflictDataChanged(object sender, ConflictDataChangedEventArgs e)
        {
            IEnumerable<ConflictInfo> conflicts = e.NewConflicts;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (conflicts != null && conflicts.Any())
                {
                    ConflictPopupViewModel controlViewModel = null;

                    if (control == null)
                    {
                        control = new ConflictPopup();
                        controlViewModel = new ConflictPopupViewModel();
                        control.DataContext = controlViewModel;
                    }
                    else
                    {
                        controlViewModel = (ConflictPopupViewModel)control.DataContext;
                    }

                    controlViewModel.ConflictViewModel.LatestVersion = e.LatestVersion;
                    controlViewModel.ConflictViewModel.FilePath = _view.GetDocumentFullPath();

                    _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, control, null);
                }
                else
                {
                    _adornmentLayer.RemoveAllAdornments();
                }
            });
        }
    }
}
