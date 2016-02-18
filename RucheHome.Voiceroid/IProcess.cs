using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ruche.voiceroid
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
        /// メインウィンドウタイトルを取得する。
        /// </summary>
        string WindowTitle { get; }

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
        /// トークテキストを取得する。
        /// </summary>
        /// <returns>
        /// トークテキスト取得タスク。
        /// 成功するとトークテキストを返す。そうでなければ null を返す。
        /// </returns>
        Task<string> GetTalkText();

        /// <summary>
        /// トークテキストを設定する。
        /// </summary>
        /// <param name="text">トークテキスト。</param>
        /// <returns>
        /// トークテキスト設定タスク。
        /// 成功すると true を返す。そうでなければ false を返す。
        /// </returns>
        /// <remarks>
        /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
        /// </remarks>
        Task<bool> SetTalkText(string text);

        /// <summary>
        /// トークテキストの再生を開始する。
        /// </summary>
        /// <returns>
        /// 再生タスク。
        /// 成功すると true を返す。そうでなければ false を返す。
        /// </returns>
        /// <remarks>
        /// 再生中の場合は何もせず true を返す。
        /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
        /// </remarks>
        Task<bool> Play();

        /// <summary>
        /// トークテキストの再生を停止する。
        /// </summary>
        /// <returns>
        /// 停止タスク。
        /// 成功すると true を返す。そうでなければ false を返す。
        /// </returns>
        /// <remarks>
        /// WAVEファイル保存中である場合は失敗する。
        /// </remarks>
        Task<bool> Stop();

        /// <summary>
        /// トークテキストをWAVEファイル保存する。
        /// </summary>
        /// <param name="filePath">保存希望WAVEファイルパス。</param>
        /// <returns>
        /// WAVEファイル保存タスク。
        /// 成功すると実際のWAVEファイルパスを返す。そうでなければ null を返す。
        /// </returns>
        /// <remarks>
        /// 再生中の場合は停止させる。
        /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
        /// 
        /// 既に同じ名前のWAVEファイルが存在する場合は拡張子の手前に "[1]" 等の
        /// 角カッコ数値文字列が追加される。
        /// 
        /// VOICEROIDの設定次第ではテキストファイルも同時に保存される。
        /// </remarks>
        Task<string> Save(string filePath);
    }
}
