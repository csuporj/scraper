namespace scraper
{
    internal partial class Program
    {
        private static async Task Main()
        {
            Directory.CreateDirectory(Settings.ThumbFolder);
            List<AlbumInfo> rssAlbums = await RssParser.GetAlbumsFromRss(Settings.RssUrl);
            List<AlbumInfo> jsonAlbums = JsonHandler.ReadJson(Settings.JsonPath);
            List<AlbumInfo> mergedAlbums = Merger.MergeRssWithJson(rssAlbums, jsonAlbums);

            await MissingAlbumsHandler.AddMissingAlbums(mergedAlbums);
            JsonHandler.WriteJson(mergedAlbums);
            Console.WriteLine($"Process finished. Total albums in list: {mergedAlbums.Count}");
        }
    }
}
