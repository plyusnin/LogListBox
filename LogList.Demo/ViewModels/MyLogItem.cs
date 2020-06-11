using System;
using LogList.Control;

namespace LogList.Demo.ViewModels
{
    public class MyLogItem : ILogItem
    {
        public MyLogItem(int Id, DateTime Time)
        {
            this.Id = Id;
            this.Time = Time;
        }
        public int Id { get; }
        public DateTime Time { get; }

        public override string ToString()
        {
            return $"{Time} (#{Id})";
        }
    }
}