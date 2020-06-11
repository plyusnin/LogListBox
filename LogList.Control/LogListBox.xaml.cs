using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace LogList.Control
{
    public partial class LogListBox : UserControl
    {
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
                .OnItemAdded(i => HostCanvas.Children.Add(i))
                .OnItemRemoved(i => HostCanvas.Children.Remove(i))
                .Bind(out _items)
                .Do(_ => RefreshPositions())
                .Subscribe();

            _dataViewModel.Heights.WhenAnyValue(x => x.ListOffset)
                          .Subscribe(_ => RefreshPositions());
        }

        private void RefreshPositions()
        {
            // var line = string.Join(" ", _items.Select(p => ((ILogItem) p.Content).Id));
            // Console.WriteLine(line);
            
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

        private IListDataViewModel _dataViewModel;

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