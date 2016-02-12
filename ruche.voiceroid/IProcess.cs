using System;
using System.ComponentModel;

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
        /// VOICEROID名称を取得する。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// プロセスが実行中であるか否かを取得する。
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// トークテキストをWAVEファイル保存中であるか否かを取得する。
        /// </summary>
        bool IsSaving { get; }

        /// <summary>
        /// トークテキストを取得する。
        /// </summary>
        /// <returns>トークテキスト。取得できない場合は null 。</returns>
        string GetTalkText();

        /// <summary>
        /// トークテキストを設定する。
        /// </summary>
        /// <param name="text">トークテキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
        /// </remarks>
        bool SetTalkText(string text);

        /// <summary>
        /// トークテキストの再生を開始する。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// 再生中の場合は停止させる。
        /// WAVEファイル保存中である場合やトークテキストが空である場合は失敗する。
        /// </remarks>
        bool Play();

        /// <summary>
        /// トークテキストの再生を停止する。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// WAVEファイル保存中である場合は失敗する。
        /// </remarks>
        bool Stop();

        /// <summary>
        /// トークテキストをWAVEファイル保存する。
        /// </summary>
        /// <param name="filePath">保存希望WAVEファイルパス。</param>
        /// <returns>実際のWAVEファイルパス。失敗した場合は null 。</returns>
        /// <remarks>
        /// 再生中の場合は停止させる。
        /// WAVEファイル保存中である場合やトークテキストが空である場合は失敗する。
        /// 
        /// 既に同じ名前のWAVEファイルが存在する場合は拡張子の手前に "[1]" 等の
        /// 角カッコ数値文字列が追加される。
        /// </remarks>
        string Save(string filePath);
    }
}
