namespace scraper
{
    internal class ThumbnailJpgHandler
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
            string relativePath = GetRelativePath(fileName);
            byte[] bytes = await client.GetByteArrayAsync(thumbnailUrl);
            await File.WriteAllBytesAsync(relativePath, bytes);
            
            return fileName;
        }

        public static string GetRelativePath(string thumbnailFileName) =>
            Path.Combine(Settings.ThumbnailsFolder, thumbnailFileName);

        public static string[] GetThumbnailFilesFromDisk() =>
            Directory.GetFiles("thumbnails", "thumb_*.jpg");
    }
}
