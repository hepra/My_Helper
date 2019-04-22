using SharpCompress.Archives;
using System;
using System.IO;

namespace Wemew.Program.ZipInfo
{
    internal static class RarExtension
    {
        public static void Save(this IArchiveEntry archive,string path)
        {
            if (!File.Exists(path))
            {
                archive.WriteToFile(path);
            }
            else
            {
                var imageInfo = new FileInfo(path);

                if (imageInfo.Length != archive.Size)
                {
                    File.Delete(path);

                    archive.WriteToFile(path);
                }
            }
        }

        public static string GetDescript(this IArchiveEntry archive)
        {
            using (var stream = new StreamReader(archive.OpenEntryStream()))
            {
                return stream.ReadToEnd();
            }
        }
    }
}
