using System;
using LogList.Control;

namespace LogList.Demo.ViewModels
{
    public class MyLogItem : ILogItem
    {
        public MyLogItem(int Id, DateTime Time, int SourceNumber, string Text)
        {
            this.Id           = Id;
            this.Time         = Time;
            this.SourceNumber = SourceNumber;
            this.Text         = Text;
        }

        public int      SourceNumber { get; }
        public int      Id           { get; }
        public string   Text         { get; }
        public DateTime Time         { get; }

        public override string ToString()
        {
            return $"{Time} (#{Id}, source: {SourceNumber})";
        }
    }
}