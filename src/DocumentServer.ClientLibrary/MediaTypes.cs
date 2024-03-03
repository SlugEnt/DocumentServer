using System.Diagnostics;
using System.Net.Mime;
using SlugEnt.DocumentServer.Models.Enums;

namespace SlugEnt.DocumentServer.ClientLibrary
{
    public class MediaTypes
    {
        public static string GetContentType(EnumMediaTypes mediaType) =>
            mediaType switch
            {
                EnumMediaTypes.Pdf       => MediaTypeNames.Application.Pdf,
                EnumMediaTypes.Png       => MediaTypeNames.Image.Png,
                EnumMediaTypes.Jpeg      => MediaTypeNames.Image.Jpeg,
                EnumMediaTypes.Tiff      => MediaTypeNames.Image.Tiff,
                EnumMediaTypes.Zip       => MediaTypeNames.Application.Zip,
                EnumMediaTypes.Bmp       => MediaTypeNames.Image.Bmp,
                EnumMediaTypes.Csv       => MediaTypeNames.Text.Csv,
                EnumMediaTypes.Html      => MediaTypeNames.Text.Html,
                EnumMediaTypes.Json      => MediaTypeNames.Application.Json,
                EnumMediaTypes.Xml       => MediaTypeNames.Application.Xml,
                EnumMediaTypes.WebP      => MediaTypeNames.Image.Webp,
                EnumMediaTypes.PlainText => MediaTypeNames.Text.Plain,
                EnumMediaTypes.RichText  => MediaTypeNames.Text.RichText,
                EnumMediaTypes.Rtf       => MediaTypeNames.Application.Rtf,
                EnumMediaTypes.MarkDown  => MediaTypeNames.Text.Markdown,

                EnumMediaTypes.Excel        => MediaTypeNames.Application.Octet,
                EnumMediaTypes.Word         => MediaTypeNames.Application.Octet,
                EnumMediaTypes.OutlookEmail => MediaTypeNames.Application.Octet,
                EnumMediaTypes.Other        => MediaTypeNames.Application.Octet,
            };


        /// <summary>
        /// Returns a MediaType based upon File Extension
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        public static EnumMediaTypes GetMediaType(string fileExtension) =>
            fileExtension switch
            {
                "pdf"  => EnumMediaTypes.Pdf,
                "png"  => EnumMediaTypes.Png,
                "jpg"  => EnumMediaTypes.Jpeg,
                "jpeg" => EnumMediaTypes.Jpeg,
                "tiff" => EnumMediaTypes.Tiff,
                "zip"  => EnumMediaTypes.Zip,
                "xls"  => EnumMediaTypes.Excel,
                "doc"  => EnumMediaTypes.Word,
                "docx" => EnumMediaTypes.Word,
                "msg"  => EnumMediaTypes.OutlookEmail,
                "bmp"  => EnumMediaTypes.Bmp,
                "csv"  => EnumMediaTypes.Csv,
                "html" => EnumMediaTypes.Html,
                "json" => EnumMediaTypes.Json,
                "xml"  => EnumMediaTypes.Xml,
                "webp" => EnumMediaTypes.WebP,
                "txt"  => EnumMediaTypes.PlainText,
                "rtf"  => EnumMediaTypes.Rtf,
                "md"   => EnumMediaTypes.MarkDown,
                _      => EnumMediaTypes.Other,
            };
    }
}