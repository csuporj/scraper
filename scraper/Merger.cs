namespace scraper
{
    internal static class Merger
    {
        public static List<AlbumInfo> Merge(List<AlbumInfo> rssAlbums, List<AlbumInfo> jsonAlbums) =>
            [.. rssAlbums.LeftJoin(jsonAlbums, r => r.AlbumUrl, j => j.AlbumUrl, Merge)];

        private static AlbumInfo Merge(AlbumInfo rss, AlbumInfo? json)
        {
            var merged = new AlbumInfo(rss.LinkText, rss.AlbumUrl);
            
            if (json != null)
            {
                merged.AlbumDate = json.AlbumDate;
                merged.LocalThumbnailPath = json.LocalThumbnailPath;
            }
            
            return merged;
        }
    }
}
