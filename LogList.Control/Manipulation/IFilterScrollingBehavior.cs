using System.Collections.Generic;
using LogList.Control.Manipulation.Implementations;

namespace LogList.Control.Manipulation
{
    public interface IFilterScrollingBehavior<TItem>
        where TItem : ILogItem
    {
        int GetOffset(List<LogRecord<TItem>> OldFiltered, List<LogRecord<TItem>> Filtered, ViewWindow Window);
    }
}