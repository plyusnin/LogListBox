using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
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

            // Observable.Interval(TimeSpan.FromMilliseconds(40))
            //           .ObserveOn(TaskPoolScheduler.Default)
            //           .Subscribe(_ => itemsSource.Add(new MyLogItem(Interlocked.Increment(ref _id), DateTime.Now)));
            
            itemsSource.AddRange(Enumerable.Range(0, 100).Select(i => new MyLogItem(Interlocked.Increment(ref _id), DateTime.Now.AddMinutes(i))));

        }

        public ListViewModel Items { get; set; }
    }
}