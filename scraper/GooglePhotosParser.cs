using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace scraper
{
    internal static partial class GooglePhotosParser
    {
        public static async Task<(string albumDate, string thumbnailUrl)> GetAlbum(string albumUrl, HttpClient client)
        {
            const string titleXmlPath = "//meta[@property='og:title']";
            const string imageXmlPath = "//meta[@property='og:image']";

            try
            {
                string html = await client.GetStringAsync(albumUrl);
                HtmlDocument doc = new();
                doc.LoadHtml(html);

                HtmlNode titleNode = doc.DocumentNode.SelectSingleNode(titleXmlPath);
                HtmlNode imageNode = doc.DocumentNode.SelectSingleNode(imageXmlPath);

                string dateStr = GetDate(titleNode);
                string thumbnailUrl = imageNode?.GetAttributeValue("content", "") ?? "";

                return (dateStr, thumbnailUrl);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                return ("Error", "");
            }
        }

        private static string GetDate(HtmlNode titleNode)
        {
            const string dateNotFound = "Date not found";

            if (titleNode == null)
            {
                return dateNotFound;
            }

            string title = titleNode.GetAttributeValue("content", "");

            Match mdyMatch = MonthDayYearRegex().Match(title);
            if (mdyMatch.Success)
            {
                return mdyMatch.Value;
            }

            Match yMatch = YearRegex().Match(title);
            string year = yMatch.Success ? yMatch.Value : DateTime.Now.Year.ToString();

            Match mdMatch = MonthDayRegex().Match(title);
            if (mdMatch.Success)
            {
                return $"{mdMatch.Value}, {year}";
            }

            return dateNotFound;
        }

        [GeneratedRegex(@"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},?\s+\d{4}\b", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex MonthDayYearRegex();

        [GeneratedRegex(@"\b\d{4}\b", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex YearRegex();

        [GeneratedRegex(@"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2}\b", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex MonthDayRegex();

    }
}
