using System;
using System.Text;

namespace Download.NetCore.Service.ZipStream
{
    public static class ConvertExtension
    {
        public static byte[] ToBytes(this string str, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            return encoding.GetBytes(str);
        }

        public static string ToStr(this byte[] bytes, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            return encoding.GetString(bytes).Replace('\0', ' ').TrimEnd();
        }

        public static long Offset(this FileStruct fileInformat)
        {
            return BitConverter.ToInt32(fileInformat.OffsetBytes, 0);
        }

        public static long Length(this FileStruct fileInformat)
        {
            return BitConverter.ToInt32(fileInformat.LengthBytes, 0);
        }

        public static long MaterialId(this ProtectInfomat infomat)
        {
            return BitConverter.ToInt64(infomat.MaterialIdBytes, 0);
        }

        public static string TypeName(this ProtectInfomat infomat)
        {
            return infomat.TypeNameBytes.ToStr();
        }

        public static string UserId(this ProtectInfomat infomat)
        {
            return infomat.UserIdBytes.ToStr();
        }

        public static string Descript(this ProtectInfomat infomat)
        {
            return infomat.DescriptBytes.ToStr();
        }
    }

}
