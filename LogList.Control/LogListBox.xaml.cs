using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        private bool _autoscroll = true;
        
        private ReadOnlyObservableCollection<ILogItem> _collection;
        private IListDataViewModel _dataViewModel;
        private VirtualRequest _pagingRequest;
        private (ILogItem Item, double Offset) _viewingPosition;

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
                      // .Buffer(TimeSpan.FromMilliseconds(100))
                      // .Where(l => l.Count > 0)
                      .ObserveOnDispatcher()
                      .Synchronize(locker)
                      // .SelectMany(l => l)
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
                             .Do(ScrollIfNeeded)
                             .Virtualise(pagingRequests)
                             .ToCollection()
                             .DistinctByDispatcher(DispatcherPriority.Loaded)
                             .Synchronize(locker)
                             .Select(ProcessListToTransitions)
                             .Do(ApplyTransitions)
                              //.Do(_ => _viewingPosition = GetViewingPosition())
                             .Subscribe(_ =>
                              {
                                  _animate = false;
                                  _autoscroll = true;
                              });

            _dataViewModel.Heights.WhenAnyValue(x => x.ListOffset)
                          .Do(_ => _viewingPosition = GetViewingPosition())
                          .Subscribe();

            _dataViewModel.Heights.WhenAnyValue(x => x.ListOffset)
                          .Subscribe(OnScroll);
        }

        private void ScrollIfNeeded(IChangeSet<ILogItem> Changes)
        {
            if (_viewingPosition.Item == null || _collection.Count == 0 || !_autoscroll)
                return;

            var index = _collection.BinarySearch(_viewingPosition.Item,
                                                 BinarySearchExtensions.ItemNotFoundBehavior.ReturnClosestTimeIndex);
            if (index < 0) return;

            var itemAbsoluteOffset = _dataViewModel.Heights.OffsetFromIndex(index);
            var offset             = itemAbsoluteOffset - _viewingPosition.Offset;

            _dataViewModel.Heights.ListOffset = offset;
        }

        private (ILogItem Item, double Offset) GetViewingPosition()
        {
            if (_visibleItems.Count == 0)
                return (null, 0);

            var index = _visibleItems.Count / 2;
            var res   = (_visibleItems[index], _dataViewModel.Heights.RelativeOffsetFromIndex(index));

            return res;
        }

        private bool AreThereChangesInsideOfRange(IChangeSet<ILogItem> Changes)
        {
            bool IsInRange(int Index)
            {
                return Index >= _pagingRequest.StartIndex &&
                       Index < _pagingRequest.StartIndex + _pagingRequest.Size;
            }

            return Changes.Flatten().ToList().Any(ch => IsInRange(ch.CurrentIndex) || IsInRange(ch.PreviousIndex));
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

            for (var i = 1; i <= top.Count; i++)
                top[^i] = new ItemTransition(top[^i].Item) { From = top[^i].From, To = -i, RemoveAfterMove = true };

            for (var i = 0; i < bottom.Count; i++)
                bottom[i] = new ItemTransition(bottom[i].Item)
                    { From = bottom[i].From, To = _pagingRequest.Size + i, RemoveAfterMove = true };


            var transitions = new[] { top, middle, bottom, @new }.SelectMany(x => x).ToList();
            return transitions;
        }

        private void ApplyTransitions(IList<ItemTransition> Transitions)
        {
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

        public void ScrollIntoView(ILogItem Item, double ScrollingMargin = 25)
        {
            VerifyAccess();
            
            var scrollIndex =
                _collection.BinarySearch(Item, BinarySearchExtensions.ItemNotFoundBehavior.ReturnClosestTimeIndex);

            var itemOffset     = _dataViewModel.Heights.OffsetFromIndex(scrollIndex);
            var listOffset     = _dataViewModel.Heights.ListOffset;
            var viewportHeight = _dataViewModel.Heights.ViewportHeight;

            if (itemOffset < listOffset + ScrollingMargin)
                _dataViewModel.Heights.ListOffset = itemOffset - ScrollingMargin;
            else if (itemOffset > listOffset                                    + viewportHeight - ScrollingMargin)
                _dataViewModel.Heights.ListOffset = itemOffset - viewportHeight + ScrollingMargin;
            
            _autoscroll = false;
            _animate = false;
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
            container.SetValue(Canvas.TopProperty, _dataViewModel.Heights.RelativeOffsetFromIndex(To));

            if (_animate)
            {
                var easingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut };
                var animation = new DoubleAnimation
                {
                    From           = _dataViewModel.Heights.RelativeOffsetFromIndex(From),
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
            
            presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding(nameof(ItemTemplate)) { Source = this});

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

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            "ItemTemplate", typeof(DataTemplate), typeof(LogListBox), new PropertyMetadata(default(DataTemplate)));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate) GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
        
        #endregion
    }
}