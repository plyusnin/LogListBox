using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace LogList.Control.Manipulation.Implementations
{
    public class ListViewModel<TItem> : ReactiveObject, IListDataViewModel<TItem>, ILogViewSource, IDisposable
        where TItem : ILogItem
    {
        private readonly CompositeDisposable _cleanUp = new CompositeDisposable();
        private readonly Subject<int> _filteredSetSize;
        private readonly object _locker = new object();

        private readonly List<LogRecord<TItem>> _original = new List<LogRecord<TItem>>();

        private readonly Subject<PresentationRequest> _presentationRequests;

        private IFilter<TItem> _filter = Filters.Empty<TItem>();
        private List<LogRecord<TItem>> _filtered = new List<LogRecord<TItem>>();
        private ViewWindow _window = new ViewWindow(0, 16);

        public ListViewModel()
        {
            _presentationRequests = new Subject<PresentationRequest>().DisposeWith(_cleanUp);
            _filteredSetSize      = new Subject<int>().DisposeWith(_cleanUp);

            var filteredSetSize = _filteredSetSize.DistinctUntilChanged()
                                                  .Replay(1);

            FilteredSetSize = filteredSetSize;

            filteredSetSize.Connect().DisposeWith(_cleanUp);
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }

        public void ApplyFilter(
            IFilter<TItem> Filter, IFilterScrollingBehavior<TItem> ScrollingBehavior, bool AnimateTransition)
        {
            lock (_locker)
            {
                var newFilter = Filter ?? Filters.Empty<TItem>();
                var input     = newFilter.IsSubFilterFor(_filter) ? _filtered : _original;
                _filter = newFilter;

                var oldFiltered = _filtered;
                _filtered = input.AsParallel().AsOrdered()
                                 .Where(record => _filter.Check(record.Item))
                                 .ToList();

                var offset = ScrollingBehavior.GetOffset(oldFiltered, _filtered, _window);

                _filteredSetSize.OnNext(_filtered.Count);
                RequestForRefresh(new ViewWindow(offset, _window.Size), AnimateTransition);
            }
        }

        public void Edit(
            Action<ILogEditor<TItem>> EditAction, IAppendScrollingBehavior<TItem> ScrollingBehavior,
            bool AnimateTransition)
        {
            lock (_locker)
            {
                var editor = new Editor(this, ScrollingBehavior);
                EditAction(editor);

                _filteredSetSize.OnNext(_filtered.Count);

                if (editor.ViewChanged) RequestForRefresh(editor.WindowAfterEdit, AnimateTransition);
            }
        }

        public IObservable<PresentationRequest> PresentationRequests => _presentationRequests;
        public IObservable<int>                 FilteredSetSize      { get; }

        public IList<LogRecord> Present(ViewWindow Window)
        {
            lock (_locker)
            {
                var offset = Math.Min(Window.Offset, _filtered.Count);
                var count  = Math.Min(Window.Size,   _filtered.Count - offset);
                _window = Window;

                var range = _filtered.GetRange(offset, count);

                return range.Select(r => r.ForgetType()).ToList();
            }
        }

        private void RequestForRefresh(ViewWindow Window, bool WithAnimation)
        {
            _presentationRequests.OnNext(new PresentationRequest(Window, WithAnimation));
        }

        private int LastRecordNumber() => _original.Count > 0 ? _original[^1].Number : -1;

        public class Editor : ILogEditor<TItem>
        {
            private readonly ListViewModel<TItem> _parent;
            private readonly IAppendScrollingBehavior<TItem> _scrollingBehavior;
            public bool ViewChanged;

            public ViewWindow WindowAfterEdit;

            public Editor(ListViewModel<TItem> Parent, IAppendScrollingBehavior<TItem> ScrollingBehavior)
            {
                _parent            = Parent;
                _scrollingBehavior = ScrollingBehavior;

                WindowAfterEdit = _parent._window;
            }

            public void Append(TItem Item)
            {
                var number = _parent.LastRecordNumber() + 1;
                var record = new LogRecord<TItem>(number, Item);
                _parent._original.Add(record);
                if (_parent._filter.Check(Item))
                {
                    var insertionIndex = _parent._filtered.Count;
                    var offset         = _scrollingBehavior.GetOffset(new[] { record }, insertionIndex, WindowAfterEdit);
                    _parent._filtered.Add(record);

                    var invisibleChange = WindowAfterEdit.Offset == offset &&
                                          insertionIndex         > WindowAfterEdit.Offset + WindowAfterEdit.Size;

                    ViewChanged     |= !invisibleChange;
                    WindowAfterEdit =  new ViewWindow(offset, WindowAfterEdit.Size);
                }
            }

            public void Append(IList<TItem> Items)
            {
                var number  = _parent.LastRecordNumber() + 1;
                var records = Items.Select((it, i) => new LogRecord<TItem>(number + i, it)).ToList();
                _parent._original.AddRange(records);
                var filtered = records.Where(r => _parent._filter.Check(r.Item)).ToList();

                if (filtered.Count > 0)
                {
                    var insertionIndex = _parent._filtered.Count;
                    var offset         = _scrollingBehavior.GetOffset(filtered, insertionIndex, WindowAfterEdit);
                    _parent._filtered.AddRange(filtered);

                    var invisibleChange = WindowAfterEdit.Offset == offset &&
                                          insertionIndex         > WindowAfterEdit.Offset + WindowAfterEdit.Size;

                    ViewChanged     |= !invisibleChange;
                    WindowAfterEdit =  new ViewWindow(offset, WindowAfterEdit.Size);
                }
            }

            public void Insert(TItem Item)
            {
                // var invisibleChange = 
                //     (insertionIndex         < WindowAfterEdit.Offset && offset == WindowAfterEdit.Offset + 1) ||
                //     (WindowAfterEdit.Offset == offset                && )
                throw new NotImplementedException();
            }

            public void Clear()
            {
                _parent._original.Clear();
                _parent._filtered.Clear();

                _parent._original.TrimExcess();
                _parent._filtered.TrimExcess();

                WindowAfterEdit = new ViewWindow(0, WindowAfterEdit.Size);
                ViewChanged     = true;
            }
        }
    }
}