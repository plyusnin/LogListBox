using System;
using System.Collections.ObjectModel;
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
        private string _filter;

        public MainViewModel()
        {
            var r           = new Random();
            var itemsSource = new SourceList<MyLogItem>();

            var filter = this.WhenAnyValue(x => x.Filter)
                             .Select(ft => ft?.Trim())
                             .Select(rq => string.IsNullOrWhiteSpace(rq)
                                         ? (Func<MyLogItem, bool>) (i => true)
                                         : (Func<MyLogItem, bool>) (i => i.ToString().Contains(rq)));

            itemsSource.Connect()
                       .Filter(filter)
                       .Cast(x => (ILogItem) x)
                       .Bind(out var items)
                       .Subscribe();

            Items = items;

            var sources = 2;
            var inserter = Observable.Interval(TimeSpan.FromMilliseconds(10))
                                     .ObserveOn(TaskPoolScheduler.Default)
                                     .Select(_ => new MyLogItem(Interlocked.Increment(ref _id),
                                                                DateTime.Now.AddDays(100),
                                                                r.Next(sources)))
                                     //.Do(x => Console.WriteLine(x))
                                     .Do(i => itemsSource.Add(i));

            var demoData = Enumerable.Range(0, 2000)
                                     .Select(i => new MyLogItem(Interlocked.Increment(ref _id),
                                                                DateTime.Now.AddMinutes(i),
                                                                r.Next(sources)));

            itemsSource.AddRange(demoData);
            
            //inserter.Subscribe();
        }

        private int _id;

        public ReadOnlyObservableCollection<ILogItem> Items { get; set; }

        public string Filter
        {
            get => _filter;
            set => this.RaiseAndSetIfChanged(ref _filter, value);
        }
    }
}