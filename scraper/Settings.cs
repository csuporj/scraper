namespace scraper
{
    internal class Settings
    {
        public const string RssUrl = "https://csuporj.blogspot.com/feeds/posts/default?alt=rss&max-results=500";
        public const string JsonFileName = "albums.json";
        public const string ThumbnailsFolder = "thumbnails";
        public const string ThumbnailQuality = "w1200-h800-p-k-no";
        public const int BatchSize = 50;
    }
}
