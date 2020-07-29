using System;
using System.Collections.Generic;
using LogList.Control.Manipulation;
using LogList.Control.Manipulation.Implementations;

namespace LogList.Control
{
    public interface ILogViewSource
    {
        IObservable<PresentationRequest> PresentationRequests { get; }
        IObservable<int>                 FilteredSetSize      { get; }
        IList<LogRecord>                 Present(ViewWindow Window);
    }

    public class PresentationRequest
    {
        public PresentationRequest(ViewWindow Window, bool AnimateTransitions)
        {
            this.Window             = Window;
            this.AnimateTransitions = AnimateTransitions;
        }

        public ViewWindow Window             { get; }
        public bool       AnimateTransitions { get; }

        public override string ToString()
        {
            return $"Present {Window} (Animate = {AnimateTransitions})";
        }
    }

    public interface ILogView
    {
        public bool       AnimateTransitions { get; }
        public ViewWindow Window             { get; }
        IList<LogRecord> VisibleItems       { get; }
    }
}