using System.Runtime.InteropServices;

namespace Download.NetCore.Service.ZipStream
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 198, Pack = 1)]
    public struct ProtectInfomat
    {
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
