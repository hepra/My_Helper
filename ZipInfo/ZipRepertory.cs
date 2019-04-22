using Download.NetCore.Service;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Linq;
using System.Text;
using static Wemew.Program.Views.MainWindow;

namespace Wemew.Program.ZipInfo
{
    public class ZipRepertory
    {
        public ZipDescEntity GetDescript(string path, string password)
        {
            var result = new ZipDescEntity();

            using (var stream = File.OpenRead(path))
            {
                using (var archive = ZipArchive.Open(stream,
                    new ReaderOptions {
                        Password = password,
                        LeaveStreamOpen = true,
                        ArchiveEncoding = new ArchiveEncoding()
                        {
                            Default = Encoding.UTF8
                        }
                    }
                ))
                {
                    //var video = archive.Entries.Count(entity => IsVideo(Path.GetExtension(entity.Key)));

                    //if (video != 1)
                    //    throw new Exception($"Zip File:{path} Struct Error");

                    var imageArchive = archive.Entries.FirstOrDefault(entity => IsImage(Path.GetExtension(entity.Key)));

                    if (imageArchive == null)
                        throw new Exception($"Rar {path} not found image file");

                    var fileDirectory = $"{Path.GetFileName(path).Replace(Path.GetExtension(Path.GetFileName(path)), "")}";
                    var root = $"{Program.LoginResProtocol.Id}";
                    var fullDirectory = $"{root}\\{fileDirectory}";
                    var imageFile = $"{fullDirectory}\\{imageArchive.Key}";
                    var comment = imageArchive.Comment;

                    if (!Directory.Exists(fullDirectory))
                    {
                        var directoryInfo = Directory.CreateDirectory(fullDirectory);
                        directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.NotContentIndexed | FileAttributes.ReadOnly;
                    }

                    imageArchive.Save(imageFile);

                    result.ImagePath = Path.GetFullPath(imageFile);
                    result.Descript = comment;
                    result.Source = path;

                    var fileInformatArchive = archive.Entries.FirstOrDefault(entity => entity.Key.Equals("FileInformat.json"));
                    using (var streamReader = new StreamReader(fileInformatArchive.OpenEntryStream()))
                    {
                        var lineToEnd = streamReader.ReadToEnd();
                        var fileInformat = JsonConvert.DeserializeObject<FileInformat>(lineToEnd);
                        result.jsonFromat = fileInformat;
                        result.TypeName = fileInformat.TypeName;
                    }
                }
            }

            return result;
        }

        public int GetVideoCount(string path, string password)
        {
            using (var stream = File.OpenRead(path))
            {
                using (var archive = ZipArchive.Open(stream,
                    new ReaderOptions
                    {
                        Password = password,
                        LeaveStreamOpen = true,
                        ArchiveEncoding = new ArchiveEncoding()
                        {
                            Default = Encoding.UTF8
                        }
                    }
                ))
                {
                    return  (int)archive.Entries.FirstOrDefault(entity => IsVideo(Path.GetExtension(entity.Key))).Size/20*1024*1024;
                }
            }
        }
        //public Stream GetVideoStream(string path, string password)
        //{
        //    //using (
        //    var stream = File.OpenRead(path);
        //    //)
        //    //{
        //    //using (
        //    var archive = ZipArchive.Open(stream,
        //    new ReaderOptions
        //    {
        //        Password = password,
        //        LeaveStreamOpen = true,
        //        ArchiveEncoding = new ArchiveEncoding()
        //        {
        //            Default = Encoding.UTF8
        //        }
        //    }
        //);
        //    //)
        //        //{
        //        var videoArchive = archive.Entries.FirstOrDefault(entity => IsVideo(Path.GetExtension(entity.Key)));
        //          //  var blockLength = 1024 * 40;
        //           // var memoryStream = new MemoryStream(blockLength * 2);
        //            var videoStream = videoArchive.OpenEntryStream();

        //            //while (videoStream.Position < videoArchive.Size)
        //            //{
        //            //    var copyLength = videoArchive.Size - videoStream.Position > blockLength ? blockLength : videoArchive.Size - videoStream.Position;
        //            //    var copyBytes = new byte[copyLength];

        //            //    videoStream.Read(copyBytes, 0, copyBytes.Length);
        //            //    memoryStream.Write(copyBytes, 0, copyBytes.Length);
        //            //}

        //            return videoStream;
        //        //}
        //    //}
        //}

        public Stream GetVideoStream(string path, string password,int index)
        {
            using (var stream = File.OpenRead(path))
            {
                using (var archive = ZipArchive.Open(stream,
                    new ReaderOptions
                    {
                        Password = password,
                        LeaveStreamOpen = true,
                        ArchiveEncoding = new ArchiveEncoding()
                        {
                            Default = Encoding.UTF8
                        }
                    }
                ))
                {
                    foreach(var e in archive.Entries)
                    {
                        if(e.Key.ToLower().Contains(".dat"))
                        {
                            var videoArchive = e;
                            var videoStream = videoArchive.OpenEntryStream();
                            var copyLength = videoArchive.Size;
                            var copyBytes = new byte[copyLength];
                            var memoryStream = new MemoryStream(copyBytes.Length);
                            videoStream.Read(copyBytes, 0, copyBytes.Length);
                            memoryStream.Write(copyBytes, 0, copyBytes.Length);
                            return videoStream;
                        }
                    }
                }
            }
            return null;
        }
        public VlcStream GetVideoStream(string path, string password)
        {
            var stream = File.OpenRead(path);
            var option = new ReaderOptions
            {
                Password = password,
                LeaveStreamOpen = true,
                ArchiveEncoding = new ArchiveEncoding()
                {
                    Default = Encoding.UTF8
                }
            };
            var archive = ZipArchive.Open(stream, option );
            VlcStream temp = new VlcStream();
            temp.Stream = archive;
            temp.ZipPath = path;
            temp.Option = option;
            temp.check = entity => IsVideo(Path.GetExtension(entity.Key));
            temp.fileStream = stream;
            return temp;
        }
        public VlcStream GetVideoStream(string path)
        {
            var stream = File.OpenRead(path);
            var option = new ReaderOptions
            {
                LeaveStreamOpen = true,
                ArchiveEncoding = new ArchiveEncoding()
                {
                    Default = Encoding.UTF8
                }
            };
            var archive = ZipArchive.Open(stream, option);
            VlcStream temp = new VlcStream();
            temp.Stream = archive;
            temp.ZipPath = path;
            temp.Option = option;
            temp.check = entity => IsImage(Path.GetExtension(entity.Key));
            temp.fileStream = stream;
            return temp;
        }
        public void CreateZip(string imageFile,string videoFile,string savePath,string password, string comment)
        {
            var stream = File.Create(savePath);
            using (var archive = new ZipOutputStream(stream))
            {
                archive.Password = password;
                archive.SetComment(comment);
                archive.SetLevel(6);

                archive.PutNextEntry(imageFile, comment);
                archive.PutNextEntry(videoFile, comment);
            }
        }

        public bool CheckZip(string path, string password)
        {
            var checkEntity = new ZipEntity();

            using (var stream = File.OpenRead(path))
            {
                using (var archive = ZipArchive.Open(stream,
                    new ReaderOptions
                    {
                        Password = password,
                        LeaveStreamOpen = true,
                        LookForHeader = true,
                        ArchiveEncoding = new ArchiveEncoding()
                        {
                            Default = Encoding.UTF8
                        }
                    }
                ))
                {
                    var imageArchive = archive.Entries.FirstOrDefault(entity => IsImage(Path.GetExtension(entity.Key)));

                    if (imageArchive == null)
                        throw new Exception($"Rar {path} not found image file");

                    var root = $"{Program.LoginResProtocol.Id}";
                    var imageFile = $"{root}\\{imageArchive.Key}";
                    var comment = imageArchive.Comment;

                    if (!Directory.Exists(root))
                    {
                        Directory.CreateDirectory(root);
                    }

                    //imageArchive.Save(imageFile);

                    checkEntity.ImagePath = imageFile;
                    checkEntity.Descript = comment;

                    var videoArchive = archive.Entries.FirstOrDefault(entity => IsVideo(Path.GetExtension(entity.Key)));

                    checkEntity.FindVideo = videoArchive != null;
                }
            }

            return checkEntity.FindVideo && !string.IsNullOrWhiteSpace(checkEntity.Descript) && !string.IsNullOrWhiteSpace(checkEntity.ImagePath);
        }

        private bool IsImage(string key)
        {
            var images = new[]
            {
                ".png",
                ".jpg",
                ".jpeg",
                ".bmp"
            };

            return images.Contains(key.ToLower());
        }

        private bool IsJson(string key)
        {
            var json = new[]
            {
                ".Json"
            };

            return json.Contains(key.ToLower());
        }

        private bool IsVideo(string key)
        {
            return ".dat".Equals(key.ToLower());
        }
    }
}
