using System;
using System.Linq.Expressions;
using LogList.Control.Manipulation.Implementations.Filtering;

namespace LogList.Control.Manipulation.Implementations
{
    public static class Filters
    {
        public static IFilter<TItem> Empty<TItem>()
        {
            return new EmptyFilter<TItem>();
        }

        public static IFilter<TItem> ByStringRequest<TItem>(
            string Request, StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase)
            where TItem : ILogItem, IFilterableByString
        {
            if (string.IsNullOrWhiteSpace(Request))
                return Empty<TItem>();

            return new StringInterfaceFilter<TItem>(Request, Comparison);
        }

        public static IFilter<TItem> ByStringRequest<TItem>(
            string Request,
            params Expression<Func<TItem, string>>[] PropertyProviders)
            where TItem : ILogItem
        {
            return ByStringRequest(Request, StringComparison.CurrentCultureIgnoreCase, PropertyProviders);
        }

        public static IFilter<TItem> ByStringRequest<TItem>(
            string Request, StringComparison Comparison,
            params Expression<Func<TItem, string>>[] PropertyProviders)
            where TItem : ILogItem
        {
            if (string.IsNullOrWhiteSpace(Request))
                return Empty<TItem>();

            return new StringExpressionFilter<TItem>(Request, Comparison, PropertyProviders);
        }

        public static IFilter<TItem> CompositeAll<TItem>(params IFilter<TItem>[] Children)
            where TItem : ILogItem
        {
            return new CompositeAllFilter<TItem>(Children);
        }
    }
}