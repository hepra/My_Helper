using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wemew.Program.Assets.utility
{
    public static class IamgeHelper
    {
       /// <summary>
       /// 获取IamgeSource
       /// </summary>
       /// <param name="AssemblyName">程序集名称</param>
       /// <param name="Path">相对路径 </param>
       /// <returns></returns>
        public static ImageSource GetImageSourceFromRelactivePath(string AssemblyName , string Path)
        {
            return new BitmapImage(new Uri($"pack://application:,,,/{AssemblyName};component/{Path}"));
        }
        /// <summary>
        /// 返回ImageSource
        /// </summary>
        /// <param name="Path">数据流</param>
        /// <returns></returns>
        public static ImageSource GetImageFromStream(MemoryStream ms)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            ms.Close();
            ms.Dispose();
            return bitmapImage;
        }
    }

}
