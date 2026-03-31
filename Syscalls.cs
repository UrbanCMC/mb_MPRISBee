using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MusicBeePlugin;

namespace LinuxSys
{
    public static class Syscalls
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

    public class Socket
    {
        // Local communication domain
        public const int AF_UNIX = 1;

        // Byte stream type
        public const int SOCK_STREAM = 1;

        // File flags constants
        public const int O_RDONLY = 0;
        public const int O_WRONLY = 1;
        public const int O_RDWR = 2;
        public const int O_CREAT = 64;
        public const int O_TRUNC = 512;
        public const int O_APPEND = 1024;

        [StructLayout(LayoutKind.Sequential)]
        private struct SockAddrUn
        {
            public ushort sunFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 108)]
            public byte[] sunPath;
        }

        private SockAddrUn socketAddress;
        private IntPtr fileDescriptor;
        private string path;
        private readonly Logger logger;

        private readonly Queue<byte> readLeftoverBuffer;

        public Socket(Logger logger, string path)
        {
            this.logger = logger;

            InitializeWinsock();

            Console.WriteLine($"MPRISBee D: Socket constructor start");
            CreateUnixSocketAddress(path);
            Console.WriteLine($"MPRISBee D: Socket constructor passed CreateUnixSocketAddress");
            this.path = path;
            OpenUnixSocket();
            Console.WriteLine($"MPRISBee D: Socket constructor passed OpenUnixSocket");
            ConnectUnixSocket();
            Console.WriteLine($"MPRISBee D: Socket constructor passed ConnectUnixSocket");

            readLeftoverBuffer = new Queue<byte>();
            Console.WriteLine($"MPRISBee D: Socket constructor passed");
        }
        ~Socket()
        {
            CloseUnixSocket(fileDescriptor);
            ShutdownWinsock();
        }

        private void CreateUnixSocketAddress(string path)
        {
            socketAddress = new SockAddrUn
            {
                sunFamily = AF_UNIX,
                sunPath = new byte[108]
            };

            byte[] pathBytes = Encoding.UTF8.GetBytes(path);
            if (pathBytes.Length >= 108)
                throw new ArgumentException("Path too long for a Unix socket");

            Array.Copy(pathBytes, socketAddress.sunPath, pathBytes.Length);
            socketAddress.sunPath[pathBytes.Length] = 0;
        }

        private void OpenUnixSocket()
        {
            fileDescriptor = Syscalls.socket(AF_UNIX, SOCK_STREAM, 0);
            if (fileDescriptor == (IntPtr)(-1))
            {
                var error = Marshal.GetLastWin32Error();
                throw new SystemException($"Cannot open a new socket: {error}");
            }
        }

        private void ConnectUnixSocket()
        {
            var size = Marshal.SizeOf<SockAddrUn>();
            var addrPtr = Marshal.AllocHGlobal(size);
            try
            {
                var rawAddress = new byte[size];
                Marshal.StructureToPtr(socketAddress, addrPtr, false);
                Marshal.Copy(addrPtr, rawAddress, 0, size);

                Console.WriteLine($"MPRISBee D: ConnectUnixSocket fd: {fileDescriptor}, addPtr: {rawAddress}, size: {rawAddress.Length}");
                var res = Syscalls.connect(fileDescriptor, rawAddress, rawAddress.Length);
                if (res < 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    throw new IOException($"MPRISBee E: Cannot connect to a socket: {error}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(addrPtr);
            }
        }

        private void CloseUnixSocket(IntPtr fd)
        {
            if (Syscalls.closesocket(fd) < 0)
            {
                throw new SystemException("Cannot close this socket");
            }
        }

        public void WriteStringNLTerminated(string text)
        {
            if (!text.EndsWith("\n"))
            {
                text += "\n";
            }

            var bytes = Encoding.UTF8.GetBytes(text);
            var written = Syscalls.send(fileDescriptor, bytes, bytes.Length, 0);
            if (written < 0)
            {
                var error = Marshal.GetLastWin32Error();
                throw new IOException($"Write failed: {error}");
            }
        }

        public string ReadStringNLTerminated()
        {
            List<byte> result = new List<byte>();
            const int chunkSize = 256;

            var stopwatch = Stopwatch.StartNew();
            const int timeoutMillis = 500;

            bool foundNL = false;
            while (!foundNL)
            {
                while (readLeftoverBuffer.Count > 0)
                {
                    byte b = readLeftoverBuffer.Dequeue();
                    if (b == '\n')
                    {
                        return Encoding.UTF8.GetString(result.ToArray());
                    }
                    result.Add(b);
                }

                // Read from the socket
                byte[] chunk = new byte[chunkSize];
                int bytesRead = Syscalls.recv(fileDescriptor, chunk, chunkSize, 0);

                if (bytesRead < 0)
                {
                    throw new IOException($"Read failed. Bytes collected: {result.Count}");
                }

                if (bytesRead == 0)
                {
                    // Possibly temporary lack of data — wait and retry
                    if (stopwatch.ElapsedMilliseconds < timeoutMillis)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    throw new TimeoutException($"Socket read timed out after {timeoutMillis}ms waiting for null terminator.");
                }

                // Process newly read data
                for (int i = 0; i < bytesRead; i++)
                {
                    byte b = chunk[i];
                    if (b == '\n')
                    {
                        foundNL = true;
                        for (int j = i + 1; j < bytesRead; j++)
                        {
                            readLeftoverBuffer.Enqueue(chunk[j]);
                        }
                        break;
                    }
                    result.Add(b);
                }
            }

            return Encoding.UTF8.GetString(result.ToArray());
        }

        public static void InitializeWinsock()
        {
            // Request version 2.2
            short versionRequested = 0x0202;

            Syscalls.WSAData data = new Syscalls.WSAData();
            IntPtr dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Syscalls.WSAData)));

            try
            {
                Marshal.StructureToPtr(data, dataPtr, false);
                int result = Syscalls.WSAStartup(versionRequested, dataPtr);

                if (result != 0)
                {
                    throw new Exception($"Oh no! WSAStartup failed with error: {result}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }

        /// <summary>
        /// Gracefully shuts down Winsock.
        /// </summary>
        public static void ShutdownWinsock()
        {
            Syscalls.WSACleanup();
        }
    }
}
