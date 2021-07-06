using System.IO;
using System.Collections.Generic;

namespace Didimo.Utils.ZipUtils //Didimo.Editor.Utils
{
    /// <summary>
    /// Derived from Andrew Novik's work on https://www.codeproject.com/Tips/319438/How-to-Compress-Decompress-directories
    /// </summary>
    public class ZipUtil
    {
        public delegate void ProgressDelegate(string file, float progress);

        public static void DecompressToDirectory(byte[] zipBytes, string sDir, ProgressDelegate progress)
        {
            using (Stream stream = new MemoryStream(zipBytes))
            {
                // Open an existing zip file for reading
                ZipStorer zip = ZipStorer.Open(stream, FileAccess.Read);

                // Read the central directory collection
                List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

                // Look for the desired file
                for (int i = 0; i < dir.Count; i++)
                {
                    ZipStorer.ZipFileEntry entry = dir[i];

                    progress(entry.FilenameInZip, (float)i / dir.Count);
                    zip.ExtractFile(entry, Path.Combine(sDir, entry.FilenameInZip));
                }

                zip.Close();
            }
        }
    }
}
