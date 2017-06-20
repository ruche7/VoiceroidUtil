using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace RucheHome.Voiceroid
{
    /// <summary>
    /// VOICEROIDプロセスインタフェース。
    /// </summary>
    public interface IProcess : INotifyPropertyChanged
    {
        /// <summary>
        /// VOICEROID識別IDを取得する。
        /// </summary>
        VoiceroidId Id { get; }

        /// <summary>
        /// VOICEROID名を取得する。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// プロダクト名を取得する。
        /// </summary>
        string Product { get; }

        /// <summary>
        /// 表示プロダクト名を取得する。
        /// </summary>
        string DisplayProduct { get; }

        /// <summary>
        /// 空白文の音声保存を行えるか否かを取得する。
        /// </summary>
        bool CanSaveBlankText { get; }

        /// <summary>
        /// 実行ファイルのパスを取得する。
        /// </summary>
        /// <remarks>
        /// プロセスが見つかっていない場合は null を返す。
        /// </remarks>
        string ExecutablePath { get; }

        /// <summary>
        /// メインウィンドウハンドルを取得する。
        /// </summary>
        /// <remarks>
        /// メインウィンドウが見つかっていない場合は IntPtr.Zero を返す。
        /// </remarks>
        IntPtr MainWindowHandle { get; }

        /// <summary>
        /// プロセスが起動中であるか否かを取得する。
        /// </summary>
        bool IsStartup { get; }

        /// <summary>
        /// プロセスが実行中であるか否かを取得する。
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// トークテキストを再生中であるか否かを取得する。
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// トークテキストをWAVEファイル保存中であるか否かを取得する。
        /// </summary>
        bool IsSaving { get; }

        /// <summary>
        /// いずれかのダイアログが表示中であるか否かを取得する。
        /// </summary>
        bool IsDialogShowing { get; }

        /// <summary>
        /// トークテキストを取得する。
        /// </summary>
        /// <returns>トークテキスト。取得できなかったならば null 。</returns>
        Task<string> GetTalkText();

        /// <summary>
        /// トークテキストを設定する。
        /// </summary>
        /// <param name="text">トークテキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
        /// </remarks>
        Task<bool> SetTalkText(string text);

        /// <summary>
        /// トークテキストの再生を開始する。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// 再生中の場合は何もせず true を返す。
        /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
        /// </remarks>
        Task<bool> Play();

        /// <summary>
        /// トークテキストの再生を停止する。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// WAVEファイル保存中である場合は失敗する。
        /// </remarks>
        Task<bool> Stop();

        /// <summary>
        /// トークテキストをWAVEファイル保存する。
        /// </summary>
        /// <param name="filePath">保存希望WAVEファイルパス。</param>
        /// <returns>保存処理結果。</returns>
        /// <remarks>
        /// 再生中の場合は停止させる。
        /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
        /// 
        /// 既に同じ名前のWAVEファイルが存在する場合は上書きする。
        /// 
        /// VOICEROIDの設定次第ではテキストファイルも同時に保存される。
        /// </remarks>
        Task<FileSaveResult> Save(string filePath);

        /// <summary>
        /// 指定した実行ファイルをVOICEROIDプロセスとして実行する。
        /// </summary>
        /// <param name="executablePath">実行ファイルパス。</param>
        /// <returns>成功したならば null 。そうでなければ失敗理由メッセージ。</returns>
        Task<string> Run(string executablePath);

        /// <summary>
        /// VOICEROIDプロセスを終了させる。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        Task<bool> Exit();

        /// <summary>
        /// ボイスプリセット名を取得する。
        /// </summary>
        /// <returns>ボイスプリセット名。</returns>
        /// <remarks>
        /// VOICEROID2かつ実行中の場合はボイスプリセット名を取得して返す。
        /// それ以外では Name の値をそのまま返す。
        /// </remarks>
        Task<string> GetVoicePresetName();
    }
}
