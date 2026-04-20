namespace scraper
{
    internal static class ThumbnailJpgHandler
    {
        /// <summary>
        /// Downloads the thumbnail and returns the thumbnail file name.
        /// </summary>
        public static async Task<string> Download(string thumbnailUrl, HttpClient client)
        {
            if (string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                return "";
            }

            string fileName = $"thumb_{Guid.NewGuid():N}.jpg";
            string path = GetPath(fileName);
            byte[] bytes = await client.GetByteArrayAsync(thumbnailUrl);
            await File.WriteAllBytesAsync(path, bytes);

            return fileName;
        }

        public static string GetPath(string thumbnailFileName) =>
            Path.GetFullPath(Path.Combine(Settings.ThumbnailsFolder, thumbnailFileName));

        public static string[] GetThumbnailFilesFromDisk() =>
            Directory.GetFiles(Settings.ThumbnailsFolder, "thumb_*.jpg");
    }
}
