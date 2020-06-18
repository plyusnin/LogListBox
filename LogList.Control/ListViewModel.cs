using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace LogList.Control
{
    public class ListViewModel : ReactiveObject, IListDataViewModel
    {
        public ListViewModel(ReadOnlyObservableCollection<ILogItem> Collection)
        {
            var sortedOriginals =
                Collection.ToObservableChangeSet()
                          .Sort(SortExpressionComparer<ILogItem>.Ascending(x => x.Time),
                                SortOptions.UseBinarySearch);

            Heights = new HeightViewModel(sortedOriginals);
        }

        public HeightViewModel Heights { get; }
    }
}