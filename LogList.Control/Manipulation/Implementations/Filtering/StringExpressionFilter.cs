using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LogList.Control.Manipulation.Implementations.Filtering
{
    public class StringExpressionFilter<TItem> : IFilter<TItem>
        where TItem : ILogItem
    {
        private readonly StringComparison _comparison;
        private readonly List<Func<TItem, string>> _compiledProviders;
        private readonly HashSet<string> _properties;
        private readonly string _request;

        public StringExpressionFilter(
            string Request, StringComparison Comparison,
            params Expression<Func<TItem, string>>[] PropertyProviders)
        {
            _request    = Request;
            _comparison = Comparison;
            _properties = PropertyProviders.Select(p => p.ToString()).ToHashSet();
            _compiledProviders = PropertyProviders.Select(e => e.Compile())
                                                  .ToList();
        }

        public bool Check(TItem Item)
        {
            return _compiledProviders.Any(p => p(Item).Contains(_request, _comparison));
        }

        public bool IsSubFilterFor(IFilter<TItem> Another)
        {
            if (Another == this)
                return true;

            switch (Another)
            {
                case EmptyFilter<TItem> emptyFilter:
                    return true;

                case StringExpressionFilter<TItem> anotherStringFilter:
                    return anotherStringFilter._comparison == _comparison         &&
                           anotherStringFilter._properties.SetEquals(_properties) &&
                           _request.Contains(anotherStringFilter._request, _comparison);

                default:
                    return false;
            }
        }

        public override string ToString()
        {
            return $"String Filter: \"{_request}\" ({string.Join(", ", _properties)})";
        }
    }
}