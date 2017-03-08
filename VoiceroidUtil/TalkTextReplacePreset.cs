using System;

namespace VoiceroidUtil
{
    /// <summary>
    /// トークテキスト置換アイテムプリセットクラス。
    /// </summary>
    public class TalkTextReplacePreset
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="description">説明文。</param>
        public TalkTextReplacePreset(string description)
        {
            this.Description =
                description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>
        /// 説明文を取得する。
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// トークテキスト置換アイテムコレクションを取得する。
        /// </summary>
        public TalkTextReplaceItemCollection Items { get; } =
            new TalkTextReplaceItemCollection();
    }
}
