using System.Collections.Generic;
using LogList.Control.Manipulation.Implementations;

namespace LogList.Control.Manipulation
{
    public interface IAppendScrollingBehavior<TItem>
        where TItem : ILogItem
    {
        int GetOffset(IList<LogRecord<TItem>> NewItems, int InsertionIndex, ViewWindow OldWindow);
    }
}