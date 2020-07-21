using System;
using System.Collections.Generic;

namespace MyTradingApp.Domain
{
    public sealed class BarCollection : Dictionary<DateTime, Bar>
    {
    }
}