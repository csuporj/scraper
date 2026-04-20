using Newtonsoft.Json;

namespace scraper
{
    internal static class JsonHandler
    {
        public static string GetPath() =>
            Path.GetFullPath(Path.Combine(Settings.JsonFolder, Settings.JsonFileName));

        public static List<AlbumInfo> Read()
        {
            var path = GetPath();
            if (File.Exists(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<AlbumInfo>>(
                        File.ReadAllText(path)) ?? [];
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
                GetPath(),
                JsonConvert.SerializeObject(mergedAlbums, Formatting.Indented));
        }
    }
}
