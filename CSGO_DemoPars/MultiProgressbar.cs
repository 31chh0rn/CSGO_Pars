using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars
{
    class MultiProgressbar
    {
        static private readonly object _sync = new object();

        public static void UpdateProgress(int line, string item, float progress)
        {
            float percentage = progress <= 1 ? (int)100.0 * progress : 100;
            lock (_sync) {
                Console.CursorLeft = 0;
                Console.CursorTop = line;
                Console.Write(item + " [" + new string('=', (int)percentage / 4) + "] " + percentage.ToString("0.00") + "%");
            }
        }
    }
}
