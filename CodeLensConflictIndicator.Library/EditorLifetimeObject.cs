using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace CodeLens.ConflictIndicator
{
    public class EditorLifetimeObject
    {
        private ITextView textView;
        private ITextBuffer textBuffer;

        public EditorLifetimeObject(ITextBuffer textBuffer, ITextView textView)
        {
            if (textView == null) throw new ArgumentNullException("textView");
            if (textBuffer == null) throw new ArgumentNullException("textBuffer");

            this.textView = textView;
            this.textBuffer = textBuffer;

            this.textView.Closed += this.OnTextViewClosed;
            this.textBuffer.Changed += this.OnTextBufferChanged;
        }

        protected ITextView TextView
        {
            get { return this.textView; }
        }

        protected ITextBuffer TextBuffer
        {
            get { return this.textBuffer; }
        }

        protected virtual void OnTextViewClosed(object sender, EventArgs e)
        {
            this.textView.Closed -= this.OnTextViewClosed;
            this.textBuffer.Changed -= this.OnTextBufferChanged;
        }

        protected virtual void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
        }
    }
}
