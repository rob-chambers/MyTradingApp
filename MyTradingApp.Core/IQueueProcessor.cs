using System;

namespace MyTradingApp.Core
{
    public interface IQueueProcessor
    {
        void Enqueue(Action job);
    }
}
