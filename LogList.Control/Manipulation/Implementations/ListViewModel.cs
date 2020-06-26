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
        private readonly Subject<int> _filteredSetSize;
        private readonly object _locker = new object();

        private readonly List<TItem> _original = new List<TItem>();

        private readonly Subject<PresentationRequest> _presentationRequests;

        private readonly CompositeDisposable _cleanUp = new CompositeDisposable();

        private IFilter<TItem> _filter = Filters.Empty<TItem>();
        private List<TItem> _filtered = new List<TItem>();
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
                _filtered = input.AsParallel()
                                 .Where(Item => _filter.Check(Item))
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

        public IList<ILogItem> Present(ViewWindow Window)
        {
            lock (_locker)
            {
                var offset = Math.Min(Window.Offset, _filtered.Count);
                var count  = Math.Min(Window.Size,   _filtered.Count - offset);
                _window = Window;

                var range = _filtered.GetRange(offset, count);

                return range.OfType<ILogItem>().ToList();
            }
        }

        private void RequestForRefresh(ViewWindow Window, bool WithAnimation)
        {
            _presentationRequests.OnNext(new PresentationRequest(Window, WithAnimation));
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
                _parent._original.Add(Item);
                if (_parent._filter.Check(Item))
                {
                    var insertionIndex = _parent._filtered.Count;
                    var offset         = _scrollingBehavior.GetOffset(new[] { Item }, insertionIndex, WindowAfterEdit);
                    _parent._filtered.Add(Item);

                    var invisibleChange = WindowAfterEdit.Offset == offset &&
                                          insertionIndex         > WindowAfterEdit.Offset + WindowAfterEdit.Size;

                    ViewChanged     |= !invisibleChange;
                    WindowAfterEdit =  new ViewWindow(offset, WindowAfterEdit.Size);
                }
            }

            public void Append(IList<TItem> Items)
            {
                _parent._original.AddRange(Items);
                var filtered = Items.Where(_parent._filter.Check).ToList();
                
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
                throw new NotImplementedException();
            }
        }
    }
}