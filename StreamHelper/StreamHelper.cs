using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace DaHeDemo.Utility
{
    public class StreamHelper
    {
        //public BuildInfo buildInfo { get; set; }
        //public StreamHelper(BuildInfo _buildInfo)
        //{
        //    buildInfo = _buildInfo;
        //}
        public static async void EncryptionVideo(string OutFileName, string PassWord, string VideoFileName)
        {
            await Protect.MergeAsync(OutFileName, PassWord, VideoFileName);
        }
        public static Stream GetStream(string FileName, string password)
        {
            try
            {
                Protect protect = new Protect(FileName);
                if (protect.Password == ("@#$%#$^E$@$" + password))
                {
                    return protect.VideoStream();
                }
                else
                {

                    MessageBox.Show("密码错误!");
                }
                return Stream.Null;
            }
            catch(Exception ex)
            {
               
                MessageBox.Show(ex.Message);
                return Stream.Null;
            }
            
        }

        public static async void DecryptVideo(string FileName, string password)
        {
            try
            {
                Protect protect = new Protect(FileName);
                if (protect.Password == ("@#$%#$^E$@$" + password))
                {
                    var temp = protect.VideoStream();
                    string extension = System.IO.Path.GetExtension(FileName);
                    FileName = FileName.Replace(extension, "解密" + extension);
                    FileName = FileName.Replace("加密", "");
                    using (var stream = File.OpenWrite(FileName))
                    {
                        await temp.CopyToAsync(stream);
                    }
                    temp.Close();
                }
                else
                {
                    MessageBox.Show("密码错误!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

    }
    public class BuildInfo
    {
        public string ImageFileName { get; set; }
        public string VideoFileName { get; set; }
        public string TargetFileName { get; set; }
        public string SourceFileName { get; set; }
        public BuildInfo(string TargetName = "", string SourceName = "", string SourceImageFileName = "", string sourceVideoFileName = "")
        {
            ImageFileName = SourceImageFileName;
            VideoFileName = sourceVideoFileName;
            TargetFileName = TargetName;
            SourceFileName = SourceName;
        }
    }

    #region 解析和Build 自定义流
    public class Protect
    {
        public string FileName { get; }
        private ProtectInfomat ProtectInfomat { get; }
        private FileStruct ImageInformat { get; }
        private FileStruct VideoInformat { get; }
        public string Password => ProtectInfomat.PassWord.ToStr();
        //public string UserId => ProtectInfomat.UserIdBytes.ToStr();
        //public string TypeName => ProtectInfomat.TypeNameBytes.ToStr();
        //public long MaterialId => BitConverter.ToInt64(ProtectInfomat.MaterialIdBytes, 0);
        public Protect(string filename)
        {
            FileName = filename;

            using (var stream = new FileStream(filename, FileMode.Open))
            {
                #region Get Header Size
                var headerSizeBytes = new byte[4];

                stream.Read(headerSizeBytes, 0, 4);

                var headerSize = BitConverter.ToInt32(headerSizeBytes, 0);
                #endregion

                #region Get Header
                var compressBytes = new byte[headerSize];

                stream.Read(compressBytes, 0, headerSize);

                var depressBytes = Depress(compressBytes);

                ProtectInfomat = BytesToStuct<ProtectInfomat>(depressBytes);
                #endregion

                var fileInformatSize = Marshal.SizeOf(typeof(FileStruct));
                //var ImageInformatBytes = new byte[fileInformatSize];
                var videoInformatBytes = new byte[fileInformatSize];

                //stream.Read(ImageInformatBytes, 0, fileInformatSize);
                //ImageInformat = BytesToStuct<FileStruct>(ImageInformatBytes);

                stream.Read(videoInformatBytes, 0, fileInformatSize);
                VideoInformat = BytesToStuct<FileStruct>(videoInformatBytes);
            }
        }

        public MemoryStream ImageStream()
        {
            using (var stream = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var memoryStream = new MemoryStream();

                var offset = ImageInformat.Offset();
                var length = ImageInformat.Length();

                stream.Position = offset;

                var blockSize = 1024 * 4;
                var blockBytes = new byte[blockSize];
                var len = 0;

                var readLength = blockSize;

                while ((len = stream.Read(blockBytes, 0, readLength)) > 0)
                {
                    memoryStream.Write(blockBytes, 0, len);
                    readLength = stream.Position + blockSize < offset + length ? blockSize : (int)(offset + length - stream.Position);
                }
                return memoryStream;
            }
            //return new ProtectStream(FileName, ImageInformat);
        }

        public Stream VideoStream()
        {
            return new ProtectStream(FileName, VideoInformat);
        }

        private static byte[] Depress(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            using (var outStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    compressionStream.CopyTo(outStream);
                    compressionStream.Flush();
                }
                return outStream.ToArray();
            }
        }

        private static T BytesToStuct<T>(byte[] bytes)
        {
            //得到结构体的大小
            var type = typeof(T);
            int size = Marshal.SizeOf(type);
            //byte数组长度小于结构体的大小
            if (size > bytes.Length)
            {
                //返回空
                return default(T);
            }
            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将byte数组拷到分配好的内存空间
            Marshal.Copy(bytes, 0, structPtr, size);
            //将内存空间转换为目标结构体
            object obj = Marshal.PtrToStructure(structPtr, type);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            //返回结构体
            return (T)obj;
        }

        public static ProtectInfomat GetInformat(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                #region Get Header Size
                var headerSizeBytes = new byte[4];

                stream.Read(headerSizeBytes, 0, 4);

                var headerSize = BitConverter.ToInt32(headerSizeBytes, 0);
                #endregion

                #region Get Header
                var compressBytes = new byte[headerSize];

                stream.Read(compressBytes, 0, headerSize);

                var depressBytes = Depress(compressBytes);

                var ProtectInfomat = BytesToStuct<ProtectInfomat>(depressBytes);
                #endregion

                return ProtectInfomat;
            }
        }

        #region 创建资源
        /// <summary>
        ///  创建资源
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="imageFileName"></param>
        /// <param name="videoFileName"></param>
        /// <returns></returns>
        public static async Task MergeAsync(string OutputFileame, string Password, string videoFileName)
        {
            //构建头部
            string extension = System.IO.Path.GetExtension(OutputFileame);
            OutputFileame = OutputFileame.Replace(extension, "加密" + extension);

            string Header = "@#$%#$^E$@$" + Password;
            var protect = new ProtectInfomat()
            {
                PassWord = Header.PadRight(1024, '\0').ToBytes(),
            };
            //压缩头部
            var headerBytes = await CompressAsync(StructToBytes(protect));
            var headerSizeBytes = BitConverter.GetBytes(headerBytes.Length);

            //构建信息
            var fileInformatSize = Marshal.SizeOf(typeof(FileStruct));
            var videoInfo = new FileInfo(videoFileName);
            //更新位置信息
            var Offset = 4 + headerBytes.Length + fileInformatSize;

            using (var stream = File.OpenWrite(OutputFileame))
            {
                //将头部写入流
                await stream.WriteAsync(headerSizeBytes, 0, 4);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                //获取流开始位置及大小
                var PosInformat = new FileStruct
                {
                    OffsetBytes = BitConverter.GetBytes((long)Offset),
                    LengthBytes = BitConverter.GetBytes(videoInfo.Length)
                };
                //分配空间
                var imageInformatBytes = StructToBytes(PosInformat);
                await stream.WriteAsync(imageInformatBytes, 0, imageInformatBytes.Length);

                using (var videoStream = videoInfo.Open(FileMode.Open))
                {
                    await videoStream.CopyToAsync(stream);
                }
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="descript"></param>
        /// <param name="userId"></param>
        /// <param name="typename"></param>
        /// <param name="materialId"></param>
        /// <returns></returns>
        //public static async Task<string> BuilderProtectAsync(
        //    string folder, string descript, string userId, string typename,
        //    long materialId)
        //{
        //    var protect = new ProtectInfomat()
        //    {
        //        DescriptBytes = descript.PadRight(30, '\0').ToBytes(),
        //        UserIdBytes = userId.PadRight(80, '\0').ToBytes(),
        //        TypeNameBytes = typename.PadRight(10, '\0').ToBytes(),
        //        MaterialIdBytes = longToBytes(materialId)
        //    };

        //    var headerBytes = await CompressAsync(StructToBytes(protect));
        //    var headerSizeBytes = BitConverter.GetBytes(headerBytes.Length);

        //    var fileInformatSize = Marshal.SizeOf(typeof(FileInformat));

        //    var imageInfo = new FileInfo($"{folder}\\Image.png");
        //    var videoInfo = new FileInfo($"{folder}\\Video.dat");

        //    var imageOffset = 4 + headerBytes.Length + fileInformatSize * 2;
        //    var videoOffset = imageOffset + imageInfo.Length;

        //    var builderFilename = $"{folder}\\Protect";

        //    using (var stream = File.OpenWrite(builderFilename))
        //    {
        //        await stream.WriteAsync(headerSizeBytes, 0, 4);
        //        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

        //        var imageInformat = new FileStruct
        //        {
        //            OffsetBytes = BitConverter.GetBytes((long)imageOffset),
        //            LengthBytes = BitConverter.GetBytes((long)imageInfo.Length)
        //        };

        //        var imageInformatBytes = StructToBytes(imageInformat);

        //        await stream.WriteAsync(imageInformatBytes, 0, imageInformatBytes.Length);

        //        var videoInformat = new FileStruct
        //        {
        //            OffsetBytes = BitConverter.GetBytes((long)videoOffset),
        //            LengthBytes = BitConverter.GetBytes((long)videoInfo.Length)
        //        };

        //        var videoInformatBytes = StructToBytes(videoInformat);

        //        await stream.WriteAsync(videoInformatBytes, 0, videoInformatBytes.Length);

        //        using (var imageStream = imageInfo.Open(FileMode.Open))
        //        {
        //            await imageStream.CopyToAsync(stream);
        //        }

        //        using (var videoStream = videoInfo.Open(FileMode.Open))
        //        {
        //            await videoStream.CopyToAsync(stream);
        //        }
        //    }

        //    return builderFilename;
        //}

        /// <summary>
        /// 分配空间
        /// </summary>
        /// <param name="structObj"></param>
        /// <returns></returns>
        private static byte[] StructToBytes(object structObj)
        {
            try
            {
                //得到结构体的大小
                int size = Marshal.SizeOf(structObj.GetType());
                //创建byte数组
                byte[] bytes = new byte[size];
                //分配结构体大小的内存空间
                IntPtr structPtr = Marshal.AllocHGlobal(size);
                //将结构体拷到分配好的内存空间
                Marshal.StructureToPtr(structObj, structPtr, false);
                //从内存空间拷到byte数组
                Marshal.Copy(structPtr, bytes, 0, size);
                //释放内存空间
                Marshal.FreeHGlobal(structPtr);
                //返回byte数组
                return bytes;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Long 转 字节
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static byte[] longToBytes(long number)
        {
            byte[] buf = new byte[8];

            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(number & 0x00000000000000ff);
                number >>= 8;
            }

            return buf;
        }

        /// <summary>
        /// 压缩流
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static async Task<byte[]> CompressAsync(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    await compressionStream.WriteAsync(data, 0, data.Length);
                    await compressionStream.FlushAsync();
                }
                //必须先关了compressionStream后才能取得正确的压缩流
                return memoryStream.ToArray();
            }
        }

    }
    #endregion

    #region 自定义流
    internal class ProtectStream : Stream
    {

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => throw new NotImplementedException();
        public override long Length => FileInformat.Length();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        private FileStruct FileInformat { get; }
        private Stream Stream { get; }
        public ProtectStream(string filename, FileStruct fileInformat)
        {
            Stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            FileInformat = fileInformat;
            Stream.Position = (fileInformat.Offset());
        }

        public override void Flush()
        {
        }

        public override void Close()
        {
            Stream.Close();
            base.Close();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!Stream.CanRead)
            {
                return 0;
            }
            if (FileInformat.Offset() + FileInformat.Length() == Stream.Position)
            {
                return 0;
            }

            if (FileInformat.Offset() + FileInformat.Length() < Stream.Position + count)
            {
                count = (int)(FileInformat.Offset() + FileInformat.Length() - Stream.Position);
            }

            return Stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!Stream.CanRead)
            {
                return 0;
            }
            var result = Stream.Seek(FileInformat.Offset() + offset, origin);

            return result - FileInformat.Offset();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region 自定义流Header格式
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 1024, Pack = 1)]
    public struct ProtectInfomat
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        public byte[] PassWord;

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        //public byte[] UserIdBytes;

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        //public byte[] TypeNameBytes;

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        //public byte[] MaterialIdBytes;
    }

    #endregion

    #region 自定义文件结构

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct FileStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] OffsetBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] LengthBytes;
    }

    #endregion

    #region 自定义拓展方法
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

        //public static long MaterialId(this ProtectInfomat infomat)
        //{
        //    return BitConverter.ToInt64(infomat.MaterialIdBytes, 0);
        //}
        public static string PassWord(this ProtectInfomat infomat)
        {
            return infomat.PassWord.ToStr();
        }
        //public static string TypeName(this ProtectInfomat infomat)
        //{
        //    return infomat.TypeNameBytes.ToStr();
        //}

        //public static string UserId(this ProtectInfomat infomat)
        //{
        //    return infomat.UserIdBytes.ToStr();
        //}

        //public static string Descript(this ProtectInfomat infomat)
        //{
        //    return infomat.DescriptBytes.ToStr();
        //}
    }
    #endregion

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct FileInformat
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] OffsetBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] LengthBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public byte[] DescriptBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] UserIdBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] TypeNameBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] MaterialIdBytes;
    }

}
