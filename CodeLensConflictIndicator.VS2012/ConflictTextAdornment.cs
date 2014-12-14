using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System;
using System.Windows.Threading;
using System.Windows;
using Microsoft.VisualStudio.Text.Classification;

namespace CodeLens.ConflictIndicator
{
    ///<summary>
    ///TextAdornment1 places red boxes behind all the "A"s in the editor window
    ///</summary>
    public class ConflictTextAdornment
    {
        public const double Opacity = 0.2;

        IAdornmentLayer _layer;
        IWpfTextView _view;
        Brush _brush;
        Pen _pen;
        private EditingSession editingSession;
        private IEnumerable<ConflictInfo> conflictInfo;
        
        public ConflictTextAdornment(IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            _view = view;
            _layer = view.GetAdornmentLayer("ConflictTextAdornment");

            //Listen to any event that changes the layout (text changes, scrolling, etc)
            _view.LayoutChanged += OnLayoutChanged;

            SolidColorBrush b = (SolidColorBrush)editorFormatMap.GetProperties(ConflictRegionFormatDefinition.Name)[EditorFormatDefinition.BackgroundBrushId]; ;
            _brush = new SolidColorBrush(Color.FromArgb((byte)(Opacity * 255), b.Color.R, b.Color.G, b.Color.B));
            Brush penBrush = (SolidColorBrush)editorFormatMap.GetProperties(ConflictRegionFormatDefinition.Name)[EditorFormatDefinition.ForegroundBrushId]; ;
            penBrush.Freeze();
            _pen = new Pen(penBrush, 1);

            string filePath = _view.GetDocumentFullPath();
            if (filePath != null)
            {
                this.editingSession = EditingSessionFactory.WaitForSession(_view.GetDocumentFullPath());
                editingSession.ConflictDataChanged += editingSession_ConflictDataChanged;
            }
        }

        public void Dispose()
        {
            editingSession.ConflictDataChanged -= editingSession_ConflictDataChanged;
        }

        /// <summary>
        /// On layout change add the adornment to any reformatted lines
        /// </summary>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            RedrawAdornments();
        }

        private void RedrawAdornments()
        {
            if (this.conflictInfo != null)
            {
                _layer.RemoveAllAdornments();
                foreach (ConflictInfo ci in this.conflictInfo)
                {
                    CreateVisuals(ci.LineSpan);
                }
            }
        }

        /// <summary>
        /// Within the given line add the scarlet box behind the a
        /// </summary>
        private void CreateVisuals(Span lineSpan)
        {
            //grab a reference to the lines in the current TextView 
            IWpfTextViewLineCollection textViewLines = _view.TextViewLines;

            SnapshotPoint start = textViewLines.ElementAt(lineSpan.Start).Start;
            SnapshotPoint end = textViewLines.ElementAt(lineSpan.End).Start;
            SnapshotSpan span = new SnapshotSpan(start, end.Position - start.Position);
            Geometry g = textViewLines.GetMarkerGeometry(span);
            if (g != null)
            {
                GeometryDrawing drawing = new GeometryDrawing(_brush, _pen, g);
                drawing.Freeze();

                DrawingImage drawingImage = new DrawingImage(drawing);
                drawingImage.Freeze();

                Image image = new Image();
                image.Source = drawingImage;

                //Align the image with the top of the bounds of the text geometry
                Canvas.SetLeft(image, g.Bounds.Left);
                Canvas.SetTop(image, g.Bounds.Top);

                _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
            }
        }

        private void editingSession_ConflictDataChanged(object sender, ConflictDataChangedEventArgs e)
        {
            this.conflictInfo = e.NewConflicts;

            Application.Current.Dispatcher.Invoke(() => 
            {
                RedrawAdornments();
            });
        }
    }
}
