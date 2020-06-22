using System;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Threading;

namespace LogList.Control
{
    public static class DispatcherDistinctExtensions
    {
        public static IObservable<T> DistinctByDispatcher<T>(this IObservable<T> Source, DispatcherPriority Priority)
            where T : class
        {
            T val = default;
            return Source.Do(v => Interlocked.Exchange(ref val, v))
                         .ObserveOnDispatcher()
                         .Select(_ => val)
                         .DistinctUntilChanged();
        }
    }
}