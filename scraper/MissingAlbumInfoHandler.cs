namespace scraper
{
    internal class MissingAlbumInfoHandler
    {
        public static async Task FillMissingInfo(List<AlbumInfo> mergedAlbums)
        {
            const string userAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
            List<AlbumInfo> missingAlbums = GetMissingAlbums(mergedAlbums);

            // todo if count is zero, get the topmost 5 albums by date and refresh their text, date and thumbnails
            if (missingAlbums.Count == 0)
            {
                return;
            }
            
            Logger.Log($"Bulk scraping {missingAlbums.Count} albums...");
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            foreach (var item in missingAlbums)
            {
                var (date, thumbUrl) = await RssParser.GetAlbum(client, item.AlbumUrl);
                await FillMissingInfo(item, date, thumbUrl, client);
            }
        }

        private static async Task FillMissingInfo(AlbumInfo album, string date, string thumbUrl, HttpClient client)
        {
            album.AlbumDate = date;
            album.ThumbnailUrl = thumbUrl;

            if (!string.IsNullOrEmpty(thumbUrl))
            {
                string fileName = $"thumb_{Guid.NewGuid():N}.jpg";
                string fullPath = Path.Combine(Settings.ThumbFolder, fileName);
                try
                {
                    var bytes = await client.GetByteArrayAsync(thumbUrl);
                    await File.WriteAllBytesAsync(fullPath, bytes);
                    album.LocalThumbnailPath = fullPath;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message);
                    Logger.Log($"Failed thumb download: {album.LinkText}");
                }
            }

            Logger.Log($"Updated: {album.LinkText} -> {date}");
        }

        private static List<AlbumInfo> GetMissingAlbums(List<AlbumInfo> mergedAlbums)
        {
            return [.. mergedAlbums
                .Where(a => string.IsNullOrEmpty(a.ThumbnailUrl) || a.AlbumDate == AlbumInfo.NotScraped)
               .Take(Settings.BatchSize)];
        }
    }
}
