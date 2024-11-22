using System;
using System.Collections.Generic;
using System.Text;

namespace Alpiste.Utils
{
    public class Milliseconds
    {
        static public long ms()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
