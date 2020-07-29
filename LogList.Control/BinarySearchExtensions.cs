using System;
using System.Collections.Generic;
using LogList.Control.Manipulation.Implementations;

namespace LogList.Control
{
    public static class BinarySearchExtensions
    {
        public enum ItemNotFoundBehavior
        {
            ReturnMinusOne,
            ReturnClosestTimeIndex
        }

        public static int BinarySearch<TLogRecord>(
            this IList<TLogRecord> Collection, TLogRecord Item,
            ItemNotFoundBehavior ItemNotFoundBehavior = ItemNotFoundBehavior.ReturnMinusOne)
            where TLogRecord : INumbered
        {
            return BinarySearch(Collection, Item, 0, Collection.Count - 1, ItemNotFoundBehavior);
        }

        public static int BinarySearch<TLogRecord>(
            this IList<TLogRecord> Collection, TLogRecord Item, int StartIndex, int EndIndex,
            ItemNotFoundBehavior ItemNotFoundBehavior = ItemNotFoundBehavior.ReturnMinusOne)
            where TLogRecord : INumbered
        {
            if (Collection.Count == 0)
                return -1;

            StartIndex = Math.Max(0, StartIndex);
            EndIndex   = Math.Min(EndIndex, Collection.Count - 1);

            if (Item.Number < Collection[StartIndex].Number)
                return -1;
            if (Item.Number > Collection[EndIndex].Number)
                return -1;

            while (EndIndex - StartIndex > 2)
            {
                var middleIndex = (StartIndex + EndIndex) / 2;
                if (Collection[middleIndex].Number > Item.Number)
                    EndIndex = middleIndex;
                else
                    StartIndex = middleIndex;
            }

            int i;
            for (i = StartIndex; Collection[i].Number <= Item.Number; i++)
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