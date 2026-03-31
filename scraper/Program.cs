namespace scraper
{
    internal partial class Program
    {
        private static async Task Main()
        {
            Directory.CreateDirectory(Settings.ThumbFolder);
            List<AlbumInfo> rss = await RssParser.GetAlbums(Settings.RssUrl);
            List<AlbumInfo> json = JsonHandler.Read(Settings.JsonPath);
            List<AlbumInfo> merged = Merger.Merge(rss, json);

            await MissingAlbumInfoHandler.FillMissingInfo(merged);
            JsonHandler.Write(merged);
            Logger.Log($"Process finished. Total albums in list: {merged.Count}");
        }
    }
}
