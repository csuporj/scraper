using Newtonsoft.Json;

namespace scraper
{
    internal static class JsonHandler
    {
        public static List<AlbumInfo> ReadJson(string jsonPath)
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
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Existing JSON not found or invalid. Starting fresh.");
                }
            }

            return [];
        }

        public static async Task WriteJson(List<AlbumInfo> mergedAlbums)
        {
            await File.WriteAllTextAsync(
                Settings.JsonPath,
                JsonConvert.SerializeObject(mergedAlbums, Formatting.Indented));
        }
    }
}
