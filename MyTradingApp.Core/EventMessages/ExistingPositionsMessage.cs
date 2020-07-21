﻿using MyTradingApp.ViewModels;
using System.Collections.Generic;

namespace MyTradingApp.EventMessages
{
    public class ExistingPositionsMessage
    {
        public ExistingPositionsMessage(IEnumerable<PositionItem> positions)
        {
            Positions = positions;
        }

        public IEnumerable<PositionItem> Positions { get; }
    }
}