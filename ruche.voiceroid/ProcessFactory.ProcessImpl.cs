using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Practices.Prism.Mvvm;

namespace ruche.voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// IProcess インタフェース実装クラス。
        /// </summary>
        private class ProcessImpl : BindableBase, IProcess
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            /// <param name="name">VOICEROID名称。</param>
            public ProcessImpl(VoiceroidId id, string name)
            {
                this.Id = id;
                this.Name = name;
            }

            /// <summary>
            /// プロセス列挙を基に状態を更新する。
            /// </summary>
            /// <param name="voiceroidApps">プロセス列挙。</param>
            public void Update(IEnumerable<Process> voiceroidApps)
            {
                // 対象プロセス検索
                var app =
                    voiceroidApps?.FirstOrDefault(
                        p => p.MainWindowTitle.StartsWith(this.Name));
                if (app == null)
                {
                    this.SetupDeadState();
                    return;
                }

                // 現在と同じウィンドウが取得できた場合はスキップ
                if (
                    !this.IsRunning ||
                    this.MainWindow.Handle != app.MainWindowHandle)
                {
                    // コントロール群更新
                    this.MainWindow = new Win32Window(app.MainWindowHandle);
                    if (!this.UpdateControls())
                    {
                        this.SetupDeadState();
                        return;
                    }
                }

                this.IsRunning = true;

                // 保存ダイアログ状態確認
                this.IsSaving = (this.FindSaveDialog() != null);
            }

            /// <summary>
            /// 保存ダイアログタイトル文字列。
            /// </summary>
            private const string SaveDialogTitle = @"音声ファイルの保存";

            /// <summary>
            /// WAVEファイル名末尾の角カッコ数値文字列にマッチする正規表現。
            /// </summary>
            private static readonly Regex RegexWaveFileDigit = new Regex(@"\[\d+\]$");

            /// <summary>
            /// WAVEファイルパスを作成する。
            /// </summary>
            /// <param name="filePath">基となるファイルパス。</param>
            /// <returns>WAVEファイルパス。</returns>
            private static string MakeWaveFilePath(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException("filePath");
                }

                // 拡張子抜きのフルパスを作成
                var basePath = Path.GetFullPath(filePath);
                if (Path.GetExtension(basePath).ToLower() == ".wav")
                {
                    basePath = Path.ChangeExtension(basePath, null);
                }

                // 終端の "[数値]" を削除
                basePath = RegexWaveFileDigit.Replace(basePath, "");

                // 重複しないパスを作成
                // テキストファイルが同時出力されるのでそちらもチェック
                var path = basePath;
                for (
                    int i = 1;
                    File.Exists(path + ".wav") || File.Exists(path + ".txt");
                    ++i)
                {
                    path = basePath + "[" + i + "]";
                }
                path += ".wav";

                return path;
            }

            /// <summary>
            /// メインウィンドウを取得または設定する。
            /// </summary>
            private Win32Window MainWindow { get; set; } = null;

            /// <summary>
            /// トークテキストエディットコントロールを取得または設定する。
            /// </summary>
            private Win32Window TalkEdit { get; set; } = null;

            /// <summary>
            /// 再生ボタンコントロールを取得または設定する。
            /// </summary>
            private Win32Window PlayButton { get; set; } = null;

            /// <summary>
            /// 停止ボタンコントロールを取得または設定する。
            /// </summary>
            private Win32Window StopButton { get; set; } = null;

            /// <summary>
            /// 保存ボタンコントロールを取得または設定する。
            /// </summary>
            private Win32Window SaveButton { get; set; } = null;

            /// <summary>
            /// メインウィンドウのコントロール群を更新する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private bool UpdateControls()
            {
                var controls = this.MainWindow.GetDescendants();

                // トークテキスト入力欄取得
                var talkEdit =
                    controls.FirstOrDefault(c => c.ClassName.Contains("RichEdit"));
                if (talkEdit == null)
                {
                    return false;
                }

                // ボタン群ハンドル取得
                var buttons =
                    controls
                        .Where(c => c.ClassName.Contains("BUTTON"))
                        .Take(3)
                        .ToArray();
                if (buttons.Length < 3)
                {
                    return false;
                }

                this.TalkEdit = talkEdit;
                this.PlayButton = buttons[0];
                this.StopButton = buttons[1];
                this.SaveButton = buttons[2];

                return true;
            }

            /// <summary>
            /// 保存ダイアログを検索する。
            /// </summary>
            /// <returns>保存ダイアログ。見つからなければ null 。</returns>
            private Win32Window FindSaveDialog()
            {
                if (this.MainWindow == null)
                {
                    return null;
                }

                return
                    Win32Window.Desktop
                        .FindChildren(title: SaveDialogTitle)
                        .FirstOrDefault(
                            w => w.GetParent().Handle == this.MainWindow.Handle);
            }

            /// <summary>
            /// 保存ダイアログを検索する。
            /// </summary>
            /// <param name="loopCount">検索ループ回数。</param>
            /// <param name="interval">検索インターバル。</param>
            /// <returns>保存ダイアログ。見つからなければ null 。</returns>
            private Win32Window FindSaveDialog(int loopCount, TimeSpan interval)
            {
                var dialog = this.FindSaveDialog();
                for (int i = 0; dialog == null && i < loopCount; ++i)
                {
                    Thread.Sleep(interval);
                    dialog = this.FindSaveDialog();
                }

                return dialog;
            }

            /// <summary>
            /// プロセス実行中ではない場合の状態セットアップを行う。
            /// </summary>
            private void SetupDeadState()
            {
                this.MainWindow = null;
                this.TalkEdit = null;
                this.PlayButton = null;
                this.StopButton = null;
                this.SaveButton = null;

                this.IsRunning = false;
                this.IsSaving = false;
            }

            /// <summary>
            /// トークテキストのカーソル位置を先頭に移動させる。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。既にWAVEファイル保存中である場合は失敗する。
            /// </remarks>
            private bool SetTalkTextCursorToHead()
            {
                if (this.TalkEdit == null || this.IsSaving || !this.Stop())
                {
                    return false;
                }

                this.TalkEdit.SendMessage(EM_SETSEL);

                return true;
            }

            #region IProcess インタフェース実装

            /// <summary>
            /// VOICEROID識別IDを取得する。
            /// </summary>
            public VoiceroidId Id { get; }

            /// <summary>
            /// VOICEROID名称を取得する。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// プロセスが実行中であるか否かを取得する。
            /// </summary>
            public bool IsRunning
            {
                get { return this.running; }
                private set { this.SetProperty(ref this.running, value); }
            }
            private bool running = false;

            /// <summary>
            /// トークテキストをWAVEファイル保存中であるか否かを取得する。
            /// </summary>
            public bool IsSaving
            {
                get { return this.saving; }
                set { this.SetProperty(ref this.saving, value); }
            }
            private bool saving = false;

            /// <summary>
            /// トークテキストを取得する。
            /// </summary>
            /// <returns>トークテキスト。取得できない場合は null 。</returns>
            public string GetTalkText()
            {
                if (this.TalkEdit == null)
                {
                    return null;
                }

                return this.TalkEdit.GetText();
            }

            /// <summary>
            /// トークテキストを設定する。
            /// </summary>
            /// <param name="text">トークテキスト。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public bool SetTalkText(string text)
            {
                if (this.TalkEdit == null || this.IsSaving || !this.Stop())
                {
                    return false;
                }

                return this.TalkEdit.SetText(text);
            }

            /// <summary>
            /// トークテキストの再生を開始する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public bool Play()
            {
                if (
                    this.PlayButton == null ||
                    this.IsSaving ||
                    !this.SetTalkTextCursorToHead())
                {
                    return false;
                }

                this.PlayButton.PostMessage(BM_CLICK);

                return true;
            }

            /// <summary>
            /// トークテキストの再生を停止する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public bool Stop()
            {
                if (this.StopButton == null || this.IsSaving)
                {
                    return false;
                }

                this.StopButton.PostMessage(BM_CLICK);

                return true;
            }

            /// <summary>
            /// トークテキストをWAVEファイル保存する。
            /// </summary>
            /// <param name="filePath">保存希望WAVEファイルパス。</param>
            /// <returns>実際のWAVEファイルパス。失敗した場合は null 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。既にWAVEファイル保存中である場合は失敗する。
            /// 既に同じ名前のWAVEファイルが存在する場合は拡張子の手前に "[1]" 等の
            /// 角カッコ数値文字列が追加される。
            /// </remarks>
            public string Save(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException("filePath");
                }

                if (this.SaveButton == null || this.IsSaving || !this.Stop())
                {
                    return null;
                }

                // ファイルパス作成
                var path = MakeWaveFilePath(filePath);

                // 保存先ディレクトリ作成
                var dirPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(dirPath))
                {
                    try
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    catch
                    {
                        return null;
                    }
                }

                // 保存ボタン押下
                this.SaveButton.PostMessage(BM_CLICK);
                this.IsSaving = true; // 一応立てる

                // 保存ダイアログ検索
                var dialog = this.FindSaveDialog(200, TimeSpan.FromMilliseconds(10));
                if (dialog == null)
                {
                    return null;
                }

                // ダイアログ発見直後はコントロール作成が完了していないので少し待つ
                Thread.Sleep(50);

                // ファイルパス設定先のエディットコントロール取得
                // ダイアログ作成直後は未作成の場合があるので何度か調べる
                Win32Window fileNameEdit = null;
                for (int i = 0; i < 100; ++i, Thread.Sleep(10))
                {
                    fileNameEdit =
                        dialog.GetDescendants().FirstOrDefault(c => c.ClassName == "Edit");
                    if (fileNameEdit != null)
                    {
                        break;
                    }
                }
                if (fileNameEdit == null)
                {
                    return null;
                }

                // ファイルパスをエディットコントロールに設定し、ENTERキー送信
                if (!fileNameEdit.SetText(path))
                {
                    return null;
                }
                fileNameEdit.PostMessage(
                    WM_KEYDOWN,
                    new IntPtr(VK_RETURN),
                    new IntPtr(0x00000001));
                fileNameEdit.PostMessage(
                    WM_KEYUP,
                    new IntPtr(VK_RETURN),
                    new IntPtr(unchecked((int)0xC0000001)));

                return path;
            }

            #endregion

            #region Win32 API 定義

            private const uint EM_SETSEL = 0x00B1;
            private const uint BM_CLICK = 0x00F5;
            private const uint WM_KEYDOWN = 0x0100;
            private const uint WM_KEYUP = 0x0101;

            private const int VK_RETURN = 0x0D;

            #endregion
        }
    }
}
