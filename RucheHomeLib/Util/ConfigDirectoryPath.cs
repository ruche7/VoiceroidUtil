using System;
using System.IO;
using System.Reflection;

namespace RucheHome.Util
{
    /// <summary>
    /// アプリケーション設定を保存するディレクトリのパスを提供するクラス。
    /// </summary>
    public class ConfigDirectoryPath
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
        public ConfigDirectoryPath(string subDirectory = null, string baseDirectory = null)
        {
            this.SubDirectory = subDirectory;
            this.BaseDirectory = baseDirectory;
        }

        /// <summary>
        /// アプリケーション設定を保存するディレクトリのパスを取得する。
        /// </summary>
        public string Value =>
            string.IsNullOrEmpty(this.SubDirectory) ?
                this.BaseDirectory :
                Path.Combine(this.BaseDirectory, this.SubDirectory);

        /// <summary>
        /// サブディレクトリパスを取得または設定する。
        /// </summary>
        public string SubDirectory
        {
            get => this.subDirectory;
            set
            {
                var v = value ?? "";
                this.subDirectory =
                    !Path.IsPathRooted(v) ?
                        v :
                        throw new ArgumentException(
                            $@"`{nameof(value)}` is absolute path.",
                            nameof(value));
            }
        }
        private string subDirectory = null;

        /// <summary>
        /// ベースディレクトリパスを取得または設定する。
        /// </summary>
        public string BaseDirectory
        {
            get => this.baseDirectory;
            set
            {
                var dir = value ?? GetCompanyName();
                if (string.IsNullOrWhiteSpace(dir))
                {
                    throw new ArgumentException(
                        $@"`{nameof(value)}` is blank.",
                        nameof(value));
                }

                if (value == null || !Path.IsPathRooted(dir))
                {
                    dir =
                        Path.Combine(
                            Environment.GetFolderPath(
                                Environment.SpecialFolder.LocalApplicationData),
                            dir);
                }

                this.baseDirectory = dir;
            }
        }
        private string baseDirectory = null;

        /// <summary>
        /// このインスタンスの文字列表現値を取得する。
        /// </summary>
        /// <returns>Value の値を返す。</returns>
        public override string ToString() => this.Value;

        /// <summary>
        /// プロセスの AssemblyCompanyAttribute 属性値を取得する。
        /// </summary>
        /// <returns>プロセスの AssemblyCompanyAttribute 属性値。</returns>
        private static string GetCompanyName()
        {
            if (companyName != null)
            {
                return companyName;
            }

            companyName =
                Assembly
                    .GetEntryAssembly()?
                    .GetCustomAttribute<AssemblyCompanyAttribute>()?
                    .Company;
            if (companyName == null)
            {
                throw new InvalidOperationException(
                    nameof(AssemblyCompanyAttribute) + @" is not defined.");
            }
            else if (string.IsNullOrWhiteSpace(companyName))
            {
                companyName = null;
                throw new InvalidOperationException(
                    nameof(AssemblyCompanyAttribute) + @" is blank.");
            }

            return companyName;
        }
        private static string companyName = null;
    }
}
