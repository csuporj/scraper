namespace scraper
{
    internal static class Settings
    {
        public const string RssUrl = "https://csuporj.blogspot.com/feeds/posts/default?alt=rss&max-results=500";
        public const string JsonFolder = "src";
        public const string JsonFileName = "albums.json";
        public static readonly string ThumbnailsFolder = $"public{Path.DirectorySeparatorChar}thumbnails";
        public const string ThumbnailQuality = "w1200-h800-p-k-no";
        public const int BatchSize = 3;
    }
}
