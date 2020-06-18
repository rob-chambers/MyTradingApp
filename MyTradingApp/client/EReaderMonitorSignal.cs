/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System.Diagnostics;
using System.Threading;

namespace IBApi
{
    public class EReaderMonitorSignal : EReaderSignal
    {
        public EReaderMonitorSignal()
        {
            Debug.WriteLine("Instantiating EReaderMonitorSignal");
        }

        object cs = new object();
        bool open = false;

        public void issueSignal()
        {
            lock (cs)
            {
                open = true;

                Monitor.PulseAll(cs);
            }
        }

        public void waitForSignal()
        {
            lock (cs)
            {
                while (!open)
                    Monitor.Wait(cs);

                open = false;
            }
        }
    }
}
