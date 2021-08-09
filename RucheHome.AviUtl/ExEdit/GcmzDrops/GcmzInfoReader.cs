using System;
using System.Runtime.InteropServices;

namespace RucheHome.AviUtl.ExEdit.GcmzDrops
{
    /// <summary>
    /// 『ごちゃまぜドロップス』の共有メモリ情報読み取り処理を提供する静的クラス。
    /// </summary>
    public static class GcmzInfoReader
    {
        /// <summary>
        /// 処理結果列挙。
        /// </summary>
        public enum Result
        {
            /// <summary>
            /// 処理成功。
            /// </summary>
            Success = 0,

            /// <summary>
            /// ファイルマッピングオブジェクト取得に失敗。
            /// </summary>
            FileMappingFail,

            /// <summary>
            /// ファイルマッピングアドレス取得に失敗。
            /// </summary>
            MapViewFail,
        }

        /// <summary>
        /// 『ごちゃまぜドロップス』の共有メモリ情報を読み取る。
        /// </summary>
        /// <param name="dest">読み取った情報の設定先。</param>
        /// <param name="tryRead0312">
        /// 『ごちゃまぜドロップス』 v0.3.12 以降の共有メモリ情報読み取りを試みるならば true 。
        /// </param>
        /// <returns>処理結果。</returns>
        public static Result Read(out GcmzInfo dest, bool tryRead0312 = false)
        {
            dest = null;

            var handle = IntPtr.Zero;
            var dataAddress = IntPtr.Zero;
            try
            {
                // ファイルマッピングハンドル取得
                handle = OpenFileMapping(FILE_MAP_READ, false, FileMapName);
                if (handle == IntPtr.Zero)
                {
                    return Result.FileMappingFail;
                }

                // ファイルマッピングアドレス取得
                dataAddress = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                if (dataAddress == IntPtr.Zero)
                {
                    return Result.MapViewFail;
                }

                bool use0312 = false;
                if (tryRead0312)
                {
                    // ファイルマッピングサイズから GcmzInfo.Data0312 を利用できるか確認
                    var data0312Size = (uint)Marshal.SizeOf(typeof(GcmzInfo.Data0312));
                    var mapInfo = new MEMORY_BASIC_INFORMATION();
                    var mapInfoSize = new UIntPtr((uint)Marshal.SizeOf(mapInfo));
                    use0312 =
                        (VirtualQuery(dataAddress, ref mapInfo, mapInfoSize) == mapInfoSize) &&
                        (mapInfo.RegionSize.ToUInt64() >= data0312Size);
                }

                // データ取得
                if (use0312)
                {
                    var data =
                        (GcmzInfo.Data0312)Marshal.PtrToStructure(
                            dataAddress,
                            typeof(GcmzInfo.Data0312));
                    dest = new GcmzInfo(ref data);
                }
                else
                {
                    var data =
                        (GcmzInfo.Data)Marshal.PtrToStructure(
                            dataAddress,
                            typeof(GcmzInfo.Data));
                    dest = new GcmzInfo(ref data);
                }
            }
            finally
            {
                if (dataAddress != IntPtr.Zero)
                {
                    UnmapViewOfFile(dataAddress);
                }
                if (handle != IntPtr.Zero)
                {
                    CloseHandle(handle);
                }
            }

            return Result.Success;
        }

        /// <summary>
        /// 『ごちゃまぜドロップス』のファイルマッピング名。
        /// </summary>
        private const string FileMapName = @"GCMZDrops";

        #region Win32 API インポート

        private const uint FILE_MAP_READ = 0x0004;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenFileMapping(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr fileMapHandle,
            uint desiredAccess,
            uint fileOffsetHigh,
            uint fileOffsetLow,
            UIntPtr numberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr address);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public ushort PartitionId;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [DllImport("kernel32.dll")]
        private static extern UIntPtr VirtualQuery(
            IntPtr address,
            ref MEMORY_BASIC_INFORMATION info,
            UIntPtr infoSize);

        #endregion
    }
}
