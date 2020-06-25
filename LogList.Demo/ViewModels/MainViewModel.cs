using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using DynamicData;
using LogList.Control;
using ReactiveUI;

namespace LogList.Demo.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly Subject<MyLogItem> _itemAddedSubject = new Subject<MyLogItem>();

        private bool _autoScroll;
        private string _filter;

        private int _id;
        private readonly string[] _poem;

        public MainViewModel()
        {
            _poem = File.ReadAllLines("poem.txt");

            var r           = new Random();
            var itemsSource = new SourceList<MyLogItem>();

            var filter = this.WhenAnyValue(x => x.Filter)
                             .Select(ft => ft?.Trim())
                             .Select(rq => string.IsNullOrWhiteSpace(rq)
                                         ? (Func<MyLogItem, bool>) (i => true)
                                         : (Func<MyLogItem, bool>) (i => i.ToString().Contains(rq) || i.Text.ToLower().Contains(rq)));

            itemsSource.Connect()
                       .Filter(filter)
                       .Cast(x => (ILogItem) x)
                       .Bind(out var items)
                       .Subscribe();

            Items = items;

            var sources = 2;
            var inserter = Observable.Interval(TimeSpan.FromMilliseconds(150))
                                     .ObserveOn(TaskPoolScheduler.Default)
                                     .Select(_ => GetMyLogItem(r, sources))
                                      //.Do(x => Console.WriteLine(x))
                                     .Do(i => itemsSource.Add(i));

            var demoData = Enumerable.Range(0, 40)
                                     .Select(i => GetMyLogItem(r, sources));

            itemsSource.AddRange(demoData);

            inserter.Subscribe(_itemAddedSubject);
        }

        public ReadOnlyObservableCollection<ILogItem> Items { get; set; }

        public IObservable<ILogItem> ItemAdded => _itemAddedSubject;

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

        private MyLogItem GetMyLogItem(Random r, int sources)
        {
            var id = Interlocked.Increment(ref _id);
            return new MyLogItem(id,
                                 DateTime.Now.AddDays(20),
                                 r.Next(sources),
                                 _poem[id % _poem.Length]);
        }
    }
}