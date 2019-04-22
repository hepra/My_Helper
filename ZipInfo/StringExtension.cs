using System.Text;

namespace Wemew.Program.ZipInfo
{
    internal static class StringExtension
    {
        public static string Md5(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var hash = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(bytes);
            var result = string.Empty;

            for (int i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("x").PadLeft(2, '0');
            }

            return result;
        }
    }
}
