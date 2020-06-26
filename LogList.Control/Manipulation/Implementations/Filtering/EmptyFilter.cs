namespace LogList.Control.Manipulation.Implementations.Filtering
{
    public sealed class EmptyFilter<TItem> : IFilter<TItem>
        where TItem : ILogItem
    {
        public bool Check(TItem Item)
        {
            return true;
        }

        public bool IsSubFilterFor(IFilter<TItem> Another)
        {
            return Another is EmptyFilter<TItem>;
        }

        public override string ToString()
        {
            return "Empty Filter";
        }
    }
}