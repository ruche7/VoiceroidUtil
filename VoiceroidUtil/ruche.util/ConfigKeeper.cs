using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ruche.util
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
        /// <param name="directoryName">
        /// 設定保存先ディレクトリ名。基準位置からの相対パス。
        /// 全体で設定を共有するならば空文字列または null 。
        /// </param>
        public ConfigKeeper(string directoryName)
        {
            this.Value = default(T);
            this.FilePath =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    string.IsNullOrEmpty(directoryName) ?
                        @"ruche-home" :
                        @"ruche-home\" + directoryName,
                    typeof(T).FullName + ".config");
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

            try
            {
                // 読み取り
                using (var stream = File.OpenRead(this.FilePath))
                {
                    var serializer = MakeSerializer();
                    var value = serializer.ReadObject(stream);
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

            return true;
        }

        /// <summary>
        /// 設定を書き出す。
        /// </summary>
        /// <returns>成功したならば true 。失敗したならば false 。</returns>
        public bool Save()
        {
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
                    var serializer = MakeSerializer();
                    serializer.WriteObject(stream, this.Value);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// シリアライザを生成する。
        /// </summary>
        /// <returns>シリアライザ。</returns>
        private static XmlObjectSerializer MakeSerializer()
        {
            return new DataContractJsonSerializer(typeof(T));
        }
    }
}
