namespace scraper
{
    internal class MissingAlbumInfoHandler
    {
        public static async Task FillInfo(List<AlbumInfo> mergedAlbums)
        {
            const string userAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

            List<AlbumInfo> missingAlbums = SelectMissingAlbums(mergedAlbums);
            // todo if count is zero, get the topmost 5 albums by date and refresh their text, date and thumbnails
            // Until then the workaround is to checkout the gallery repo, set ThumbnailFileName to "" in albums.json, and commit the change.
            if (missingAlbums.Count == 0)
            {
                return;
            }

            Logger.Log($"Bulk scraping {missingAlbums.Count} albums...");
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            foreach (AlbumInfo item in missingAlbums)
            {
                (string? date, string? thumbnailUrl) = await GooglePhotosParser.GetAlbum(item.AlbumUrl, client);
                thumbnailUrl = AdjustQuality(thumbnailUrl);
                await FillInfo(item, date, thumbnailUrl, client);
            }
        }

        private static string AdjustQuality(string thumbnailUrl)
        {
            int equals = thumbnailUrl.LastIndexOf('=');
            
            if (equals > 0)
            {
                string fixedPart = thumbnailUrl[..(equals + 1)];
                thumbnailUrl = fixedPart + Settings.ThumbnailQuality;
            }
            
            return thumbnailUrl;
        }

        private static async Task FillInfo(AlbumInfo album, string date, string thumbnailUrl, HttpClient client)
        {
            album.AlbumDate = date;
            album.ThumbnailUrl = thumbnailUrl;

            try
            {
                album.ThumbnailFileName = await ThumbnailJpgHandler.Download(thumbnailUrl, client);
            }
            catch (Exception ex)
            {
                album.ThumbnailFileName = "";
                Logger.Log(ex.Message);
                Logger.Log($"Failed thumbnail download: {album.LinkText}");
            }

            Logger.Log($"Updated: {album.LinkText} -> {date}");
        }

        private static List<AlbumInfo> SelectMissingAlbums(List<AlbumInfo> mergedAlbums)
        {
            return [.. mergedAlbums
                .Where(a => string.IsNullOrWhiteSpace(a.ThumbnailFileName))
               .Take(Settings.BatchSize)];
        }
    }
}
