using System;
using LogList.Control;
using LogList.Control.Manipulation.Implementations.Filtering;

namespace LogList.Demo.ViewModels
{
    public class MyLogItem : ILogItem, IFilterableByString
    {
        public MyLogItem(int Id, DateTime Time, int SourceNumber, string Text)
        {
            this.Id           = Id;
            this.Time         = Time;
            this.SourceNumber = SourceNumber;
            this.Text         = Text;

            FilterString = ToString();
        }

        public int    SourceNumber { get; }
        public int    Id           { get; }
        public string Text         { get; }

        public string   FilterString { get; }
        public DateTime Time         { get; }

        public override string ToString()
        {
            return $"{Time} (#{Id}, source: {SourceNumber})";
        }
    }
}