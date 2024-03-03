namespace SlugEnt.DocumentServer.Models.Enums;

public enum EnumMediaTypes
{
    Pdf          = 0,
    Jpeg         = 1,
    Tiff         = 2,
    Png          = 3,
    Xml          = 4,
    Json         = 5,
    Zip          = 6,
    Rtf          = 7,
    Bmp          = 8,
    WebP         = 9,
    Csv          = 10,
    Html         = 11,
    MarkDown     = 12,
    PlainText    = 13,
    RichText     = 14,
    OutlookEmail = 15,

    /// <summary>
    /// This means the PDF is password protected, with a randomly generated password.  Password is stored in DocumentServer
    /// </summary>
    SecurePdf = 16,
    Excel = 17,
    Word  = 18,

    Other        = 253,
    NotSpecified = 254
}