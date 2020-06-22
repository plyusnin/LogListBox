using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;

namespace LogList.Control
{
    public partial class LogListBox : UserControl
    {
        private bool _animate;
        private ReadOnlyObservableCollection<ILogItem> _collection;
        private IListDataViewModel _dataViewModel;


        private VirtualRequest _pagingRequest;

        private List<ILogItem> _visibleItems = new List<ILogItem>();


        public LogListBox()
        {
            InitializeComponent();
        }

        private void Attach(ReadOnlyObservableCollection<ILogItem> Collection)
        {
            if (Collection == null)
                return;

            var locker = new object();

            Collection.ToObservableChangeSet()
                      .Synchronize(locker)
                      .Buffer(TimeSpan.FromMilliseconds(100))
                      .Where(l => l.Count > 0)
                      .ObserveOnDispatcher()
                      .Synchronize(locker)
                      .SelectMany(l => l)
                      .Sort(SortExpressionComparer<ILogItem>.Ascending(x => x.Time),
                            SortOptions.UseBinarySearch)
                      .Bind(out _collection)
                      .Subscribe();

            var collectionChanges = _collection.ToObservableChangeSet()
                                               .Synchronize(locker);

            var viewModel = new ListViewModel(collectionChanges.Count());

            DataContext                           = viewModel;
            _dataViewModel                        = viewModel;
            _dataViewModel.Heights.ViewportHeight = HostCanvas.ActualHeight;

            var pagingRequests = viewModel.Heights.PagingRequests;

            pagingRequests
               .Synchronize(locker)
               .Subscribe(r => _pagingRequest = r);

            collectionChanges.Do(changes => _animate |= AreThereChangesInsideOfRange(changes))
                             .Virtualise(pagingRequests)
                             .ToCollection()
                             .DistinctByDispatcher(DispatcherPriority.Loaded)
                             .Synchronize(locker)
                             .Select(ProcessListToTransitions)
                             .Do(ApplyTransitions)
                             .Subscribe(_ => _animate = false);

            // Collection.ToObservableChangeSet()
            //            //.Buffer(TimeSpan.FromMilliseconds(100))
            //            //.Where(l => l.Count > 0)
            //           .ObserveOnDispatcher()
            //            //.Do(l => Console.WriteLine(l.Count))
            //            //.SelectMany(l => l)
            //           .Sort(SortExpressionComparer<ILogItem>.Ascending(x => x.Time),
            //                 SortOptions.UseBinarySearch)
            //           .Bind(out _collection)
            //            //           //.Do(_ => Rescroll())
            //           .Do(changes => _animate = AreThereChangesInsideOfRange(changes))
            //           .Virtualise(pagingRequests)
            //           .ToCollection()
            //           .Do(col => Console.WriteLine($"New collection: {col.Count}"))
            //           .DistinctByDispatcher(DispatcherPriority.Loaded)
            //           .Select(ProcessListToTransitions)
            //           .Do(ApplyTransitions)
            //           .Subscribe(_ => _animate = false);

            _dataViewModel.Heights.WhenAnyValue(x => x.ListOffset)
                           .Subscribe(OnScroll);
        }

        private bool AreThereChangesInsideOfRange(IChangeSet<ILogItem> Changes)
        {
            bool IsInRange(int Index)
            {
                return Index >= _pagingRequest.StartIndex &&
                       Index < _pagingRequest.StartIndex + _pagingRequest.Size;
            }

            var itemChanges = Changes.Flatten().ToList();
            var changesWhithin = itemChanges.Where(ch => IsInRange(ch.CurrentIndex) || IsInRange(ch.PreviousIndex)).ToList();
            return changesWhithin.Any();
        }

        private void Rescroll()
        {
            var newIndexes = new Dictionary<ILogItem, int>();

            if (_visibleItems.Any())
            {
                var topIndex    = 0;
                var bottomIndex = _collection.Count - 1;

                foreach (var t in _visibleItems)
                {
                    var index = _collection.BinarySearch(t, 0, bottomIndex);
                    if (index != -1)
                    {
                        topIndex = index;
                        break;
                    }
                }

                for (var i = _visibleItems.Count - 1; i >= 0; i--)
                {
                    var idx                    = _collection.BinarySearch(_visibleItems[i], topIndex, bottomIndex);
                    if (idx != -1) bottomIndex = idx;
                    newIndexes.Add(_visibleItems[i], idx);
                }

                var shift = _visibleItems.Select((item, i) => new { old = i, @new = newIndexes[item] })
                                         .Where(x => x.@new != -1)
                                         .Select(x => _dataViewModel.Heights.OffsetFromIndex(x.@new) -
                                                      _dataViewModel.Heights.OffsetFromIndex(x.old))
                                         .Average();

                _dataViewModel.Heights.ListOffset = shift;
            }
        }

        private IList<ItemTransition> ProcessListToTransitions(IReadOnlyCollection<ILogItem> NewItems)
        {
            var top    = new List<ItemTransition>();
            var middle = new List<ItemTransition>();
            var bottom = new List<ItemTransition>();

            var stage = 0;

            var allNew = NewItems.Select((item, index) =>
                                             new
                                             {
                                                 item,
                                                 index,
                                                 isNew = !_visibleItems.Contains(item)
                                             })
                                 .Where(x => x.isNew)
                                 .ToList();

            List<ItemTransition> @new;
            if (_visibleItems.Any())
            {
                var oldMostTopTime = _visibleItems.First().Time;
                var topNew         = allNew.Where(x => x.item.Time < oldMostTopTime).ToList();
                var topNewTransitions = topNew.Select((item, index) => new ItemTransition(item.item)
                                                          { To = item.index, From = index - topNew.Count, New = true });

                var oldMotBottomTime = _visibleItems.Last().Time;
                var bottomNew        = allNew.Where(x => x.item.Time > oldMotBottomTime).ToList();
                var bottomNewTransitions = bottomNew.Select((item, index) => new ItemTransition(item.item)
                {
                    To = item.index, From = _pagingRequest.Size + index, New = true
                });

                var middleNewTransitions = allNew
                                          .Where(x => x.item.Time >= oldMostTopTime && x.item.Time <= oldMotBottomTime)
                                          .Select(x => new ItemTransition(x.item)
                                                      { To = x.index, New = true, Inserted = true });

                @new = topNewTransitions.Concat(middleNewTransitions).Concat(bottomNewTransitions).ToList();
            }
            else
            {
                @new = allNew.Select(x => new ItemTransition(x.item) { To = x.index, New = true, Inserted = true })
                             .ToList();
            }

            for (var i = 0; i < _visibleItems.Count; i++)
            {
                var item     = _visibleItems[i];
                var newIndex = NewItems.IndexOf(item);

                switch (stage)
                {
                    case 0:
                        if (newIndex == -1)
                        {
                            top.Add(new ItemTransition(item) { From = i });
                        }
                        else
                        {
                            middle.Add(new ItemTransition(item) { From = i, To = newIndex });
                            stage = 1;
                        }

                        break;

                    case 1:
                        if (newIndex == -1)
                        {
                            bottom.Add(new ItemTransition(item) { From = i });
                        }
                        else
                        {
                            middle.AddRange(bottom.Select(it => new ItemTransition(it.Item)
                                                              { From = it.From, Deleted = true }));
                            middle.Add(new ItemTransition(item) { From = i, To = newIndex });
                            bottom.Clear();
                        }

                        break;
                }
            }

            #region Слишком сложный алгоритм

            // if (top.Any())
            // {
            //     int topListBottomIndex = _pagingRequest.StartIndex;
            //     int topListTopIndex = 0;
            //
            //     for (int i = 0; i < top.Count; i++)
            //     {
            //         var index = _collection.BinarySearch(top[i].Item, 0, topListBottomIndex);
            //         if (index != -1)
            //         {
            //             topListBottomIndex = index;
            //             break;
            //         }
            //     }
            //     
            //     for (int i = top.Count - 1; i >= 0; i--)
            //     {
            //         var bottomIndex = _collection.BinarySearch(top[^i].Item, topListTopIndex, topListBottomIndex);
            //         if (bottomIndex != -1)
            //         {
            //             top[^i]
            //         }
            //     }
            // }

            #endregion

            for (var i = 1; i <= top.Count; i++)
                top[^i] = new ItemTransition(top[^i].Item) { From = top[^i].From, To = -i, RemoveAfterMove = true };

            for (var i = 0; i < bottom.Count; i++)
                bottom[i] = new ItemTransition(bottom[i].Item)
                    { From = bottom[i].From, To = _pagingRequest.Size + i, RemoveAfterMove = true };


            var transitions = new[] { top, middle, bottom, @new }.SelectMany(x => x).ToList();
            Console.WriteLine($"Prepared {transitions.GetHashCode():x}");

            return transitions;
        }

        private void ApplyTransitions(IList<ItemTransition> Transitions)
        {
            Console.WriteLine($"Rendered {Transitions.GetHashCode():x} (Animations: {_animate})");

            foreach (var newItem in Transitions.Where(t => t.New))
                CreateItem(newItem.Item, newItem.To);

            foreach (var itemTransition in Transitions)
                if (itemTransition.Inserted)
                    FadeInItem(itemTransition.Item);
                else if (itemTransition.Deleted)
                    FadeOutItem(itemTransition.Item);
                else
                    MoveItem(itemTransition.Item, itemTransition.From, itemTransition.To,
                             itemTransition.RemoveAfterMove);

            _visibleItems = Transitions.Where(t => !t.Deleted && !t.RemoveAfterMove)
                                       .OrderBy(t => t.To)
                                       .Select(t => t.Item)
                                       .ToList();
        }

        private void OnScroll(double Offset)
        {
            for (var i = 0; i < _visibleItems.Count; i++)
            {
                var container = _containers[_visibleItems[i]];
                container.SetValue(Canvas.TopProperty,
                                   _dataViewModel.Heights.RelativeOffsetFromIndex(i));
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.HeightChanged && _dataViewModel != null)
                _dataViewModel.Heights.ViewportHeight = HostCanvas.ActualHeight;
        }

        #region WPF Events

        private void OnMouseWheel(object Sender, MouseWheelEventArgs E)
        {
            _dataViewModel.Heights.ListOffset -= E.Delta;
        }

        #endregion

        private struct ItemTransition
        {
            public ItemTransition(ILogItem Item) : this()
            {
                this.Item = Item;
            }

            public ILogItem Item            { get; }
            public int      From            { get; set; }
            public int      To              { get; set; }
            public bool     Deleted         { get; set; }
            public bool     New             { get; set; }
            public bool     RemoveAfterMove { get; set; }
            public bool     Inserted        { get; set; }

            public override string ToString()
            {
                if (New)
                    return $"+ {To}  {Item}";
                if (Deleted)
                    return $"- {From}  {Item}";
                return $"{From} -> {To}  {Item}";
            }
        }

        #region Transitions

        private void MoveItem(ILogItem Item, int From, int To, bool RemoveAfterMove)
        {
            var container = _containers[Item];
            container.SetValue(Canvas.TopProperty, _dataViewModel.Heights.OffsetFromIndex(To));

            if (_animate)
            {
                var easingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut };
                var animation = new DoubleAnimation
                {
                    From           = _dataViewModel.Heights.OffsetFromIndex(From),
                    Duration       = TimeSpan.FromMilliseconds(300),
                    EasingFunction = easingFunction,
                    IsAdditive     = true
                };
                if (RemoveAfterMove)
                {
                    RemoveItem(Item, false);
                    animation.Completed += (Sender, Args) => HostCanvas.Children.Remove(container);
                }

                container.BeginAnimation(Canvas.TopProperty, animation);
            }
            else
            {
                if (RemoveAfterMove)
                    RemoveItem(Item, true);
            }
        }

        private void RemoveItem(ILogItem Item, bool RemoveFromVisualTree)
        {
            var container = _containers[Item];
            _containers.Remove(Item);
            if (RemoveFromVisualTree)
                HostCanvas.Children.Remove(container);
        }

        private void FadeInItem(ILogItem Item)
        {
            if (!_animate) return;

            var container      = _containers[Item];
            var easingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut };
            container.BeginAnimation(OpacityProperty,
                                     new DoubleAnimation
                                     {
                                         From           = 0,
                                         Duration       = TimeSpan.FromMilliseconds(600),
                                         EasingFunction = easingFunction
                                     });
        }

        private void CreateItem(ILogItem Item, int Index)
        {
            if (_containers.ContainsKey(Item))
                return;

            var yPosition = _dataViewModel.Heights.RelativeOffsetFromIndex(Index);

            var presenter = new ContentPresenter
            {
                Content = Item,
                Height  = _dataViewModel.Heights.ItemHeight,
                Width   = Width
            };
            presenter.SetValue(Canvas.LeftProperty, 0.0);
            presenter.SetValue(Canvas.TopProperty,  yPosition);

            _containers.Add(Item, presenter);
            HostCanvas.Children.Add(presenter);
        }

        private void FadeOutItem(ILogItem Item)
        {
            if (!_animate)
            {
                RemoveItem(Item, true);
                return;
            }

            var easingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut };

            var container = _containers[Item];

            var animation = new DoubleAnimation
            {
                To             = 0.0,
                Duration       = TimeSpan.FromMilliseconds(300),
                EasingFunction = easingFunction
            };

            RemoveItem(Item, false);
            animation.Completed += (Sender, Args) => HostCanvas.Children.Remove(container);

            container.BeginAnimation(OpacityProperty, animation);
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(ReadOnlyObservableCollection<ILogItem>), typeof(LogListBox),
            new PropertyMetadata(null,
                                 ItemsSourceChangedCallback));


        private readonly Dictionary<ILogItem, ContentPresenter> _containers =
            new Dictionary<ILogItem, ContentPresenter>();


        private static void ItemsSourceChangedCallback(DependencyObject D, DependencyPropertyChangedEventArgs E)
        {
            var data    = (ReadOnlyObservableCollection<ILogItem>) E.NewValue;
            var control = (LogListBox) D;
            control.Attach(data);
        }

        public IListDataViewModel ItemsSource
        {
            get => (IListDataViewModel) GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        #endregion
    }
}