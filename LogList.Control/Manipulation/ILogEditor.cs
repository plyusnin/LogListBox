namespace LogList.Control.Manipulation
{
    public interface ILogEditor<in TItem>
        where TItem : ILogItem
    {
        void Append(TItem Item);
        void Insert(TItem Item);
        void Clear();
    }
}