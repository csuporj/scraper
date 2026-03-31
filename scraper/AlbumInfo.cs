using Newtonsoft.Json;

namespace scraper
{
    internal class AlbumInfo(string linkText, string albumUrl)
    {
        public string LinkText { get; set; } = linkText;

        public string AlbumUrl { get; set; } = albumUrl;

        public string AlbumDate { get; set; } = "";

        [JsonIgnore]
        public string ThumbnailUrl { get; set; } = "";

        public string ThumbnailFileName { get; set; } = "";
    }
}
