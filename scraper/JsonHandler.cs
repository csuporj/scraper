using Newtonsoft.Json;

namespace scraper
{
    internal static class JsonHandler
    {
        public static List<AlbumInfo> Read(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<AlbumInfo>>(
                        File.ReadAllText(jsonPath)) ?? [];
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
                Settings.JsonPath,
                JsonConvert.SerializeObject(mergedAlbums, Formatting.Indented));
        }
    }
}
