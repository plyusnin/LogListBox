﻿using System.ComponentModel;

namespace LogList.Control
{
    public interface IListDataViewModel : INotifyPropertyChanged
    {
        HeightViewModel Heights { get; }
    }
}