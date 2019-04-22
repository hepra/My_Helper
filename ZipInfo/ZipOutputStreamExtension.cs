using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace Wemew.Program.ZipInfo
{
    internal static class ZipOutputStreamExtension
    {
        public static void PutNextEntry(this ZipOutputStream archive,string filename,string comment)
        {
            var entity = new ZipEntry(Path.GetFileName(filename));

            entity.Comment = comment;
            entity.IsUnicodeText = true;
            archive.PutNextEntry(entity);

            var blockLength = 1024 * 4;
            using(var stream = File.OpenRead(filename))
            {
                while (stream.Position < stream.Length)
                {
                    var copyLength = stream.Length - stream.Position > blockLength ? blockLength : stream.Length - stream.Position;
                    var copyBytes = new byte[copyLength];

                    stream.Read(copyBytes, 0, copyBytes.Length);
                    archive.Write(copyBytes, 0, copyBytes.Length);
                }
            }
        }
    }
}
