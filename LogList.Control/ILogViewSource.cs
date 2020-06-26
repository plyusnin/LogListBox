using System;
using System.Collections.Generic;
using LogList.Control.Manipulation;

namespace LogList.Control
{
    public interface ILogViewSource
    {
        IObservable<PresentationRequest> PresentationRequests { get; }
        IObservable<int>                 FilteredSetSize      { get; }
        IList<ILogItem>                  Present(ViewWindow Window);
    }

    public class PresentationRequest
    {
        public PresentationRequest(ViewWindow Window, bool AnimateTransitions)
        {
            this.Window             = Window;
            this.AnimateTransitions = AnimateTransitions;
        }

        public override string ToString()
        {
            return $"Present {Window} (Animate = {AnimateTransitions})";
        }

        public ViewWindow Window             { get; }
        public bool       AnimateTransitions { get; }
    }

    public interface ILogView
    {
        public bool       AnimateTransitions { get; }
        public ViewWindow Window             { get; }
        IList<ILogItem>   VisibleItems       { get; }
    }
}