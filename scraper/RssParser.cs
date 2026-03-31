using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace scraper
{
    internal static partial class RssParser
    {
        public static async Task<List<AlbumInfo>> GetAlbumsFromRss(string url)
        {
            Console.WriteLine("Fetching RSS feed...");

            var list = new List<AlbumInfo>();
            var seen = new HashSet<string>();
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            try
            {
                XDocument doc = XDocument.Parse(await client.GetStringAsync(url));
                foreach (var item in doc.Descendants("item"))
                {
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(item.Element("description")?.Value ?? "");
                    var nodes = htmlDoc.DocumentNode.SelectNodes("//a[contains(@href, 'photos.app.goo.gl') or contains(@href, 'photos.google.com')]");
                    if (nodes != null)
                    {
                        foreach (var link in nodes)
                        {
                            string href = link.GetAttributeValue("href", "").Trim();
                            string text = HtmlEntity.DeEntitize(link.InnerText).Trim();
                            if (!string.IsNullOrEmpty(href) && seen.Add(href))
                            {
                                list.Add(new AlbumInfo(text, href));
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            
            return list;
        }

        public static async Task<(string date, string thumb)> GetAlbumMetadata(HttpClient client, string url)
        {
            const string titleNodePath = "//meta[@property='og:title']";
            const string imageNodePath = "//meta[@property='og:image']";

            try
            {
                string html = await client.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                HtmlNode titleNode = doc.DocumentNode.SelectSingleNode(titleNodePath);
                HtmlNode imageNode = doc.DocumentNode.SelectSingleNode(imageNodePath);

                string dateStr = GetDateFromTitle(titleNode);
                string thumb = imageNode?.GetAttributeValue("content", "") ?? "";

                return (dateStr, thumb);
            }
            catch
            {
                return ("Error", "");
            }
        }

        private static string GetDateFromTitle(HtmlNode titleNode)
        {
            const string dateNotFound = "Date not found";

            if (titleNode == null)
            {
                return dateNotFound;
            }

            string title = titleNode.GetAttributeValue("content", "");

            var mdyMatch = MonthDayYearRegex().Match(title);
            if (mdyMatch.Success)
            {
                return mdyMatch.Value;
            }

            var mdMatch = MonthDayRegex().Match(title);
            if (mdMatch.Success)
            {
                return $"{mdMatch.Value}, {DateTime.Now.Year}";
            }

            return dateNotFound;
        }

        [GeneratedRegex(@"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},?\s+\d{4}\b", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex MonthDayYearRegex();

        [GeneratedRegex(@"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2}\b", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex MonthDayRegex();
    }
}
