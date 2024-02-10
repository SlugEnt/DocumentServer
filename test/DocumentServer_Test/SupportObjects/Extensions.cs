using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_DocumentServer.SupportObjects;

public static class Extension
{
    // Extend DateTime to have a Range Check.  Returns true if the datetime is within the start and end dates
    public static bool IsInRange(this DateTime date,
                                 DateTime start,
                                 DateTime end)
    {
        return ((date >= start) && (date <= end));
    }
}