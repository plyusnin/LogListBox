using System;
using ReactiveUI;

namespace LogList.Control
{
    public class ListViewModel : ReactiveObject, IListDataViewModel
    {
        public ListViewModel(IObservable<int> SourceSize)
        {
        }
    }
}