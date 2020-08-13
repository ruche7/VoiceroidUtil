using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;

namespace RucheHome.Util
{
    /// <summary>
    /// 設定の読み書きを行うクラス。
    /// </summary>
    /// <typeparam name="T">設定値の型。</typeparam>
    public class ConfigKeeper<T>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="subDirectory">
        /// ベースディレクトリを基準位置とするサブディレクトリパス。
        /// 絶対パスであったり、空白文字のみであってはならない。
        /// 空文字列または null を指定すると、ベースディレクトリパスをそのまま用いる。
        /// </param>
        /// <param name="baseDirectory">
        /// ベースディレクトリパス。
        /// 空文字列や空白文字のみであってはならない。
        /// 相対パスを指定すると、ローカルアプリケーションフォルダを基準位置とする。
        /// null を指定すると、プロセスの AssemblyCompanyAttribute 属性を利用する。
        /// </param>
        /// <param name="serializer">
        /// シリアライザ。既定のシリアライザを用いるならば null 。
        /// </param>
        public ConfigKeeper(
            string subDirectory = null,
            string baseDirectory = null,
            XmlObjectSerializer serializer = null)
        {
            this.Value = default;

            var dir = new ConfigDirectoryPath(subDirectory, baseDirectory);
            var fileName = typeof(T).FullName + @".config";
            this.FilePath = Path.Combine(dir.Value, fileName);

            this.Serializer = serializer ?? (new DataContractJsonSerializer(typeof(T)));
        }

        /// <summary>
        /// 設定値を取得または設定する。
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// 設定ファイルパスを取得する。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// シリアライザを取得する。
        /// </summary>
        public XmlObjectSerializer Serializer { get; }

        /// <summary>
        /// 設定を読み取る。
        /// </summary>
        /// <returns>成功したならば true 。失敗したならば false 。</returns>
        public bool Load()
        {
            // ファイルがなければ読み取れない
            if (!File.Exists(this.FilePath))
            {
                return false;
            }

            if (Interlocked.Exchange(ref this.ioLock, 1) != 0)
            {
                return false;
            }

            try
            {
                // 読み取り
                using (var stream = File.OpenRead(this.FilePath))
                {
                    var value = this.Serializer.ReadObject(stream);
                    if (!(value is T))
                    {
                        return false;
                    }
                    this.Value = (T)value;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref this.ioLock, 0);
            }

            return true;
        }

        /// <summary>
        /// 設定を書き出す。
        /// </summary>
        /// <returns>成功したならば true 。失敗したならば false 。</returns>
        public bool Save()
        {
            if (Interlocked.Exchange(ref this.ioLock, 1) != 0)
            {
                return false;
            }

            try
            {
                // 親ディレクトリ作成
                var dirPath = Path.GetDirectoryName(Path.GetFullPath(this.FilePath));
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                // 書き出し
                using (var stream = File.Create(this.FilePath))
                {
                    this.Serializer.WriteObject(stream, this.Value);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref this.ioLock, 0);
            }

            return true;
        }

        /// <summary>
        /// I/O処理排他ロック用。
        /// </summary>
        private int ioLock = 0;
    }
}
