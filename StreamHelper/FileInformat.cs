using System.Runtime.InteropServices;

namespace Download.NetCore.Service.ZipStream
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct FileInformat
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] OffsetBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] LengthBytes;
    }

}
