using System;
using System.IO;

namespace Download.NetCore.Service.ZipStream
{
    internal class ProtectStream : Stream
    {

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => FileInformat.Length();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private FileStruct FileInformat { get; }

        private Stream Stream { get; }

        public ProtectStream(string filename,FileStruct fileInformat)
        {
            Stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            FileInformat = fileInformat;
            Stream.Position = fileInformat.Offset();
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
            if (!Stream.CanRead )
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

}
