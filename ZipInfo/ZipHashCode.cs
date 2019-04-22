using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wemew.Program.ZipInfo
{
    public static class ZipHashCode
    {
#if DEBUG
        public static string HashKey => "123456789";
#else
        public static string HashKey => $"Wemem_{Program.LoginResProtocol.Id}_{Program.LoginResProtocol.LoginId}".Md5();
#endif
    }
}
