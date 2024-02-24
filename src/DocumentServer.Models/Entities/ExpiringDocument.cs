using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;

namespace SlugEnt.DocumentServer.Models.Entities
{
    /// <summary>
    /// Keeps track of planned Document Expiration Times
    /// </summary>
    public class ExpiringDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("StoredDocument")]
        public long StoredDocumentId { get; set; }

        /// <summary>
        /// When the document Expires
        /// </summary>
        public DateTime ExpirationDateUtcDateTime { get; set; }


        public StoredDocument StoredDocument;


        /// <summary>
        /// Constructor - Date is automatically calculated.  For ParentDefined Lifetime the 2nd parameter is expected
        /// </summary>
        /// <param name="documentLifetime"></param>
        /// <param name="expirationDateOnlySetForParentLifetime">This is only used when the EnumLifetime value is Parent Type.  If not set and type is ParentDetermined then 1 year is used</param>
        public static Result<ExpiringDocument> Create(EnumDocumentLifetimes documentLifetime,
                                                      DateTime? expirationDateOnlySetForParentLifetime = null)
        {
            ExpiringDocument expiringDocument = new();

            // Calculate the expiration date.
            expiringDocument.ExpirationDateUtcDateTime = documentLifetime switch
            {
                EnumDocumentLifetimes.Never       => DateTime.MaxValue,
                EnumDocumentLifetimes.HoursOne    => DateTime.UtcNow.AddHours(1),
                EnumDocumentLifetimes.HoursFour   => DateTime.UtcNow.AddHours(4),
                EnumDocumentLifetimes.HoursTwelve => DateTime.UtcNow.AddHours(12),
                EnumDocumentLifetimes.DayOne      => DateTime.UtcNow.AddDays(1),
                EnumDocumentLifetimes.WeekOne     => DateTime.UtcNow.AddDays(7),
                EnumDocumentLifetimes.MonthOne    => DateTime.UtcNow.AddMonths(1),
                EnumDocumentLifetimes.MonthsThree => DateTime.UtcNow.AddMonths(3),
                EnumDocumentLifetimes.MonthsSix   => DateTime.UtcNow.AddMonths(6),
                EnumDocumentLifetimes.YearOne     => DateTime.UtcNow.AddYears(1),
                EnumDocumentLifetimes.YearsTwo    => DateTime.UtcNow.AddYears(2),
                EnumDocumentLifetimes.YearsThree  => DateTime.UtcNow.AddYears(3),
                EnumDocumentLifetimes.YearsFour   => DateTime.UtcNow.AddYears(4),
                EnumDocumentLifetimes.YearsSeven  => DateTime.UtcNow.AddYears(7),
                EnumDocumentLifetimes.YearsTen    => DateTime.UtcNow.AddYears(10),
                EnumDocumentLifetimes.ParentDetermined => expirationDateOnlySetForParentLifetime != null
                                                              ? (DateTime)expirationDateOnlySetForParentLifetime
                                                              : DateTime.UtcNow.AddYears(1),
                _ => DateTime.MinValue
            };

            if (expiringDocument.ExpirationDateUtcDateTime == DateTime.MinValue)
                return Result.Fail(new Error("Unknown DocumentLifetime value of [ " + documentLifetime + " ]"));

            return Result.Ok(expiringDocument);
        }
    }
}