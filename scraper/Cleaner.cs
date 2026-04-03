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
            HashSet<string> jsonThumbnailFileNames = albums.Select(a => a.ThumbnailFileName.Trim()).ToHashSet();
            string[] relativePathsOnDisk = ThumbnailJpgHandler.GetThumbnailFilesFromDisk();
            foreach (var relativePath in relativePathsOnDisk)
            {
                var fileName = Path.GetFileName(relativePath).Trim();
                if (!jsonThumbnailFileNames.Contains(fileName))
                {
                    File.Delete(relativePath);
                    Logger.Log($"Deleted: {fileName}");
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
