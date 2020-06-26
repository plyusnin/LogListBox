using System.Collections.Generic;

namespace LogList.Control.Manipulation
{
    public interface ILogEditor<TItem>
        where TItem : ILogItem
    {
        void Append(TItem Item);
        void Append(IList<TItem> Items);
        void Insert(TItem Item);
        void Clear();
    }
}