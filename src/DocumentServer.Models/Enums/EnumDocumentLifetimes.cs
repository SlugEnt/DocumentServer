namespace SlugEnt.DocumentServer.Models.Enums;

/// <summary>
/// Lifetime of a document after it is considered closed.  Meaning for a temporary document its closure clock starts at the day its created.
/// For other documents, it's when its parent entity, ie, a claim, referral, etc is considered closed. This is when its clock starts.
/// </summary>
public enum EnumDocumentLifetimes
{
    HoursOne    = 3,
    HoursFour   = 4,
    HoursTwelve = 12,
    DayOne      = 14,
    WeekOne     = 20,
    MonthOne    = 22,
    MonthsThree = 30,
    MonthsSix   = 35,
    YearOne     = 101,
    YearsTwo    = 102,
    YearsThree  = 103,
    YearsFour   = 104,
    YearsSeven  = 107,
    YearsTen    = 110,

    /// <summary>
    /// There is no preset value, the parent will determine
    /// </summary>
    ParentDetermined = 250,

    /// <summary>
    /// Document is not considered to not have any value set, so it will never expire
    /// </summary>
    Never = 254,
}