using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpgGrabber.IO
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
                foreach (string subfolder in Directory.GetDirectories(folder))
                    DeleteFolderContent(subfolder, recursive, true);
            }

            if (includeThisFolder)
                Directory.Delete(folder, false);
        }
    }
}
