using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using DynamicData;

namespace LogList.Control
{
    public partial class LogListBox : UserControl
    {
        public LogListBox()
        {
            InitializeComponent();
        }

        private void Attach(IListDataViewModel Data)
        {
            if (Data == null)
                return;
            
            DataContext = Data;
            _dataViewModel = Data;
            _dataViewModel.Heights.ViewportHeight = HostCanvas.ActualHeight;

            Data.VisibleItems
                .ObserveOnDispatcher()
                .Flatten()
                .Subscribe(ProcessChange);
        }

        private void ProcessChange<TContent>(Change<TContent, int> Change)
        {
            switch (Change.Reason)
            {
                case ChangeReason.Add:
                    AddItem(Change.Current, Change.Key, Change.CurrentIndex);
                    break;

                case ChangeReason.Remove:
                    RemoveItem(Change.Key);
                    break;
                
                default:
                    break;
            }
        }

        private void RemoveItem(int Key)
        {
            var presenter = _items[Key];
            HostCanvas.Children.Remove(presenter);
            _items.Remove(Key);
        }

        private void AddItem<TContent>(TContent ItemData, int Key, int Index)
        {
            var presenter = new ContentPresenter { Content = ItemData, Height = _dataViewModel.Heights.ItemHeight, Width = Width };
            presenter.SetValue(Canvas.TopProperty, OffsetFromIndex(Index));
            _items.Add(Key, presenter);
            HostCanvas.Children.Add(presenter);
        }

        private double OffsetFromIndex(int Index)
        {
            return _dataViewModel.Heights.OffsetFromIndex(Index);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.HeightChanged && _dataViewModel != null)
                _dataViewModel.Heights.ViewportHeight = HostCanvas.ActualHeight;
        }

        #region Properties

        private readonly Dictionary<int, ContentPresenter> _items = new Dictionary<int, ContentPresenter>();

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