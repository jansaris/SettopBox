using System.IO;

namespace WebHelper
{
    public sealed class FileHelper
    {
        public static void DeleteFolderContent(string folder, bool recursive, bool includeThisFolder = false)
        {
            foreach (string file in Directory.GetFiles(folder))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            if (recursive)
            {
                foreach (var subfolder in Directory.GetDirectories(folder))
                    DeleteFolderContent(subfolder, true, true);
            }

            if (includeThisFolder)
                Directory.Delete(folder, false);
        }
    }
}
