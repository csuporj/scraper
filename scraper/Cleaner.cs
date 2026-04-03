namespace scraper
{
    internal static class Cleaner
    {
        public static async Task Cleanup(List<AlbumInfo> albums)
        {
            CleanupOrphanedJpgs(albums);
            CleanupJson(albums);
        }

        public static void CleanupOrphanedJpgs(List<AlbumInfo> albums)
        {
            Logger.Log("Cleaning thumbnails...");
            
            HashSet<string> jsonFileNames = [.. albums.Select(a => a.ThumbnailFileName.Trim())];
            string[] diskRelativePaths = ThumbnailJpgHandler.GetThumbnailFilesFromDisk();
            
            foreach (var diskPath in diskRelativePaths)
            {
                var diskFileName = Path.GetFileName(diskPath).Trim();
                if (!jsonFileNames.Contains(diskFileName))
                {
                    File.Delete(diskPath);
                    Logger.Log($"Deleted: {diskFileName}");
                }
            }
        }

        public static void CleanupJson(List<AlbumInfo> albums)
        {
            Logger.Log($"Cleaning json...");
            foreach (var album in albums)
            {
                if (!string.IsNullOrWhiteSpace(album.ThumbnailFileName))
                {
                    var path = ThumbnailJpgHandler.GetRelativePath(album.ThumbnailFileName);
                    if (!File.Exists(path))
                    {
                        string oldName = album.ThumbnailFileName;
                        album.ThumbnailFileName = "";
                        Logger.Log($"Cleaned: {album.LinkText} -> {album.AlbumDate} -> {oldName}");
                    }
                }
            }
        }
    }
}
