using System;
using System.Collections.Generic;

namespace LogList.Control.Manipulation.Implementations.Scrolling
{
    public class PreservePositionScrollingBehavior<TItem> : IAppendScrollingBehavior<TItem>,
                                                            IFilterScrollingBehavior<TItem>
        where TItem : ILogItem
    {
        public int GetOffset(IList<TItem> NewItems, int InsertionIndex, ViewWindow OldWindow)
        {
            return OldWindow.Offset;
        }

        public int GetOffset(List<TItem> OldFiltered, List<TItem> Filtered, ViewWindow Window)
        {
            if (OldFiltered.Count == 0 || Filtered.Count == 0)
                return 0;
            
            var indexInTheMiddle = Math.Min(Window.Offset + Window.Size / 2, OldFiltered.Count - 1);
            var itemInTheMiddle  = OldFiltered[indexInTheMiddle];
            var closestIndex = Filtered.BinarySearch(itemInTheMiddle,
                                                     BinarySearchExtensions
                                                        .ItemNotFoundBehavior.ReturnClosestTimeIndex);
            var offset = Math.Max(0, closestIndex - Window.Size / 2);
            return offset;
        }
    }
}