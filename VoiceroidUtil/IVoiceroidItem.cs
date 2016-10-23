using System;
using System.ComponentModel;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROID識別IDに紐付くアイテムを表すインタフェース。
    /// </summary>
    public interface IVoiceroidItem : INotifyPropertyChanged
    {
        /// <summary>
        /// VOICEROID識別IDを取得する。
        /// </summary>
        VoiceroidId VoiceroidId { get; }
    }
}
