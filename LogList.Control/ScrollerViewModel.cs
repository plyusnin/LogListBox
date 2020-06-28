using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using LogList.Control.Manipulation;
using ReactiveUI;

namespace LogList.Control
{
    public class ScrollerViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _cleanup = new CompositeDisposable();

        private readonly ObservableAsPropertyHelper<double> _listHeight;
        private readonly ObservableAsPropertyHelper<double> _scrollableMaximum;
        private readonly ObservableAsPropertyHelper<double> _smallChange;

        private double _listOffset;
        private double _shift;
        private double _viewportHeight;

        public ScrollerViewModel(
            IObservable<int> SourceSize, IObservable<PresentationRequest> OuterRequests, double ItemHeight = 20)
        {
            this.ItemHeight = ItemHeight;

            SourceSize
               .Select(c => c * ItemHeight)
               .ToProperty(this, x => x.ListHeight, out _listHeight)
               .DisposeWith(_cleanup);

            bool scroll = true;
            
            var innerRequests = this.WhenAnyValue(x => x.ListOffset,
                                                  x => x.ViewportHeight,
                                                  (offset, height) => new { offset, height })
                                    .Select(x => new ViewWindow(IndexFromOffset(x.offset), Count(x.offset, x.height)))
                                    .Where(_ => scroll)
                                    .DistinctUntilChanged()
                                    .Select(w => new PresentationRequest(w, false));


            Requests = OuterRequests.ObserveOnDispatcher()
                                    .Do(_ => scroll = false)
                                    .Do(r => ListOffset = OffsetFromIndex(r.Window.Offset) + _shift)
                                    .Do(_ => scroll = true)
                                    .Merge(innerRequests);

            this.WhenAnyValue(x => x.ListHeight,
                              x => x.ViewportHeight,
                              (l, v) => Math.Max(0, l - v))
                .ToProperty(this, x => x.ScrollableMaximum, out _scrollableMaximum);

            this.WhenAnyValue(x => x.ListOffset)
                .Select(offset => offset - OffsetFromIndex(IndexFromOffset(offset)))
                .Subscribe(shift => _shift = shift);

            this.WhenAnyValue(x => x.ItemHeight)
                .Select(h => h * 3)
                .ToProperty(this, x => x.SmallChange, out _smallChange);
        }


        public IObservable<PresentationRequest> Requests { get; }

        public double ItemHeight { get; }

        public double ListHeight        => _listHeight.Value;
        public double ScrollableMaximum => _scrollableMaximum.Value;
        public double SmallChange       => _smallChange.Value;

        public double ListOffset
        {
            get => _listOffset;
            set => this.RaiseAndSetIfChanged(ref _listOffset, Math.Max(0, Math.Min(ScrollableMaximum, value)));
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

        private int Count(double StartingFrom, double AreaHeight)
        {
            return (int) Math.Ceiling(AreaHeight / ItemHeight) + 1;
        }

        public int IndexFromOffset(double Offset)
        {
            return (int) Math.Floor(Offset / ItemHeight);
        }

        public double OffsetFromIndex(int Index)
        {
            return ItemHeight * Index;
        }

        public double RelativeOffsetFromIndex(int IndexOnPage)
        {
            return OffsetFromIndex(IndexOnPage) - _shift;
        }
    }
}