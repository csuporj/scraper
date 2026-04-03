namespace scraper
{
    internal partial class Program
    {
        private static async Task Main()
        {
            Directory.CreateDirectory(Settings.ThumbnailsFolder);
            List<AlbumInfo> json = JsonHandler.Read();
            await Cleaner.Cleanup(json);
            List<AlbumInfo> rss = await RssParser.GetAlbums();
            List<AlbumInfo> merged = Merger.Merge(rss, json);
            await MissingAlbumInfoHandler.FillInfo(merged);
            JsonHandler.Write(merged);
            
            Logger.Log("Process finished. Total albums in list: " + merged.Count);
        }
    }
}
