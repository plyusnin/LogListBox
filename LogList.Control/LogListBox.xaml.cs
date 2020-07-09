using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LogList.Control.Manipulation.Implementations;
using ReactiveUI;

namespace LogList.Control
{
    public partial class LogListBox : UserControl
    {
        private int _transitionSetId = -1;
        private List<ILogItem> _visibleItems = new List<ILogItem>();

        public LogListBox()
        {
            InitializeComponent();
        }

        private void Attach(ILogViewSource ViewSource)
        {
            _viewSourceSubscription?.Dispose();

            if (ViewSource == null)
                return;

            var locker = new object();

            Scroller = new ScrollerViewModel(ViewSource.FilteredSetSize, ViewSource.PresentationRequests)
            {
                ViewportHeight = HostCanvas.ActualHeight
            };

            _viewSourceSubscription = Scroller.Requests
                                              .DistinctByDispatcher(DispatcherPriority.Loaded)
                                              .Synchronize(locker)
                                              .Select(r => new LogView(
                                                          r.Window,
                                                          ViewSource.Present(r.Window),
                                                          r.AnimateTransitions))
                                              .Select(ProcessListToTransitions)
                                              .Do(x => ApplyTransitions(x))
                                              .Subscribe();

            Scroller.WhenAnyValue(x => x.ListOffset)
                    .Synchronize(locker)
                    .Subscribe(OnScroll);
        }

        private TransitionsSet ProcessListToTransitions(ILogView NewView)
        {
            var top    = new List<ItemTransition>();
            var middle = new List<ItemTransition>();
            var bottom = new List<ItemTransition>();

            var stage = 0;

            var allNew = NewView.VisibleItems.Select((item, index) =>
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
                    To = item.index, From = NewView.Window.Size + index, New = true
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
                var newIndex = NewView.VisibleItems.IndexOf(item);

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
                    { From = bottom[i].From, To = NewView.Window.Size + i, RemoveAfterMove = true };


            var transitions = new[] { top, middle, bottom, @new }.SelectMany(x => x).ToList();

            return new TransitionsSet(Interlocked.Increment(ref _transitionSetId), transitions,
                                      NewView.AnimateTransitions);
        }

        private void ApplyTransitions(TransitionsSet Set)
        {
            foreach (var newItem in Set.Transitions.Where(t => t.New))
                CreateItem(newItem.Item, newItem.To);

            foreach (var itemTransition in Set.Transitions)
                if (itemTransition.Inserted)
                    FadeInItem(itemTransition.Item, Set.Animate);
                else if (itemTransition.Deleted)
                    FadeOutItem(itemTransition.Item, Set.Animate, Set.Id);
                else
                    MoveItem(itemTransition.Item,            itemTransition.From, itemTransition.To,
                             itemTransition.RemoveAfterMove, Set.Animate,         Set.Id);

            _visibleItems = Set.Transitions
                               .Where(t => !t.Deleted && !t.RemoveAfterMove)
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
                                   Scroller.RelativeOffsetFromIndex(i));
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.HeightChanged && Scroller != null)
                Scroller.ViewportHeight = HostCanvas.ActualHeight;

            if (sizeInfo.WidthChanged)
            {
                if (_containers.Any())
                    HostCanvas.MinWidth = _containers.Values.Max(c => c.ActualWidth);

                foreach (var container in _containers.Values)
                    container.MinWidth = HostCanvas.ActualWidth;
            }
        }

        // public void ScrollIntoView(ILogItem Item, double ScrollingMargin = 25)
        // {
        //     VerifyAccess();
        //
        //     var scrollIndex =
        //         _collection.BinarySearch(Item, BinarySearchExtensions.ItemNotFoundBehavior.ReturnClosestTimeIndex);
        //
        //     var itemOffset     = Scroller.OffsetFromIndex(scrollIndex);
        //     var listOffset     = Scroller.ListOffset;
        //     var viewportHeight = Scroller.ViewportHeight;
        //
        //     if (itemOffset < listOffset + ScrollingMargin)
        //         Scroller.ListOffset = itemOffset                  - ScrollingMargin;
        //     else if (itemOffset > listOffset                      + viewportHeight - ScrollingMargin)
        //         Scroller.ListOffset = itemOffset - viewportHeight + ScrollingMargin;
        // }

        #region WPF Events

        private void OnMouseWheel(object Sender, MouseWheelEventArgs E)
        {
            E.Handled           =  false;
            Scroller.ListOffset -= E.Delta;
        }

        #endregion

        private class TransitionsSet
        {
            public TransitionsSet(int Id, List<ItemTransition> Transitions, bool Animate)
            {
                this.Id          = Id;
                this.Transitions = Transitions;
                this.Animate     = Animate;
            }

            public int                  Id          { get; }
            public List<ItemTransition> Transitions { get; }
            public bool                 Animate     { get; }

            public override string ToString()
            {
                return $"{Id}: {Transitions.Count} transitions, Animation: {Animate}";
            }
        }

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

        private void MoveItem(ILogItem Item, int From, int To, bool RemoveAfterMove, bool Animate, int SetId)
        {
            var container = _containers[Item];
            container.SetValue(Canvas.TopProperty, Scroller.RelativeOffsetFromIndex(To));

            if (Animate)
            {
                var easingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut };
                var animation = new DoubleAnimation
                {
                    From           = Scroller.RelativeOffsetFromIndex(From),
                    Duration       = TimeSpan.FromMilliseconds(300),
                    EasingFunction = easingFunction,
                    IsAdditive     = true
                };
                if (RemoveAfterMove)
                {
                    RemoveItem(Item, false, SetId);
                    animation.Completed += (Sender, Args) => HostCanvas.Children.Remove(container);
                }

                container.BeginAnimation(Canvas.TopProperty, animation);
            }
            else
            {
                if (RemoveAfterMove)
                    RemoveItem(Item, true, SetId);
            }
        }

        private void RemoveItem(ILogItem Item, bool RemoveFromVisualTree, int SetId)
        {
            var container = _containers[Item];
            _containers.Remove(Item);
            if (RemoveFromVisualTree)
            {
                container.Visibility = Visibility.Collapsed;
                _spareContainers.Push(container);
            }
        }

        private void FadeInItem(ILogItem Item, bool Animate)
        {
            if (!Animate) return;

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

        private readonly Stack<ContentPresenter> _spareContainers = new Stack<ContentPresenter>();

        private void CreateItem(ILogItem Item, int Index)
        {
            if (_containers.ContainsKey(Item))
                return;

            var yPosition = Scroller.RelativeOffsetFromIndex(Index);

            var freshCreated = false;
            if (!_spareContainers.TryPop(out var presenter))
            {
                presenter = new ContentPresenter
                {
                    Height              = Scroller.ItemHeight,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                freshCreated = true;
            }
            else
            {
                presenter.Visibility = Visibility.Visible;
            }

            presenter.Content  = Item;
            presenter.MinWidth = HostCanvas.ActualWidth;
            presenter.SetValue(Canvas.LeftProperty, 0.0);
            presenter.SetValue(Canvas.TopProperty,  yPosition);

            presenter.SetBinding(ContentPresenter.ContentTemplateProperty,
                                 new Binding(nameof(ItemTemplate)) { Source = this });

            _containers.Add(Item, presenter);

            if (freshCreated)
                HostCanvas.Children.Add(presenter);

            HostCanvas.MinWidth = _containers.Values.Max(c => c.ActualWidth);
        }

        private void FadeOutItem(ILogItem Item, bool Animate, int SetId)
        {
            if (Animate)
            {
                RemoveItem(Item, true, SetId);
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

            RemoveItem(Item, false, SetId);
            animation.Completed += (Sender, Args) => HostCanvas.Children.Remove(container);

            container.BeginAnimation(OpacityProperty, animation);
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(ILogViewSource), typeof(LogListBox),
            new PropertyMetadata(null, ItemsSourceChangedCallback));

        private readonly Dictionary<ILogItem, ContentPresenter> _containers =
            new Dictionary<ILogItem, ContentPresenter>();


        private static void ItemsSourceChangedCallback(DependencyObject D, DependencyPropertyChangedEventArgs E)
        {
            var data    = (ILogViewSource) E.NewValue;
            var control = (LogListBox) D;
            control.Attach(data);
        }

        public ILogViewSource ItemsSource
        {
            get => (ILogViewSource) GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            "ItemTemplate", typeof(DataTemplate), typeof(LogListBox), new PropertyMetadata(default(DataTemplate)));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate) GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly DependencyProperty ScrollerProperty = DependencyProperty.Register(
            "Scroller", typeof(ScrollerViewModel), typeof(LogListBox),
            new PropertyMetadata(new ScrollerViewModel(Observable.Return(0), Observable.Never<PresentationRequest>())));

        private IDisposable _viewSourceSubscription;

        public ScrollerViewModel Scroller
        {
            get => (ScrollerViewModel) GetValue(ScrollerProperty);
            set => SetValue(ScrollerProperty, value);
        }

        #endregion
    }
}