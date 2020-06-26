using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LogList.Control
{
    public static class BinarySearchExtensions
    {
        public enum ItemNotFoundBehavior
        {
            ReturnMinusOne,
            ReturnClosestTimeIndex
        }

        public static int BinarySearch<TItem>(
            this IList<TItem> Collection, TItem Item,
            ItemNotFoundBehavior ItemNotFoundBehavior = ItemNotFoundBehavior.ReturnMinusOne)
            where TItem : ILogItem
        {
            return BinarySearch(Collection, Item, 0, Collection.Count - 1, ItemNotFoundBehavior);
        }

        public static int BinarySearch<TItem>(
            this IList<TItem> Collection, TItem Item, int StartIndex, int EndIndex,
            ItemNotFoundBehavior ItemNotFoundBehavior = ItemNotFoundBehavior.ReturnMinusOne)
            where TItem : ILogItem
        {
            if (Collection.Count == 0)
                return -1;

            StartIndex = Math.Max(0, StartIndex);
            EndIndex   = Math.Min(EndIndex, Collection.Count - 1);

            if (Item.Time < Collection[StartIndex].Time)
                return -1;
            if (Item.Time > Collection[EndIndex].Time)
                return -1;

            while (EndIndex - StartIndex > 2)
            {
                var middleIndex = (StartIndex + EndIndex) / 2;
                if (Collection[middleIndex].Time > Item.Time)
                    EndIndex = middleIndex;
                else
                    StartIndex = middleIndex;
            }

            int i;
            for (i = StartIndex; Collection[i].Time <= Item.Time; i++)
                if (Collection[i].Equals(Item))
                    return i;

            switch (ItemNotFoundBehavior)
            {
                case ItemNotFoundBehavior.ReturnMinusOne:
                    return -1;

                case ItemNotFoundBehavior.ReturnClosestTimeIndex:
                    return i;

                default:
                    throw new ArgumentException($"Unknown {nameof(ItemNotFoundBehavior)}",
                                                nameof(ItemNotFoundBehavior));
            }
        }
    }
}