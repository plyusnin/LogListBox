using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using LogList.Demo.ViewModels;
using ReactiveUI;

namespace LogList.Demo
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var viewModel = new MainViewModel();
            viewModel.ItemAdded
                     .ObserveOnDispatcher()
                     .CombineLatest(viewModel.WhenAnyValue(x => x.AutoScroll), (Item, AutoScroll) => new { Item, AutoScroll })
                     .Where(x => x.AutoScroll)
                     .Subscribe(x => ListBox.ScrollIntoView(x.Item));
            DataContext = viewModel;
        }
    }
}