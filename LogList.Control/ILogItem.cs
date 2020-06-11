using System;

namespace LogList.Control
{
    public interface ILogItem
    {
        int      Id   { get; }
        DateTime Time { get; }
    }
}