namespace LogList.Control.Manipulation
{
    public interface IFilter<TItem>
    {
        bool Check(TItem Item);
        bool IsSubFilterFor(IFilter<TItem> Another);
    }
}