namespace scraper
{
    internal class MissingAlbumInfoHandler
    {
        public static async Task FillInfo(List<AlbumInfo> mergedAlbums)
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

            foreach (AlbumInfo item in missingAlbums)
            {
                (string? date, string? thumbUrl) = await RssParser.GetAlbum(item.AlbumUrl, client);
                await FillInfo(item, date, thumbUrl, client);
            }
        }

        private static async Task FillInfo(AlbumInfo album, string date, string thumbUrl, HttpClient client)
        {
            album.AlbumDate = date;
            album.ThumbnailUrl = thumbUrl;

            if (!string.IsNullOrEmpty(thumbUrl))
            {
                string fileName = $"thumb_{Guid.NewGuid():N}.jpg";
                string fullPath = Path.Combine(Settings.ThumbnailsFolder, fileName);
                try
                {
                    byte[] bytes = await client.GetByteArrayAsync(thumbUrl);
                    await File.WriteAllBytesAsync(fullPath, bytes);
                    album.LocalThumbnailPath = fullPath;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message);
                    Logger.Log($"Failed thumbnail download: {album.LinkText}");
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
