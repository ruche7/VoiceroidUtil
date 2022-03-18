using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using RucheHome.Util;

using static RucheHome.Util.ArgumentValidater;

namespace RucheHome.Voiceroid
{
    /// <summary>
    /// HostStatus ラッパー列挙。
    /// </summary>
    internal enum AiVoiceHostStatus
    {
        NotAvailable = -1,

        NotRunning = 0,
        NotConnected = 1,
        Idle = 2,
        Busy = 3,
    }

    /// <summary>
    /// TextEditMode ラッパー列挙。
    /// </summary>
    internal enum AiVoiceTextEditMode
    {
        NotAvailable = -1,

        Text = 0,
        List = 1,
    }

    /// <summary>
    /// A.I.VOICE Editor API の動的ラッパークラス。
    /// </summary>
    /// <remarks>
    /// A.I.VOICE version 1.3.0 以降がインストールされていない環境でも動作するために、
    /// アセンブリおよび各 API を動的ロードする必要がある。
    /// </remarks>
    internal sealed class AiVoiceApi
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AiVoiceApi()
        {
            var assembly = LoadAssembly();
            if (assembly != null)
            {
                try
                {
                    this.Api = new AssemblyWrapper(assembly);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    this.Api = null;
                }
            }
        }

        /// <summary>
        /// 利用可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// このプロパティが false を返す場合、他のプロパティやメソッドは必ず
        /// null や false といった失敗値を返す。
        /// また、プロパティへの設定やメソッドの呼び出しでは何も行われない。
        /// </remarks>
        public bool IsAvailable => this.Api != null;

        /// <summary>
        /// API が初期化されているか否かを取得する。
        /// </summary>
        public bool IsInitialized => this.Api?.TtsControl.IsInitialized ?? false;

        /// <summary>
        /// ホストの状態を取得する。
        /// </summary>
        /// <remarks>
        /// ラッパーが利用不可能な場合は常に
        /// <see cref="AiVoiceHostStatus.NotAvailable"/> を返す。
        /// </remarks>
        public AiVoiceHostStatus Status =>
            this.IsAvailable ?
                (AiVoiceHostStatus)Convert.ToInt32(this.Api.TtsControl.Status) :
                AiVoiceHostStatus.NotAvailable;

        /// <summary>
        /// ボイスプリセット名を取得または設定する。
        /// </summary>
        public string CurrentVoicePresetName
        {
            get => this.Api?.TtsControl.CurrentVoicePresetName;
            set
            {
                if (this.IsAvailable)
                {
                    this.Api.TtsControl.CurrentVoicePresetName = value;
                }
            }
        }

        /// <summary>
        /// ボイスプリセット名配列を取得する。
        /// </summary>
        /// <remarks>
        /// 参照する度に新しい配列を作成して返すと思われるため、
        /// 必要に応じて参照元でキャッシュすること。
        /// </remarks>
        public string[] VoicePresetNames => this.Api?.TtsControl.VoicePresetNames;

        /// <summary>
        /// テキスト入力形式を取得または設定する。
        /// </summary>
        /// <remarks>
        /// ラッパーが利用不可能な場合は常に
        /// <see cref="AiVoiceTextEditMode.NotAvailable"/> を返す。
        /// </remarks>
        public AiVoiceTextEditMode TextEditMode
        {
            get =>
                this.IsAvailable ?
                    (AiVoiceTextEditMode)Convert.ToInt32(this.Api.TtsControl.TextEditMode) :
                    AiVoiceTextEditMode.NotAvailable;
            set
            {
                if (this.IsAvailable)
                {
                    this.Api.TtsControl.TextEditMode =
                        (dynamic)Enum.ToObject(this.Api.TextEditModeType, (int)value);
                }
            }
        }

        /// <summary>
        /// テキスト形式の入力テキストを取得または設定する。
        /// </summary>
        public string Text
        {
            get => this.Api?.TtsControl.Text;
            set
            {
                if (this.IsAvailable)
                {
                    this.Api.TtsControl.Text = value;
                }
            }
        }

        /// <summary>
        /// テキスト形式の入力テキストの選択開始位置を取得または設定する。
        /// </summary>
        /// <remarks>
        /// ラッパーが利用不可能な場合は常に -1 を返す。
        /// </remarks>
        public int TextSelectionStart
        {
            get => this.Api?.TtsControl.TextSelectionStart ?? -1;
            set
            {
                if (this.IsAvailable)
                {
                    this.Api.TtsControl.TextSelectionStart = value;
                }
            }
        }

        /// <summary>
        /// テキスト形式の入力テキストの選択文字数を取得または設定する。
        /// </summary>
        /// <remarks>
        /// ラッパーが利用不可能な場合は常に -1 を返す。
        /// </remarks>
        public int TextSelectionLength
        {
            get => this.Api?.TtsControl.TextSelectionLength ?? -1;
            set
            {
                if (this.IsAvailable)
                {
                    this.Api.TtsControl.TextSelectionLength = value;
                }
            }
        }

        /// <summary>
        /// 利用可能なホスト名の配列を取得する。
        /// </summary>
        /// <returns>利用可能なホスト名の配列。</returns>
        public string[] GetAvailableHostNames() => this.Api?.TtsControl.GetAvailableHostNames();

        /// <summary>
        /// API を初期化する。
        /// </summary>
        /// <param name="serviceName">接続先ホスト名。</param>
        public void Initialize(string hostName) => this.Api?.TtsControl.Initialize(hostName);

        /// <summary>
        /// ホストと接続する。
        /// </summary>
        public void Connect() => this.Api?.TtsControl.Connect();

        /// <summary>
        /// ホストとの接続を解除する。
        /// </summary>
        public void Disconnect() => this.Api?.TtsControl.Disconnect();

        /// <summary>
        /// 音声の再生を開始または一時停止する。
        /// </summary>
        public void Play() => this.Api?.TtsControl.Play();

        /// <summary>
        /// 音声の再生を停止する。
        /// </summary>
        public void Stop() => this.Api?.TtsControl.Stop();

        /// <summary>
        /// 音声をファイルに保存する。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <remarks>
        /// 本体のファイル命名規則が有効である場合、引数
        /// <paramref name="filePath"/> は無視される。
        /// </remarks>
        public void SaveAudioToFile(string filePath) =>
            this.Api?.TtsControl.SaveAudioToFile(filePath);

        /// <summary>
        /// A.I.VOICE Editor API アセンブリラッパークラス。
        /// </summary>
        private sealed class AssemblyWrapper
        {
            /// <summary>
            /// HostStatus 列挙定義クラス。
            /// </summary>
            public sealed class HostStatusEnum
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="type">HostStatus 型情報。</param>
                public HostStatusEnum(Type type)
                {
                    ValidateArgumentNull(type, nameof(type));

                    var values = type.GetEnumValues();

                    this.NotRunning = values.GetValue(0);
                    this.NotConnected = values.GetValue(1);
                    this.Idle = values.GetValue(2);
                    this.Busy = values.GetValue(3);
                }

                /// <summary>
                /// NotRunning 列挙値を取得する。
                /// </summary>
                public dynamic NotRunning { get; }

                /// <summary>
                /// NotConnected 列挙値を取得する。
                /// </summary>
                public dynamic NotConnected { get; }

                /// <summary>
                /// Idle 列挙値を取得する。
                /// </summary>
                public dynamic Idle { get; }

                /// <summary>
                /// Busy 列挙値を取得する。
                /// </summary>
                public dynamic Busy { get; }
            }

            /// <summary>
            /// TextEditMode 列挙定義クラス。
            /// </summary>
            public sealed class TextEditModeEnum
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="type">TextEditMode 型情報。</param>
                public TextEditModeEnum(Type type)
                {
                    ValidateArgumentNull(type, nameof(type));

                    var values = type.GetEnumValues();

                    this.Text = values.GetValue(0);
                    this.List = values.GetValue(1);
                }

                /// <summary>
                /// Text 列挙値を取得する。
                /// </summary>
                public dynamic Text { get; }

                /// <summary>
                /// List 列挙値を取得する。
                /// </summary>
                public dynamic List { get; }
            }

            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="assembly">A.I.VOICE Editor API アセンブリ。</param>
            public AssemblyWrapper(Assembly assembly)
            {
                ValidateArgumentNull(assembly, nameof(assembly));

                const string ns = @"AI.Talk.Editor.Api.";

                this.HostStatusType = assembly.GetType(ns + nameof(this.HostStatus), true);
                this.HostStatus = new HostStatusEnum(this.HostStatusType);

                this.TextEditModeType = assembly.GetType(ns + nameof(this.TextEditMode), true);
                this.TextEditMode = new TextEditModeEnum(this.TextEditModeType);

                this.TtsControl =
                    Activator.CreateInstance(
                        assembly.GetType(ns + nameof(this.TtsControl), true));
            }

            /// <summary>
            /// HostStatus 列挙型情報を取得する。
            /// </summary>
            public Type HostStatusType { get; }

            /// <summary>
            /// HostStatus 列挙定義オブジェクトを取得する。
            /// </summary>
            public HostStatusEnum HostStatus { get; }

            /// <summary>
            /// TextEditMode 列挙型情報を取得する。
            /// </summary>
            public Type TextEditModeType { get; }

            /// <summary>
            /// TextEditMode 列挙定義オブジェクトを取得する。
            /// </summary>
            public TextEditModeEnum TextEditMode { get; }

            /// <summary>
            /// TtsControl オブジェクトを取得する。
            /// </summary>
            public dynamic TtsControl { get; }
        }

        /// <summary>
        /// A.I.VOICE Editor API アセンブリファイル名。
        /// </summary>
        private const string AssemblyFileName = @"AI.Talk.Editor.Api.dll";

        /// <summary>
        /// A.I.VOICE Editor API アセンブリをロードする。
        /// </summary>
        /// <returns>
        /// A.I.VOICE Editor API アセンブリ。ロードできなければ null 。
        /// </returns>
        private static Assembly LoadAssembly()
        {
            try
            {
                // レジストリーからインストールディレクトリパス取得
                var key =
                    Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\AI\AIVoice\AIVoiceEditor\1.0");
                if (key == null)
                {
                    return null;
                }
                var path = key.GetValue("InstallDir") as string;
                if (path == null)
                {
                    return null;
                }

                // アセンブリファイル名と連結
                path = Path.Combine(path, AssemblyFileName);
                if (!File.Exists(path))
                {
                    return null;
                }

                return Assembly.LoadFrom(path);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// アセンブリラッパーを取得する。
        /// </summary>
        private AssemblyWrapper Api { get; } = null;
    }
}
