namespace scraper
{
    internal class Settings
    {
        public const string RssUrl = "https://csuporj.blogspot.com/feeds/posts/default?alt=rss&max-results=500";
        public const string JsonPath = "albums.json";
        public const string ThumbFolder = "thumbnails";
        public const int BatchSize = 5;
        public const int DelayBetweenRequestsMs = 200;
    }
}
