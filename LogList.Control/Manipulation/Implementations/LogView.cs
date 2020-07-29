using System.Collections.Generic;

namespace LogList.Control.Manipulation.Implementations
{
    public class LogView : ILogView
    {
        public LogView(ViewWindow Window, IList<LogRecord> VisibleItems, bool AnimateTransitions)
        {
            this.VisibleItems       = VisibleItems;
            this.AnimateTransitions = AnimateTransitions;
            this.Window             = Window;
        }

        public ViewWindow       Window             { get; }
        public bool             AnimateTransitions { get; }
        public IList<LogRecord> VisibleItems       { get; }
    }
}