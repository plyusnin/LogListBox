using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
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

        private readonly Subject<ImmutableHashSet<ILogItem>> _selectionSubject =
            new Subject<ImmutableHashSet<ILogItem>>();

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

            SelectedItems = new ObservableCollection<LogRecord<TItem>>();
            SelectedItems.CollectionChanged += (Sender, Args) =>
            {
                var logItems = SelectedItems.Select(i => (ILogItem) i.Item).ToImmutableHashSet();
                _selectionSubject.OnNext(logItems);
            };
        }

        private ObservableCollection<LogRecord<TItem>> SelectedItems { get; }

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

        public IObservable<ImmutableHashSet<ILogItem>> Selection => _selectionSubject;

        public void ToggleSelection(LogRecord Record)
        {
            if (SelectedItems.Contains(Record.RecallType<TItem>()))
                SelectedItems.Remove(Record.RecallType<TItem>());
            else
                SelectedItems.Add(Record.RecallType<TItem>());
        }

        public void Select(LogRecord Record)
        {
            SelectedItems.Clear();
            SelectedItems.Add(Record.RecallType<TItem>());
        }

        public void ExtendSelection(LogRecord ToRecord)
        {
            if (SelectedItems.Count == 0)
                return;

            var record = ToRecord.RecallType<TItem>();

            var selectedMinimum = SelectedItems.First();
            var selectedMaximum = SelectedItems.Last();
            foreach (var item in SelectedItems)
            {
                if (item.Number < selectedMinimum.Number) selectedMinimum = item;
                if (item.Number > selectedMinimum.Number) selectedMaximum = item;
            }

            if (ToRecord.Number < selectedMinimum.Number)
            {
                var from = _filtered.BinarySearch(
                    record, BinarySearchExtensions.ItemNotFoundBehavior.ReturnClosestTimeIndex);
                SelectedItems.AddRange(_filtered.Skip(from).TakeWhile(r => r.Number < selectedMinimum.Number));
            }
            else if (ToRecord.Number > selectedMinimum.Number)
            {
                var from = _filtered.BinarySearch(selectedMaximum,
                                                  BinarySearchExtensions.ItemNotFoundBehavior.ReturnClosestTimeIndex);
                SelectedItems.AddRange(_filtered.Skip(from).TakeWhile(r => r.Number <= ToRecord.Number));
            }
        }

        private void RequestForRefresh(ViewWindow Window, bool WithAnimation)
        {
            _presentationRequests.OnNext(new PresentationRequest(Window, WithAnimation));
        }

        private int LastRecordNumber()
        {
            return _original.Count > 0 ? _original[^1].Number : -1;
        }

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
                    var offset = _scrollingBehavior.GetOffset(new[] { record }, insertionIndex, WindowAfterEdit);
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