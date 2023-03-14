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
        /// <param name="installPath">
        /// A.I.VOICE インストール先ディレクトリパス。
        /// null ならばレジストリー情報から取得する。
        /// </param>
        public AiVoiceApi(string installPath = null)
        {
            var assembly = LoadAssembly(ref installPath);
            this.InstallPath = installPath;

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
        /// インストール先ディレクトリパスを取得する。
        /// </summary>
        /// <remarks>
        /// コンストラクタで指定せず、レジストリーからも取得できなかった場合は null を返す。
        /// </remarks>
        public string InstallPath { get; }

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
            /// コンストラクタ。
            /// </summary>
            /// <param name="assembly">A.I.VOICE Editor API アセンブリ。</param>
            public AssemblyWrapper(Assembly assembly)
            {
                ValidateArgumentNull(assembly, nameof(assembly));

                this.TextEditModeType = assembly.GetType(Namespace + @".TextEditMode", true);
                this.TtsControl =
                    Activator.CreateInstance(
                        assembly.GetType(Namespace + '.' + nameof(this.TtsControl), true));
            }

            /// <summary>
            /// TextEditMode 列挙型情報を取得する。
            /// </summary>
            public Type TextEditModeType { get; }

            /// <summary>
            /// TtsControl オブジェクトを取得する。
            /// </summary>
            public dynamic TtsControl { get; }
        }

        /// <summary>
        /// A.I.VOICE Editor API 名前空間。
        /// </summary>
        private const string Namespace = @"AI.Talk.Editor.Api";

        /// <summary>
        /// A.I.VOICE Editor API アセンブリファイル名。
        /// </summary>
        private const string AssemblyFileName = Namespace + @".dll";

        /// <summary>
        /// A.I.VOICE Editor API アセンブリをロードする。
        /// </summary>
        /// <param name="installPath">
        /// A.I.VOICE インストール先ディレクトリパス。
        /// null ならばレジストリー情報から取得して上書きする。
        /// </param>
        /// <returns>
        /// A.I.VOICE Editor API アセンブリ。ロードできなければ null 。
        /// </returns>
        private static Assembly LoadAssembly(ref string installPath)
        {
            try
            {
                if (installPath == null)
                {
                    // レジストリーからインストール先ディレクトリパス取得
                    var key =
                        Registry.LocalMachine.OpenSubKey(
                            @"SOFTWARE\AI\AIVoice\AIVoiceEditor\1.0");
                    if (key == null)
                    {
                        return null;
                    }
                    installPath = key.GetValue("InstallDir") as string;
                    if (installPath == null)
                    {
                        return null;
                    }
                }

                // アセンブリファイル名と連結
                var path = Path.Combine(installPath, AssemblyFileName);
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
