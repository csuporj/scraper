using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace scraper
{
    internal static partial class RssParser
    {
        public static async Task<List<AlbumInfo>> GetAlbums()
        {
            Logger.Log("Fetching RSS feed...");

            List<AlbumInfo> albums = [];
            HashSet<string> seenAlbums = [];
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            try
            {
                XDocument rss = XDocument.Parse(await client.GetStringAsync(Settings.RssUrl));
                foreach (XElement rssItem in rss.Descendants("item"))
                {
                    GetAlbums(albums, seenAlbums, rssItem);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }

            return albums;
        }

        private static void GetAlbums(List<AlbumInfo> albums, HashSet<string> seen, XElement rssItem)
        {
            const string linkPattern =
                "//a[contains(@href, 'photos.app.goo.gl') or contains(@href, 'photos.google.com') or contains(@href, 'goo.gl/photos')]";

            HtmlDocument html = new();
            html.LoadHtml(rssItem.Element("description")?.Value ?? "");
            HtmlNodeCollection links = html.DocumentNode.SelectNodes(linkPattern);

            if (links != null)
            {
                foreach (HtmlNode link in links)
                {
                    string href = link.GetAttributeValue("href", "").Trim();
                    string text = HtmlEntity.DeEntitize(link.InnerText).Trim();
                    if (!string.IsNullOrEmpty(href) && !seen.Contains(href))
                    {
                        seen.Add(href);
                        albums.Add(new AlbumInfo(text, href));
                    }
                }
            }
        }

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

            Match mdMatch = MonthDayRegex().Match(title);
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
