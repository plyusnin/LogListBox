namespace LogList.Control.Manipulation.Implementations
{
    public interface INumbered
    {
        int Number { get; }
    }

    public struct LogRecord<TItem> : INumbered
        where TItem : ILogItem
    {
        public LogRecord(int Number, TItem Item)
        {
            this.Number = Number;
            this.Item   = Item;
        }

        public int   Number { get; }
        public TItem Item   { get; }

        public override string ToString()
        {
            return $"{Number}: {Item}";
        }

        public LogRecord ForgetType()
        {
            return new LogRecord(Number, Item);
        }
    }

    public struct LogRecord : INumbered
    {
        public LogRecord(int Number, ILogItem Item)
        {
            this.Number = Number;
            this.Item   = Item;
        }

        public int      Number { get; }
        public ILogItem Item   { get; }

        public LogRecord<TItem> RecallType<TItem>()
            where TItem : ILogItem
        {
            return new LogRecord<TItem>(Number, (TItem) Item);
        }

        public override string ToString()
        {
            return $"{Number}: {Item}";
        }
    }
}