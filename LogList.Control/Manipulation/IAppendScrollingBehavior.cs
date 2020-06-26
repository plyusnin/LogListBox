using System.Collections.Generic;

namespace LogList.Control.Manipulation
{
    public interface IAppendScrollingBehavior<TItem>
        where TItem : ILogItem
    {
        int GetOffset(IList<TItem> NewItems, int InsertionIndex, ViewWindow OldWindow);
    }
}