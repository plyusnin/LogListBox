using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Aggregation;
using ReactiveUI;

namespace LogList.Control
{
    public class HeightViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _cleanup = new CompositeDisposable();

        private readonly ObservableAsPropertyHelper<double> _listHeight;
        private readonly ObservableAsPropertyHelper<double> _scrollableMaximum;

        private double _listOffset;
        private double _viewportHeight;

        public HeightViewModel(IObservable<ISortedChangeSet<ILogItem, int>> Items, double ItemHeight = 25)
        {
            this.ItemHeight = ItemHeight;

            CountEx.Count(Items)
                   .Select(c => c * ItemHeight)
                   .ToProperty(this, x => x.ListHeight, out _listHeight)
                   .DisposeWith(_cleanup);

            PagingRequests = this.WhenAnyValue(x => x.ListOffset,
                                               x => x.ViewportHeight,
                                               (offset, height) => new { offset, height })
                                 .Select(x => new VirtualRequest(IndexFromOffset(x.offset), Count(x.offset, x.height)));

            this.WhenAnyValue(x => x.ListHeight,
                              x => x.ViewportHeight,
                              (l, v) => Math.Max(0, l - v))
                .ToProperty(this, x => x.ScrollableMaximum, out _scrollableMaximum);
        }


        public IObservable<VirtualRequest> PagingRequests { get; }

        public double ItemHeight { get; }

        public double ListHeight        => _listHeight.Value;
        public double ScrollableMaximum => _scrollableMaximum.Value;

        public double ListOffset
        {
            get => _listOffset;
            set => this.RaiseAndSetIfChanged(ref _listOffset, value);
        }

        public double ViewportHeight
        {
            get => _viewportHeight;
            set => this.RaiseAndSetIfChanged(ref _viewportHeight, value);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }

        private int Count(double StartingFrom, double Height)
        {
            return (int) Math.Ceiling(Height / ItemHeight);
        }

        public int IndexFromOffset(double Offset)
        {
            return (int) Math.Floor(Offset / ItemHeight);
        }

        public double OffsetFromIndex(double Index)
        {
            return ItemHeight * Index;
        }
    }
}