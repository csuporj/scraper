using HtmlAgilityPack;
using System.Xml.Linq;

namespace scraper
{
    internal static partial class RssParser
    {
        public static async Task<List<AlbumInfo>> GetAlbums()
        {
            const string userAgent = "Mozilla/5.0";
            
            Logger.Log("Fetching RSS feed...");
            List<AlbumInfo> albums = [];
            HashSet<string> seenAlbums = [];
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

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
    }
}
