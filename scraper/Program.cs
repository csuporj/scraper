using System.Text.Json;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

public class AlbumInfo(string linkText, string albumUrl)
{
    public string LinkText { get; set; } = linkText;
    public string AlbumUrl { get; set; } = albumUrl;
    public string AlbumDate { get; set; } = "Not Scraped";
    public string ThumbnailUrl { get; set; } = "";
    public string? LocalThumbnailPath { get; set; }
}

class Program
{
    static async Task Main()
    {
        const string blogUrl = "https://csuporj.blogspot.com";
        const string jsonPath = "albums.json";
        const string thumbFolder = "thumbnails";
        string rssUrl = $"{blogUrl}/feeds/posts/default?alt=rss&max-results=500";

        Directory.CreateDirectory(thumbFolder);

        // 1. Read existing local data
        var localCache = new Dictionary<string, AlbumInfo>();
        if (File.Exists(jsonPath))
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                var doc = JsonDocument.Parse(jsonContent);
                var albums = JsonSerializer.Deserialize<List<AlbumInfo>>(doc.RootElement.GetProperty("Albums").GetRawText());
                if (albums != null) localCache = albums.ToDictionary(a => a.AlbumUrl, a => a);
            }
            catch { Console.WriteLine("Existing JSON not found or invalid. Starting fresh."); }
        }

        // 2. Fetch latest RSS (Source of Truth for Order)
        Console.WriteLine("Fetching RSS feed...");
        var feedAlbums = await GetAlbumsFromFeed(rssUrl);

        // 3. Sync Feed Order with Local Data
        var finalOrderedList = new List<AlbumInfo>();
        foreach (var item in feedAlbums)
        {
            if (localCache.TryGetValue(item.AlbumUrl, out var existing))
            {
                existing.LinkText = item.LinkText; // Sync text update
                finalOrderedList.Add(existing);
            }
            else
            {
                finalOrderedList.Add(item);
            }
        }

        // 4. Bulk Scrape: Identify first 100 missing data
        var missingData = finalOrderedList
            .Where(a => string.IsNullOrEmpty(a.ThumbnailUrl) || a.AlbumDate == "Not Scraped")
            .Take(100) // CHANGED FROM 3 TO 100
            .ToList();

        if (missingData.Count > 0)
        {
            Console.WriteLine($"Bulk scraping {missingData.Count} albums...");
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

            foreach (var item in missingData)
            {
                var (date, thumbUrl) = await GetAlbumMetadata(client, item.AlbumUrl);
                item.AlbumDate = date;
                item.ThumbnailUrl = thumbUrl;

                if (!string.IsNullOrEmpty(thumbUrl))
                {
                    string fileName = $"thumb_{Guid.NewGuid():N}.jpg";
                    string fullPath = Path.Combine(thumbFolder, fileName);
                    try
                    {
                        var bytes = await client.GetByteArrayAsync(thumbUrl);
                        await File.WriteAllBytesAsync(fullPath, bytes);
                        item.LocalThumbnailPath = fullPath;
                    }
                    catch { Console.WriteLine($"Failed thumb download: {item.LinkText}"); }
                }

                Console.WriteLine($"Updated: {item.LinkText} -> {date}");

                // Small delay to be polite to Google's servers
                await Task.Delay(200);
            }
        }

        // 5. Final Save
        var output = new { Blog = blogUrl, Albums = finalOrderedList };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"\nProcess finished. Total albums in list: {finalOrderedList.Count}");
    }

    static async Task<(string date, string thumb)> GetAlbumMetadata(HttpClient client, string url)
    {
        try
        {
            string html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string thumb = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", "") ?? "";
            string dateStr = "Date Not Found";

            if (doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']") is { } node)
            {
                string content = node.GetAttributeValue("content", "");
                var match = Regex.Match(content, @"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2}\b", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    dateStr = match.Value;
                    if (!Regex.IsMatch(content, @"\b\d{4}\b")) dateStr += $", {DateTime.Now.Year}";
                    else
                    {
                        var full = Regex.Match(content, @"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},?\s+\d{4}\b", RegexOptions.IgnoreCase);
                        if (full.Success) dateStr = full.Value;
                    }
                }
            }
            return (dateStr, thumb);
        }
        catch { return ("Error", ""); }
    }

    static async Task<List<AlbumInfo>> GetAlbumsFromFeed(string url)
    {
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
                            list.Add(new AlbumInfo(text, href));
                    }
                }
            }
        }
        catch { }
        return list;
    }
}
