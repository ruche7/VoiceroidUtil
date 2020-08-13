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
        /// <returns>処理結果。</returns>
        public static Result Read(out GcmzInfo dest)
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

                // データ取得
                var data =
                    (GcmzInfo.Data)Marshal.PtrToStructure(
                        dataAddress,
                        typeof(GcmzInfo.Data));

                dest = new GcmzInfo(ref data);
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

        #endregion
    }
}
