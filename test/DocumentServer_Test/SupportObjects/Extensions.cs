namespace Test_DocumentServer.SupportObjects;

public static class Extension
{
    // Extend DateTime to have a Range Check.  Returns true if the datetime is within the start and end dates
    public static bool IsInRange(this DateTime date,
                                 DateTime start,
                                 DateTime end) =>
        date >= start && date <= end;
}