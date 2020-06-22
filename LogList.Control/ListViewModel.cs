using System;
using ReactiveUI;

namespace LogList.Control
{
    public class ListViewModel : ReactiveObject, IListDataViewModel
    {
        public ListViewModel(IObservable<int> SourceSize)
        {
            Heights = new HeightViewModel(SourceSize);
        }

        public HeightViewModel Heights { get; }
    }
}