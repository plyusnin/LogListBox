using System;

namespace LogList.Control.Manipulation.Implementations.Filtering
{
    public class StringInterfaceFilter<TItem> : IFilter<TItem>
        where TItem : ILogItem, IFilterableByString
    {
        private readonly StringComparison _comparison;
        private readonly string _request;

        public StringInterfaceFilter(
            string Request, StringComparison Comparison)
        {
            _request    = Request;
            _comparison = Comparison;
        }

        public bool Check(TItem Item)
        {
            return Item.FilterString.Contains(_request, _comparison);
        }

        public bool IsSubFilterFor(IFilter<TItem> Another)
        {
            if (Another == this)
                return true;

            switch (Another)
            {
                case EmptyFilter<TItem> emptyFilter:
                    return true;

                case StringInterfaceFilter<TItem> anotherStringFilter:
                    return anotherStringFilter._comparison == _comparison &&
                           _request.Contains(anotherStringFilter._request, _comparison);

                default:
                    return false;
            }
        }

        public override string ToString()
        {
            return $"String Filter: \"{_request}\"";
        }
    }
}