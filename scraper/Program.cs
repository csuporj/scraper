using System.Text.Json;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

// Using .NET 10 Primary Constructor to handle initialization and non-nullability
public class AlbumInfo(string linkText, string albumUrl)
{
    public string LinkText { get; set; } = linkText;
    public string AlbumUrl { get; set; } = albumUrl;
    public string AlbumDate { get; set; } = "Not Scraped";
    public string ThumbnailUrl { get; set; } = "";
}

class Program
{
    static async Task Main()
    {
        const string blogUrl = "https://csuporj.blogspot.com";
        string rssUrl = $"{blogUrl}/feeds/posts/default?alt=rss&max-results=500";

        Console.WriteLine("Step 1: Fetching links from RSS...");
        var allAlbums = await GetAlbums(rssUrl);

        // Take 3 for deep scraping
        var top3 = allAlbums.Take(3).ToList();

        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        Console.WriteLine("Step 2: Scraping metadata for the first 3...");
        foreach (var item in top3)
        {
            var (date, thumb) = await GetAlbumMetadata(client, item.AlbumUrl);
            item.AlbumDate = date;
            item.ThumbnailUrl = thumb;
            Console.WriteLine($"Processed: {item.LinkText} -> {item.AlbumDate}");
        }

        // Combine everything (top 3 with data + the rest)
        var finalData = top3.Concat(allAlbums.Skip(3)).ToList();

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        string jsonOutput = JsonSerializer.Serialize(new { Blog = blogUrl, Albums = finalData }, jsonOptions);

        await File.WriteAllTextAsync("albums.json", jsonOutput);
        Console.WriteLine("\nSuccess! Results saved to albums.json");
    }

    static async Task<(string date, string thumb)> GetAlbumMetadata(HttpClient client, string url)
    {
        try
        {
            string html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Thumbnail
            var metaImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
            string thumb = metaImage?.GetAttributeValue("content", "") ?? "";

            // Date logic
            string dateStr = "Date Not Found";
            var metaTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            if (metaTitle is { } titleNode)
            {
                string content = titleNode.GetAttributeValue("content", "");
                var dateMatch = Regex.Match(content, @"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2}\b", RegexOptions.IgnoreCase);
                if (dateMatch.Success)
                {
                    dateStr = dateMatch.Value;
                    if (!Regex.IsMatch(content, @"\b\d{4}\b"))
                        dateStr = $"{dateStr}, {DateTime.Now.Year}";
                    else
                    {
                        var fullDate = Regex.Match(content, @"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},?\s+\d{4}\b", RegexOptions.IgnoreCase);
                        if (fullDate.Success) dateStr = fullDate.Value;
                    }
                }
            }
            return (dateStr, thumb);
        }
        catch { return ("Error", ""); }
    }

    static async Task<List<AlbumInfo>> GetAlbums(string url)
    {
        var list = new List<AlbumInfo>();
        var seen = new HashSet<string>();
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        try
        {
            string xml = await client.GetStringAsync(url);
            XDocument doc = XDocument.Parse(xml);
            foreach (var item in doc.Descendants("item"))
            {
                string content = item.Element("description")?.Value ?? "";
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);
                var nodes = htmlDoc.DocumentNode.SelectNodes("//a[contains(@href, 'photos.app.goo.gl') or contains(@href, 'photos.google.com')]");

                if (nodes is not null)
                {
                    foreach (var link in nodes)
                    {
                        string href = link.GetAttributeValue("href", "").Trim();
                        string text = HtmlEntity.DeEntitize(link.InnerText).Trim();
                        if (!string.IsNullOrEmpty(href) && seen.Add(href))
                            list.Add(new AlbumInfo(text, href));
                    }
                }
            }
        }
        catch { }
        return list;
    }
}
