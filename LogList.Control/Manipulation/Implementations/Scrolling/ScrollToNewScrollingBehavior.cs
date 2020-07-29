using System;
using System.Collections.Generic;

namespace LogList.Control.Manipulation.Implementations.Scrolling
{
    public class ScrollToNewScrollingBehavior<TItem> : IAppendScrollingBehavior<TItem>
        where TItem : ILogItem
    {
        public int GetOffset(IList<LogRecord<TItem>> NewItems, int InsertionIndex, ViewWindow OldWindow)
        {
            return Math.Max(0, InsertionIndex + NewItems.Count - OldWindow.Size);
        }
    }
}