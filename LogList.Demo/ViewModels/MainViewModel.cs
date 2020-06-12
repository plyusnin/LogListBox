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
        private string _filter;
        private int _id;

        public MainViewModel()
        {
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

            Items = new ListViewModel(items);

            var inserter = Observable.Interval(TimeSpan.FromMilliseconds(3000))
                                     .ObserveOn(TaskPoolScheduler.Default)
                                     .Do(_ => itemsSource.Add(
                                             new MyLogItem(Interlocked.Increment(ref _id), DateTime.Now)));

            var demoData = Enumerable.Range(0, 10000)
                                     .Select(i => new MyLogItem(Interlocked.Increment(ref _id),
                                                                DateTime.Now.AddMinutes(i)));

            inserter.Subscribe();
            itemsSource.AddRange(demoData);
        }

        public ListViewModel Items { get; set; }

        public string Filter
        {
            get => _filter;
            set => this.RaiseAndSetIfChanged(ref _filter, value);
        }
    }
}