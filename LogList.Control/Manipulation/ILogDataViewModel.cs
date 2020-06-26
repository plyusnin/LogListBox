using System;
using System.Collections.Generic;
using LogList.Control.Manipulation.Implementations.Scrolling;

namespace LogList.Control.Manipulation
{
    public interface IListDataViewModel<TItem>
        where TItem : ILogItem
    {
        void ApplyFilter(
            IFilter<TItem> Filter, IFilterScrollingBehavior<TItem> ScrollingBehavior, bool AnimateTransition);

        void Edit(
            Action<ILogEditor<TItem>> EditAction, IAppendScrollingBehavior<TItem> ScrollingBehavior,
            bool AnimateTransition);
    }

    public static class ListDataViewModelExtensions
    {
        public static void ApplyFilter<TItem>(
            this IListDataViewModel<TItem> Data, IFilter<TItem> Filter, bool AnimateTransition = true)
            where TItem : ILogItem
        {
            Data.ApplyFilter(Filter, new PreservePositionScrollingBehavior<TItem>(), AnimateTransition);
        }

        public static void Append<TItem>(
            this IListDataViewModel<TItem> Data, TItem Item, bool ScrollToItem = false, bool Animate = false)
            where TItem : ILogItem
        {
            var scrollingBehavior = ScrollToItem
                ? (IAppendScrollingBehavior<TItem>) new ScrollToNewScrollingBehavior<TItem>()
                : new PreservePositionScrollingBehavior<TItem>();

            Data.Edit(list => list.Append(Item), scrollingBehavior, Animate);
        }

        public static void Append<TItem>(
            this IListDataViewModel<TItem> Data, IList<TItem> Items, bool ScrollToItem = false, bool Animate = false)
            where TItem : ILogItem
        {
            var scrollingBehavior = ScrollToItem
                ? (IAppendScrollingBehavior<TItem>) new ScrollToNewScrollingBehavior<TItem>()
                : new PreservePositionScrollingBehavior<TItem>();

            Data.Edit(list => list.Append(Items), scrollingBehavior, Animate);
        }

        public static void Clear<TItem>(this IListDataViewModel<TItem> Data)
            where TItem : ILogItem
        {
            Data.Edit(l => l.Clear(), new PreservePositionScrollingBehavior<TItem>(), false);
        }
    }
}