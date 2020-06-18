using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LogList.Control
{
    public static class BinarySearchExtensions
    {
        public static int BinarySearch(this ReadOnlyObservableCollection<ILogItem> Collection, ILogItem Item, int StartIndex, int EndIndex)
        {
            if (Collection[StartIndex].Time < Item.Time)
                return -1;
            if (Collection[EndIndex].Time > Item.Time)
                return -1;

            while (EndIndex - StartIndex > 0)
            {
                int middleIndex = (StartIndex + EndIndex) / 2;
                if (Collection[middleIndex].Time > Item.Time)
                    EndIndex = middleIndex;
                else
                    StartIndex = middleIndex;
            }

            for (int i = StartIndex; Collection[i].Time == Item.Time; i++)
            {
                if (Collection[i] == Item)
                    return i;
            }

            return -1;
        }
    }
}