using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using LogList.Control.Manipulation;
using LogList.Control.Manipulation.Implementations;
using ReactiveUI;

namespace LogList.Demo.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly string[] _poem;
        private bool _autoScroll;
        private string _filter;

        private int _id;

        public MainViewModel()
        {
            _poem = File.ReadAllLines("poem.txt");

            var r = new Random();
            Items = new ListViewModel<MyLogItem>();

            this.WhenAnyValue(x => x.Filter)
                .Select(ft => ft?.Trim())
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Select(rq => string.IsNullOrWhiteSpace(rq)
                            ? Filters.Empty<MyLogItem>()
                            : Filters.ByStringRequest<MyLogItem>(rq))
                .Subscribe(f => Items.ApplyFilter(f));


            var sources = 2;
            var demoData = Enumerable.Range(0, 8000000)
                                     .Select(_ => GetMyLogItem(r, sources, i => DateTime.Now.AddMilliseconds(i)))
                                     .ToList();

            var inserter = Observable.Interval(TimeSpan.FromMilliseconds(500))
                                     .ObserveOn(TaskPoolScheduler.Default)
                                     .Select(i => GetMyLogItem(r, sources, i => DateTime.Now.AddDays(40)))
                                      //.Do(x => Console.WriteLine(x))
                                     .Do(i => Items.Append(i, _autoScroll));

            Items.Append(demoData);

            inserter.Subscribe();
        }

        public ListViewModel<MyLogItem> Items { get; }

        public string Filter
        {
            get => _filter;
            set => this.RaiseAndSetIfChanged(ref _filter, value);
        }

        public bool AutoScroll
        {
            get => _autoScroll;
            set => this.RaiseAndSetIfChanged(ref _autoScroll, value);
        }

        private MyLogItem GetMyLogItem(Random r, int sources, Func<int, DateTime> DateFactory)
        {
            var id = Interlocked.Increment(ref _id);
            return new MyLogItem(id,
                                 DateFactory(id),
                                 r.Next(sources),
                                 _poem[id % _poem.Length]);
        }
    }
}