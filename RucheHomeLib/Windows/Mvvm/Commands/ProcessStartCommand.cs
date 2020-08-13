using System;
using System.Diagnostics;
using System.Windows.Input;

namespace RucheHome.Windows.Mvvm.Commands
{
    /// <summary>
    /// コマンドパラメータ、またはプロパティ値を
    /// Process.Start メソッドに渡すコマンドを定義するクラス。
    /// </summary>
    /// <remarks>
    /// 有効なコマンドパラメータが設定されていればそれをファイル名として用いる。
    /// そうでなければプロパティ値を用いる。
    /// </remarks>
    public class ProcessStartCommand : ICommand
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="fileName">ファイル名。</param>
        /// <param name="arguments">コマンドライン引数文字列。</param>
        public ProcessStartCommand(string fileName = null, string arguments = null)
        {
            this.FileName = fileName;
            this.Arguments = arguments;
        }

        /// <summary>
        /// ファイル名を取得または設定する。
        /// </summary>
        /// <remarks>
        /// null ならばプロパティ値を利用しない。
        /// </remarks>
        public string FileName { get; set; }

        /// <summary>
        /// コマンドライン引数文字列を取得または設定する。
        /// </summary>
        /// <remarks>
        /// null ならばコマンドライン引数なしで実行する。
        /// コマンドパラメータを用いる場合は無視される。
        /// </remarks>
        public string Arguments { get; set; }

        #region ICommand の実装

        /// <summary>
        /// コマンドを実行可能であるか否かを取得する。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <returns>常に true 。</returns>
        public bool CanExecute(object parameter) => true;

        /// <summary>
        /// コマンド処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        public void Execute(object parameter)
        {
            var fileName = parameter as string;
            string arguments = null;

            // コマンドパラメータが無効ならプロパティ値を用いる
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = this.FileName;
                arguments = this.Arguments;

                if (fileName == null)
                {
                    return;
                }
            }

            try
            {
                if (arguments == null)
                {
                    Process.Start(fileName);
                }
                else
                {
                    Process.Start(fileName, arguments);
                }
            }
            catch { }
        }

        #endregion

        #region ICommand の明示的実装

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        #endregion
    }
}
