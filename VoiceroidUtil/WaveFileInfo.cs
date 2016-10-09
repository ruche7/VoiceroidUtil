using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VoiceroidUtil
{
    /// <summary>
    /// WAVEファイルフォーマット情報クラス。
    /// </summary>
    public class WaveFileInfo
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        public WaveFileInfo(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using (var stream = File.OpenRead(filePath))
            {
                this.Read(stream);
            }
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="stream">ストリーム。</param>
        public WaveFileInfo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.Read(stream);
        }

        /// <summary>
        /// データフォーマットIDを取得する。
        /// </summary>
        public ushort Format { get; private set; }

        /// <summary>
        /// チャンネル数を取得する。
        /// </summary>
        public ushort Channels { get; private set; }

        /// <summary>
        /// 1秒あたりのサンプル数を取得する。
        /// </summary>
        public uint SamplesPerSecond { get; private set; }

        /// <summary>
        /// 1秒あたりのバイト数を取得する。
        /// </summary>
        public uint BytesPerSecond { get; private set; }

        /// <summary>
        /// ブロックサイズを取得する。
        /// </summary>
        public ushort BlockAlign { get; private set; }

        /// <summary>
        /// 1サンプルあたりのビット数を取得する。
        /// </summary>
        public ushort BitsPerSample { get; private set; }

        /// <summary>
        /// 波形データサイズを取得する。
        /// </summary>
        public uint DataSize { get; private set; }

        /// <summary>
        /// 合計時間を取得する。
        /// </summary>
        public TimeSpan TotalTime { get; private set; }

        /// <summary>
        /// 'RIFF' チャンクID値。
        /// </summary>
        private const uint RiffChunkId = 0x46464952;

        /// <summary>
        /// 'WAVE' タイプID値。
        /// </summary>
        private const uint WaveTypeId = 0x45564157;

        /// <summary>
        /// 'fmt ' チャンクID値。
        /// </summary>
        private const uint FmtChunkId = 0x20746D66;

        /// <summary>
        /// 'data' チャンクID値。
        /// </summary>
        private const uint DataChunkId = 0x61746164;

        /// <summary>
        /// WAVEフォーマットデータを読み取る。
        /// </summary>
        /// <param name="stream">ストリーム。</param>
        private void Read(Stream stream)
        {
            Debug.Assert(stream != null);

            using (var reader = new BinaryReader(stream, Encoding.ASCII))
            {
                this.Read(reader);
            }
        }

        /// <summary>
        /// WAVEフォーマットデータを読み取る。
        /// </summary>
        /// <param name="reader">バイナリリーダ。</param>
        private void Read(BinaryReader reader)
        {
            Debug.Assert(reader != null);

            if (reader.ReadUInt32() != RiffChunkId)
            {
                throw new FormatException(@"'RIFF' chunk ID is not found.");
            }
            var riffSize = 8L + reader.ReadInt32();
            if (reader.ReadUInt32() != WaveTypeId)
            {
                throw new FormatException(@"'WAVE' type ID is not found.");
            }

            // チャンク読み取り
            this.ReadChunks(reader, riffSize);

            // 合計時間算出
            if (this.BytesPerSecond == 0)
            {
                throw new FormatException(@"The BytesPerSecond parameter is 0.");
            }
            this.TotalTime =
                TimeSpan.FromSeconds((double)this.DataSize / this.BytesPerSecond);
        }

        /// <summary>
        /// WAVEフォーマットチャンク群を読み取る。
        /// </summary>
        /// <param name="reader">バイナリリーダ。</param>
        /// <param name="riffSize">チャンクヘッダ含むRIFFデータサイズ。</param>
        private void ReadChunks(BinaryReader reader, long riffSize)
        {
            Debug.Assert(reader != null);

            bool fmtFound = false, dataFound = false;
            while ((!fmtFound || !dataFound) && reader.BaseStream.Position < riffSize)
            {
                var chunk = reader.ReadUInt32();
                var size = reader.ReadUInt32();

                if (chunk == FmtChunkId)
                {
                    if (size < 16)
                    {
                        throw new FormatException(@"'fmt ' chunk size is less than 16.");
                    }

                    this.Format = reader.ReadUInt16();
                    this.Channels = reader.ReadUInt16();
                    this.SamplesPerSecond = reader.ReadUInt32();
                    this.BytesPerSecond = reader.ReadUInt32();
                    this.BlockAlign = reader.ReadUInt16();
                    this.BitsPerSample = reader.ReadUInt16();

                    fmtFound = true;
                    reader.BaseStream.Seek(size - 16, SeekOrigin.Current);
                }
                else
                {
                    if (chunk == DataChunkId)
                    {
                        this.DataSize = size;
                        dataFound = true;
                    }
                    reader.BaseStream.Seek(size, SeekOrigin.Current);
                }
            }

            if (!fmtFound)
            {
                throw new FormatException(@"'fmt ' chunk ID is not found.");
            }
            if (!dataFound)
            {
                throw new FormatException(@"'data' chunk ID is not found.");
            }
        }
    }
}
