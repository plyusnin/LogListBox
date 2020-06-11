using System;
using System.ComponentModel;
using DynamicData;

namespace LogList.Control
{
    public interface IListDataViewModel : INotifyPropertyChanged
    {
        IObservable<IVirtualChangeSet<ILogItem, int>> VisibleItems { get; }
        HeightViewModel Heights { get; }
    }
}