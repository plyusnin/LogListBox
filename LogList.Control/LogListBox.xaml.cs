using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace LogList.Control
{
    public partial class LogListBox : UserControl
    {
        private IListDataViewModel _dataViewModel;
        private ReadOnlyObservableCollection<ContentPresenter> _items;

        public LogListBox()
        {
            InitializeComponent();
        }

        private void Attach(IListDataViewModel Data)
        {
            if (Data == null)
                return;

            DataContext                           = Data;
            _dataViewModel                        = Data;
            _dataViewModel.Heights.ViewportHeight = HostCanvas.ActualHeight;

            Data.VisibleItems
                .RemoveKey()
                .Sort(SortExpressionComparer<ILogItem>.Ascending(x => x.Time), SortOptions.UseBinarySearch)
                .ObserveOnDispatcher()
                .Transform(GetItemPresenter)
                .OnItemAdded(AddItem)
                .OnItemRemoved(RemoveItem)
                .Bind(out _items)
                .Do(_ => RefreshPositions())
                .Subscribe();

            _items.ObserveCollectionChanges()
                  .Where(ch => ch.EventArgs.Action == NotifyCollectionChangedAction.Add)
                  .Subscribe(ch => AnimateInsertion(ch.EventArgs.NewItems, ch.EventArgs.NewStartingIndex));

            _dataViewModel.Heights.WhenAnyValue(x => x.ListOffset)
                          .Do(_ =>
                           {
                               _skipAnimations = true;
                               Dispatcher.BeginInvoke(() => _skipAnimations = false);
                           })
                          .Subscribe(_ => RefreshPositions());
        }

        private void AnimateInsertion(IList NewItems, int NewStartingIndex)
        {
            if (_skipAnimations)
                return;

            var easingFunction = new PowerEase { EasingMode = EasingMode.EaseOut };

            var shift = NewItems.OfType<System.Windows.Controls.Control>().Sum(i => i.Height);

            for (var i = NewStartingIndex + NewItems.Count; i < _items.Count; i++)
            {
                var item = _items[i];
                item.BeginAnimation(Canvas.TopProperty,
                                    new DoubleAnimation
                                    {
                                        From           = (double) item.GetValue(Canvas.TopProperty) - shift,
                                        Duration       = TimeSpan.FromMilliseconds(300),
                                        EasingFunction = easingFunction,
                                        IsAdditive     = true
                                    });
            }
        }

        private void AddItem(ContentPresenter Item)
        {
            var easingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2 };

            HostCanvas.Children.Add(Item);
            if (!_skipAnimations)
            {
                Item.BeginAnimation(OpacityProperty,
                                    new DoubleAnimation { From = 0, Duration = TimeSpan.FromMilliseconds(200) });
                Item.BeginAnimation(Canvas.LeftProperty,
                                    new DoubleAnimation
                                    {
                                        From           = 50,
                                        Duration       = TimeSpan.FromMilliseconds(500),
                                        EasingFunction = easingFunction
                                    });
            }
        }

        private void RemoveItem(ContentPresenter Item)
        {
            HostCanvas.Children.Remove(Item);
        }

        private void RefreshPositions()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var offset = _dataViewModel.Heights.RelativeOffsetFromIndex(i);
                _items[i].SetValue(Canvas.TopProperty, offset);
            }
        }

        private ContentPresenter GetItemPresenter(ILogItem ItemData)
        {
            var presenter = new ContentPresenter
            {
                Content = ItemData,
                Height  = _dataViewModel.Heights.ItemHeight,
                Width   = Width
            };
            presenter.SetValue(Canvas.LeftProperty, 0.0);
            return presenter;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.HeightChanged && _dataViewModel != null)
                _dataViewModel.Heights.ViewportHeight = HostCanvas.ActualHeight;
        }

        #region Properties

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(IListDataViewModel), typeof(LogListBox),
            new PropertyMetadata(default(IListDataViewModel),
                                 ItemsSourceChangedCallback));

        private bool _skipAnimations;


        private static void ItemsSourceChangedCallback(DependencyObject D, DependencyPropertyChangedEventArgs E)
        {
            var data    = (IListDataViewModel) E.NewValue;
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