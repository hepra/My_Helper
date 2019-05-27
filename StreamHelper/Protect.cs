using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Download.NetCore.Service.ZipStream
{
    public class Protect
    {
        public string FileName { get; }
        private ProtectInfomat ProtectInfomat { get; }
        private FileStruct ImageInformat { get; }
        private FileStruct VideoInformat { get; }
        public string Descript => ProtectInfomat.DescriptBytes.ToStr();
        public string UserId => ProtectInfomat.UserIdBytes.ToStr();
        public string TypeName => ProtectInfomat.TypeNameBytes.ToStr();
        public long MaterialId => BitConverter.ToInt64(ProtectInfomat.MaterialIdBytes, 0);
        public Protect(string filename)
        {
            FileName = filename;

            using (var stream = new FileStream(filename,FileMode.Open))
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
                var ImageInformatBytes = new byte[fileInformatSize];
                var videoInformatBytes = new byte[fileInformatSize];

                stream.Read(ImageInformatBytes, 0, fileInformatSize);
                ImageInformat = BytesToStuct<FileStruct>(ImageInformatBytes);

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


        public static async Task MergeAsync(string filename, string password, string imageFile, IEnumerable<string> pairList, FileInformat informat)
        {
            var protect = new ProtectInfomat()
            {
                DescriptBytes = informat.Descript.PadRight(100, '\0').ToBytes(),
                UserIdBytes = informat.UserId.PadRight(80, '\0').ToBytes(),
                TypeNameBytes = informat.TypeName.PadRight(10, '\0').ToBytes(),
                MaterialIdBytes = longToBytes(informat.MaterialId)
            };

            var headerBytes = await CompressAsync(StructToBytes(protect));
            var headerSizeBytes = BitConverter.GetBytes(headerBytes.Length);

            var fileInformatSize = Marshal.SizeOf(typeof(FileStruct));

            var imageInfo = new FileInfo(imageFile);

            var imageOffset = 4 + headerBytes.Length + fileInformatSize * 2;
            var videoOffset = imageOffset + imageInfo.Length;

            using (var stream = File.OpenWrite(filename))
            {
                await stream.WriteAsync(headerSizeBytes, 0, 4);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                var imageInformat = new FileStruct
                {
                    OffsetBytes = BitConverter.GetBytes((long)imageOffset),
                    LengthBytes = BitConverter.GetBytes((long)imageInfo.Length)
                };

                var imageInformatBytes = StructToBytes(imageInformat);

                await stream.WriteAsync(imageInformatBytes, 0, imageInformatBytes.Length);

                var videoInformat = new FileStruct
                {
                    OffsetBytes = BitConverter.GetBytes((long)videoOffset),
                    LengthBytes = BitConverter.GetBytes(GetPairMaxLength(pairList))
                };

                var videoInformatBytes = StructToBytes(videoInformat);

                await stream.WriteAsync(videoInformatBytes, 0, videoInformatBytes.Length);

                using (var imageStream = imageInfo.Open(FileMode.Open))
                {
                    await imageStream.CopyToAsync(stream);
                }

                //using (var videoStream = videoInfo.Open(FileMode.Open))
                //{
                //    await videoStream.CopyToAsync(stream);
                //}

                foreach (var pair in pairList)
                {
                    using (var pairStream = File.OpenRead(pair))
                    {
                        await pairStream.CopyToAsync(stream);
                    }
                }
            }
        }

        private static long GetPairMaxLength(IEnumerable<string> pairList)
        {
            var length = 0L;

            foreach(var pair in pairList)
            {
                var pairInfo = new FileInfo(pair);

                length += pairInfo.Length;
            }

            return length;
        }

#if DEBUG
        public static async Task<string> BuilderProtectAsync(
            string folder, string descript, string userId, string typename,
            long materialId)
        {
            var protect = new ProtectInfomat()
            {
                DescriptBytes = descript.PadRight(30, '\0').ToBytes(),
                UserIdBytes = userId.PadRight(80, '\0').ToBytes(),
                TypeNameBytes = typename.PadRight(10, '\0').ToBytes(),
                MaterialIdBytes = longToBytes(materialId)
            };

            var headerBytes = await CompressAsync(StructToBytes(protect));
            var headerSizeBytes = BitConverter.GetBytes(headerBytes.Length);

            var fileInformatSize = Marshal.SizeOf(typeof(FileInformat));

            var imageInfo = new FileInfo($"{folder}\\Image.png");
            var videoInfo = new FileInfo($"{folder}\\Video.dat");

            var imageOffset = 4 + headerBytes.Length + fileInformatSize * 2;
            var videoOffset = imageOffset + imageInfo.Length;

            var builderFilename = $"{folder}\\Protect";

            using (var stream = File.OpenWrite(builderFilename))
            {
                await stream.WriteAsync(headerSizeBytes, 0, 4);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                var imageInformat = new FileStruct
                {
                    OffsetBytes = BitConverter.GetBytes((long)imageOffset),
                    LengthBytes = BitConverter.GetBytes((long)imageInfo.Length)
                };

                var imageInformatBytes = StructToBytes(imageInformat);

                await stream.WriteAsync(imageInformatBytes, 0, imageInformatBytes.Length);

                var videoInformat = new FileStruct
                {
                    OffsetBytes = BitConverter.GetBytes((long)videoOffset),
                    LengthBytes = BitConverter.GetBytes((long)videoInfo.Length)
                };

                var videoInformatBytes = StructToBytes(videoInformat);

                await stream.WriteAsync(videoInformatBytes, 0, videoInformatBytes.Length);

                using (var imageStream = imageInfo.Open(FileMode.Open))
                {
                    await imageStream.CopyToAsync(stream);
                }

                using (var videoStream = videoInfo.Open(FileMode.Open))
                {
                    await videoStream.CopyToAsync(stream);
                }
            }

            return builderFilename;
        }

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
#endif
    }

}
