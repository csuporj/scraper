using System.Xml.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;

public class AlbumInfo(string linkText, string albumUrl)
{
    public string LinkText { get; set; } = linkText;
    public string AlbumUrl { get; set; } = albumUrl;
    public string AlbumDate { get; set; } = "Not Scraped";
    public string ThumbnailUrl { get; set; } = "";
    public string LocalThumbnailPath { get; set; } = "";
}

public partial class Program
{
    private const string RssUrl = "https://csuporj.blogspot.com/feeds/posts/default?alt=rss&max-results=500";
    private const string JsonPath = "albums.json";
    private const string ThumbFolder = "thumbnails";
    private const int BatchSize = 5;
    
    private static async Task Main()
    {

        Directory.CreateDirectory(ThumbFolder);
        Dictionary<string, AlbumInfo> localAlbums = ReadJson(JsonPath);
        List<AlbumInfo> feedAlbums = await GetAlbumsFromFeed(RssUrl);
        List<AlbumInfo> mergedAlbums = MergeJsonWithRss(localAlbums, feedAlbums);

         var missingAlbums = mergedAlbums
            .Where(a => string.IsNullOrEmpty(a.ThumbnailUrl) || a.AlbumDate == "Not Scraped")
            .Take(BatchSize)
            .ToList();

        if (missingAlbums.Count > 0)
        {
            Console.WriteLine($"Bulk scraping {missingAlbums.Count} albums...");
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

            foreach (var item in missingAlbums)
            {
                var (date, thumbUrl) = await GetAlbumMetadata(client, item.AlbumUrl);
                item.AlbumDate = date;
                item.ThumbnailUrl = thumbUrl;

                if (!string.IsNullOrEmpty(thumbUrl))
                {
                    string fileName = $"thumb_{Guid.NewGuid():N}.jpg";
                    string fullPath = Path.Combine(ThumbFolder, fileName);
                    try
                    {
                        var bytes = await client.GetByteArrayAsync(thumbUrl);
                        await File.WriteAllBytesAsync(fullPath, bytes);
                        item.LocalThumbnailPath = fullPath;
                    }
                    catch { Console.WriteLine($"Failed thumb download: {item.LinkText}"); }
                }

                Console.WriteLine($"Updated: {item.LinkText} -> {date}");

                // don't get blocked by google
                await Task.Delay(200);
            }
        }

        // 5. Final Save
        var output = JsonConvert.SerializeObject(mergedAlbums, Formatting.Indented);
        await File.WriteAllTextAsync(JsonPath, output);
        Console.WriteLine($"\nProcess finished. Total albums in list: {mergedAlbums.Count}");
    }

    private static Dictionary<string, AlbumInfo> ReadJson(string jsonPath)
    {
        var localCache = new Dictionary<string, AlbumInfo>();
        if (File.Exists(jsonPath))
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var albums = JsonConvert.DeserializeObject<AlbumInfo[]>(json);
                if (albums != null)
                    localCache = albums.ToDictionary(a => a.AlbumUrl, a => a);
            }
            catch { Console.WriteLine("Existing JSON not found or invalid. Starting fresh."); }
        }

        return localCache;
    }

    private static async Task<List<AlbumInfo>> GetAlbumsFromFeed(string url)
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
                            list.Add(new AlbumInfo(text, href));
                    }
                }
            }
        }
        catch { }
        return list;
    }

    private static List<AlbumInfo> MergeJsonWithRss(Dictionary<string, AlbumInfo> localList, List<AlbumInfo> feedList)
    {
        var mergedList = new List<AlbumInfo>();
        foreach (var feedAlbum in feedList)
        {
            if (localList.TryGetValue(feedAlbum.AlbumUrl, out var localAlbum))
            {
                localAlbum.LinkText = feedAlbum.LinkText;
                mergedList.Add(localAlbum);
            }
            else
            {
                mergedList.Add(feedAlbum);
            }
        }

        return mergedList;
    }

    private static async Task<(string date, string thumb)> GetAlbumMetadata(HttpClient client, string url)
    {
        const string imageNodePath = "//meta[@property='og:image']";
        const string titleNodePath = "//meta[@property='og:title']";        
        
        try
        {
            string html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode imageNode = doc.DocumentNode.SelectSingleNode(imageNodePath);

            string thumb = imageNode?.GetAttributeValue("content", "") ?? "";
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode(titleNodePath);

            string dateStr = GetDateFromTitle(titleNode);
            return (dateStr, thumb);
        }
        catch { return ("Error", ""); }
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
            return  $"{mdMatch.Value}, {DateTime.Now.Year}";
        }

        return dateNotFound;
    }

    [GeneratedRegex(@"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},?\s+\d{4}\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MonthDayYearRegex();

    [GeneratedRegex(@"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2}\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MonthDayRegex();
    
}
