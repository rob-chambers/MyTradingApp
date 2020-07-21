/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

namespace MyTradingApp.Messages
{
    public class ConnectionStatusMessage
    {       
        public ConnectionStatusMessage(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; private set; }
    }
}
