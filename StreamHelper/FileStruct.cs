using System.Runtime.InteropServices;

namespace Download.NetCore.Service.ZipStream
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct FileStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] OffsetBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] LengthBytes;
    }

}
