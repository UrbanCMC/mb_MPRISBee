using System;
using System.Runtime.InteropServices;

namespace MPrisBee
{
    public static class WinsockInterop
    {
        [StructLayout(LayoutKind.Sequential)]
        public class WSAData
        {
            public short wVersion;
            public short wHighVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            public string szDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
            public string szSystemStatus;
            public short iMaxSockets;
            public short iMaxUdpDg;
            public IntPtr lpVendorInfo;
        }

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int WSAStartup(short wVersionRequested, IntPtr lpWSAData);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int WSACleanup();

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern IntPtr socket(int af, int type, int protocol);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int connect(IntPtr s, byte[] name, int namelen);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int send(IntPtr s, byte[] buf, int len, int flags);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int recv(IntPtr s, byte[] buf, int len, int flags);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int closesocket(IntPtr s);
    }
}
