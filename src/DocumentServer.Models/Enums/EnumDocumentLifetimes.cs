using System.ComponentModel.DataAnnotations;

namespace SlugEnt.DocumentServer.Models.Enums;

/// <summary>
///     Lifetime of a document after it is considered closed.  Meaning for a temporary document its closure clock starts at
///     the day its created.
///     For other documents, it's when its parent entity, ie, a claim, referral, etc is considered closed. This is when its
///     clock starts.
/// </summary>
public enum EnumDocumentLifetimes
{
    [Display(Description = "1 Hour")]    HoursOne    = 3,
    [Display(Description = "4 Hours")]   HoursFour   = 4,
    [Display(Description = "12 Hours")]  HoursTwelve = 12,
    [Display(Description = "1 Day")]     DayOne      = 14,
    [Display(Description = "1 Week")]    WeekOne     = 20,
    [Display(Description = "1 Month")]   MonthOne    = 22,
    [Display(Description = "3 Months")]  MonthsThree = 30,
    [Display(Description = "6 Months")]  MonthsSix   = 35,
    [Display(Description = "1 Year")]    YearOne     = 101,
    [Display(Description = "18 Months")] Months18    = 102,
    [Display(Description = "2 Years")]   YearsTwo    = 120,
    [Display(Description = "3 Years")]   YearsThree  = 130,
    [Display(Description = "4 Years")]   YearsFour   = 140,
    [Display(Description = "5 Years")]   YearsFive   = 150,
    [Display(Description = "7 Years")]   YearsSeven  = 170,
    [Display(Description = "10 Years")]  YearsTen    = 200,

    /// <summary>
    ///     There is no preset value, the parent will determine
    /// </summary>
    [Display(Description = "Parent Save Determined")]
    ParentDetermined = 250,

    /// <summary>
    ///     Document is not considered to not have any value set, so it will never expire
    /// </summary>
    Never = 254
}