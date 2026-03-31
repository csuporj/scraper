using Newtonsoft.Json;

namespace scraper
{
    internal static class JsonHandler
    {
        public static List<AlbumInfo> Read()
        {
            if (File.Exists(Settings.JsonFileName))
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<AlbumInfo>>(
                        File.ReadAllText(Settings.JsonFileName)) ?? [];
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message);
                }
            }

            Logger.Log("Existing JSON not found or invalid. Starting fresh.");
            return [];
        }

        public static void Write(IEnumerable<AlbumInfo> mergedAlbums)
        {
            File.WriteAllText(
                Settings.JsonFileName,
                JsonConvert.SerializeObject(mergedAlbums, Formatting.Indented));
        }
    }
}
