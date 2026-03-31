namespace scraper
{
    internal class AlbumInfo(string linkText, string albumUrl)
    {
        public const string NotScraped = "Not Scraped";

        public string LinkText { get; set; } = linkText;
        public string AlbumUrl { get; set; } = albumUrl;
        public string AlbumDate { get; set; } = NotScraped;
        public string ThumbnailUrl { get; set; } = "";
        public string LocalThumbnailPath { get; set; } = "";
    }
}