namespace LogList.Control.Manipulation
{
    public interface IFilter<TItem> where TItem: ILogItem
    {
        bool Check(TItem Item);
        bool IsSubFilterFor(IFilter<TItem> Another);
    }
}