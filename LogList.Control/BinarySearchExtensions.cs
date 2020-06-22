using System.Collections.ObjectModel;

namespace LogList.Control
{
    public static class BinarySearchExtensions
    {
        public static int BinarySearch(this ReadOnlyObservableCollection<ILogItem> Collection, ILogItem Item, int StartIndex, int EndIndex)
        {
            if (Item.Time < Collection[StartIndex].Time)
                return -1;
            if (Item.Time > Collection[EndIndex].Time)
                return -1;

            while (EndIndex - StartIndex > 2)
            {
                var middleIndex = (StartIndex + EndIndex) / 2;
                if (Collection[middleIndex].Time > Item.Time)
                    EndIndex = middleIndex;
                else
                    StartIndex = middleIndex;
            }

            for (var i = StartIndex; Collection[i].Time <= Item.Time; i++)
                if (Collection[i] == Item)
                    return i;

            return -1;
        }
    }
}