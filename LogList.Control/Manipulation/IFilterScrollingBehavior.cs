using System.Collections.Generic;

namespace LogList.Control.Manipulation
{
    public interface IFilterScrollingBehavior<TItem>
        where TItem : ILogItem
    {
        int GetOffset(List<TItem> OldFiltered, List<TItem> Filtered, ViewWindow Window);
    }
}