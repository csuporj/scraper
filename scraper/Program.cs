namespace scraper
{
    internal partial class Program
    {
        private static async Task Main()
        {
            const string userAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

            Directory.CreateDirectory(Settings.ThumbFolder);
            List<AlbumInfo> rssAlbums = await RssParser.GetAlbumsFromRss(Settings.RssUrl);
            List<AlbumInfo> jsonAlbums = JsonHandler.ReadJson(Settings.JsonPath);
            List<AlbumInfo> mergedAlbums = Merger.MergeRssWithJson(rssAlbums, jsonAlbums);

            var missingAlbums = mergedAlbums
               .Where(a => string.IsNullOrEmpty(a.ThumbnailUrl) || a.AlbumDate == AlbumInfo.NotScraped)
               .Take(Settings.BatchSize)
               .ToList();

            if (missingAlbums.Count > 0)
            {
                Console.WriteLine($"Bulk scraping {missingAlbums.Count} albums...");
                using HttpClient client = new();
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);

                foreach (var item in missingAlbums)
                {
                    var (date, thumbUrl) = await RssParser.GetAlbumMetadata(client, item.AlbumUrl);
                    item.AlbumDate = date;
                    item.ThumbnailUrl = thumbUrl;

                    if (!string.IsNullOrEmpty(thumbUrl))
                    {
                        string fileName = $"thumb_{Guid.NewGuid():N}.jpg";
                        string fullPath = Path.Combine(Settings.ThumbFolder, fileName);
                        try
                        {
                            var bytes = await client.GetByteArrayAsync(thumbUrl);
                            await File.WriteAllBytesAsync(fullPath, bytes);
                            item.LocalThumbnailPath = fullPath;
                        }
                        catch
                        {
                            Console.WriteLine($"Failed thumb download: {item.LinkText}");
                        }
                    }

                    Console.WriteLine($"Updated: {item.LinkText} -> {date}");

                    // don't get blocked by google
                    await Task.Delay(Settings.DelayBetweenRequestsMs);
                }
            }

            await JsonHandler.WriteJson(mergedAlbums);
            Console.WriteLine($"Process finished. Total albums in list: {mergedAlbums.Count}");
        }
    }
}
