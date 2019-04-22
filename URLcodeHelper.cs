using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wemew.Program.Assets.utility
{
    public class URLcodeHelper
    {
        public static string URLDecode(string data)
        {
            return System.Web.HttpUtility.UrlDecode(data);
        }
    }
}
