using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using DynamicData;
using LogList.Control;
using ReactiveUI;

namespace LogList.Demo.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private int _id;

        public MainViewModel()
        {
            var itemsSource = new SourceList<MyLogItem>();

            itemsSource.Connect()
                       .Cast(x => (ILogItem)x)
                       .Bind(out var items)
                       .Subscribe();

            Items = new ListViewModel(items);

            // Observable.Interval(TimeSpan.FromMilliseconds(1000))
            //           .ObserveOn(TaskPoolScheduler.Default)
            //           .Subscribe(_ => itemsSource.Add(new MyLogItem(Interlocked.Increment(ref _id), DateTime.Now)));
            
            itemsSource.AddRange(Enumerable.Range(0, 50).Select(i => new MyLogItem(i, DateTime.Now.AddMinutes(i))));
        }

        public ListViewModel Items { get; set; }
    }
}