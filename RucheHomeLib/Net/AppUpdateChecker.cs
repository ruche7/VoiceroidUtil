using System;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using RucheHome.Util;

namespace RucheHome.Net
{
    /// <summary>
    /// アプリ更新情報チェッククラス。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 内部で <see cref="WebClient"/> クラスを用いている。
    /// TLS 1.1/1.2 に対応させたい場合は
    /// <see cref="ServicePointManager"/> クラスを利用すること。
    /// </para>
    /// <para>
    /// アプリ更新情報JSONファイルには、下記の項目を持つオブジェクトを定義すること。
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>項目</term>
    /// <description>値</description>
    /// </listheader>
    /// <item>
    /// <term>product</term>
    /// <description>
    /// プロダクト名を定義する。リダイレクトしないならば定義必須。
    /// </description>
    /// </item>
    /// <item>
    /// <term>version</term>
    /// <description>
    /// バージョンを定義する。リダイレクトしないならば定義必須。
    /// </description>
    /// </item>
    /// <item>
    /// <term>page_uri</term>
    /// <description>
    /// ダウンロードページURIを定義する。リダイレクトしないならば定義必須。
    /// </description>
    /// </item>
    /// <item>
    /// <term>display_name</term>
    /// <description>
    /// 表示名を定義する。定義が無ければ product および version が利用される。
    /// </description>
    /// </item>
    /// <item>
    /// <term>redirect_base_uri</term>
    /// <description>
    /// リダイレクト用のベースURIを定義する。
    /// redirect_product とどちらか片方もしくは両方が定義されていればリダイレクトする。
    /// </description>
    /// </item>
    /// <item>
    /// <term>redirect_product</term>
    /// <description>
    /// リダイレクト用のプロダクト名を定義する。
    /// redirect_base_uri とどちらか片方もしくは両方が定義されていればリダイレクトする。
    /// </description>
    /// </item>
    /// </list> 
    /// </remarks>
    public class AppUpdateChecker : BindableBase
    {
        /// <summary>
        /// アプリ更新情報JSONファイルの既定ベースURI。
        /// </summary>
        public static readonly string DefaultBaseUri =
            @"https://ruche-home.net/files/versions/";

        /// <summary>
        /// 現在実行中のプロセスを用いるコンストラクタ。
        /// </summary>
        /// <param name="baseUri">
        /// アプリ更新情報JSONファイルのベースURI。既定のURIを用いるならば null 。
        /// </param>
        /// <remarks>
        /// 実行中のプロセスに AssemblyProductAttribute 属性と
        /// AssemblyVersionAttribute 属性が定義されている必要がある。
        /// </remarks>
        public AppUpdateChecker(string baseUri = null)
            : this(Assembly.GetEntryAssembly(), baseUri)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="app">
        /// 対象アセンブリ。
        /// AssemblyProductAttribute 属性と
        /// AssemblyVersionAttribute 属性が定義されている必要がある。
        /// </param>
        /// <param name="baseUri">
        /// アプリ更新情報JSONファイルのベースURI。既定のURIを用いるならば null 。
        /// </param>
        public AppUpdateChecker(Assembly app, string baseUri = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var productAttr = app.GetCustomAttribute<AssemblyProductAttribute>();
            if (productAttr == null)
            {
                throw new ArgumentException(
                    nameof(AssemblyProductAttribute) + @" is not defined.",
                    nameof(app));
            }
            if (string.IsNullOrWhiteSpace(productAttr.Product))
            {
                throw new ArgumentException(
                    nameof(AssemblyProductAttribute) + @" is blank.",
                    nameof(app));
            }

            var version = app.GetName()?.Version;
            if (version == null)
            {
                throw new ArgumentException(
                    @"The version of application is not defined.",
                    nameof(app));
            }

            this.CurrentProduct = productAttr.Product;
            this.CurrentVersion = version;
            this.BaseUri = baseUri ?? DefaultBaseUri;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="currentProduct">現行プロダクト名。</param>
        /// <param name="currentVersion">現行バージョン。</param>
        /// <param name="baseUri">
        /// アプリ更新情報JSONファイルのベースURI。既定のURIを用いるならば null 。
        /// </param>
        public AppUpdateChecker(
            string currentProduct,
            Version currentVersion,
            string baseUri = null)
        {
            if (currentProduct == null)
            {
                throw new ArgumentNullException(nameof(currentProduct));
            }
            if (string.IsNullOrWhiteSpace(currentProduct))
            {
                throw new ArgumentException(
                    $@"`{nameof(currentProduct)}` is blank.",
                    nameof(currentProduct));
            }
            if (currentVersion == null)
            {
                throw new ArgumentNullException(nameof(currentVersion));
            }

            this.CurrentProduct = currentProduct;
            this.CurrentVersion = currentVersion;
            this.BaseUri = baseUri ?? DefaultBaseUri;
        }

        /// <summary>
        /// 現行プロダクト名を取得する。
        /// </summary>
        public string CurrentProduct { get; }

        /// <summary>
        /// 現行バージョンを取得する。
        /// </summary>
        public Version CurrentVersion { get; }

        /// <summary>
        /// アプリ更新情報JSONファイルのベースURIを取得する。
        /// </summary>
        /// <remarks>
        /// このURIの末尾に プロダクト名 + ".json" を付けたURIへアクセスする。
        /// </remarks>
        public string BaseUri { get; }

        /// <summary>
        /// 更新チェック処理中であるか否かを取得する。
        /// </summary>
        public bool IsBusy
        {
            get => this.busy;
            private set => this.SetProperty(ref this.busy, value);
        }
        private bool busy = false;

        /// <summary>
        /// 新しいバージョンのアプリが存在するか否かを取得する。
        /// </summary>
        /// <remarks>
        /// 未チェック状態では false を返す。
        /// </remarks>
        public bool CanUpdate
        {
            get => this.canUpdate;
            private set => this.SetProperty(ref this.canUpdate, value);
        }
        private bool canUpdate = false;

        /// <summary>
        /// 最新プロダクト名を取得する。
        /// </summary>
        /// <remarks>
        /// 未チェック状態では null を返す。
        /// </remarks>
        public string NewestProduct
        {
            get => this.newestProduct;
            private set => this.SetProperty(ref this.newestProduct, value);
        }
        private string newestProduct = null;

        /// <summary>
        /// 最新バージョンを取得する。
        /// </summary>
        /// <remarks>
        /// 未チェック状態では null を返す。
        /// </remarks>
        public Version NewestVersion
        {
            get => this.newestVersion;
            private set => this.SetProperty(ref this.newestVersion, value);
        }
        private Version newestVersion = null;

        /// <summary>
        /// ダウンロードページURIを取得する。
        /// </summary>
        /// <remarks>
        /// 未チェック状態では null を返す。
        /// </remarks>
        public Uri PageUri
        {
            get => this.pageUri;
            private set => this.SetProperty(ref this.pageUri, value);
        }
        private Uri pageUri = null;

        /// <summary>
        /// 表示名を取得する。
        /// </summary>
        /// <remarks>
        /// 未チェック状態では null を返す。
        /// </remarks>
        public string DisplayName
        {
            get => this.displayName;
            private set => this.SetProperty(ref this.displayName, value);
        }
        private string displayName = null;

        /// <summary>
        /// 更新チェック処理を行う。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// <para>
        /// 非同期処理はアプリ更新情報JSONファイルのダウンロード待ちでのみ行われる。
        /// JSONファイルの解析等は同期処理であるため、
        /// 必要であれば呼び出し元で Task.Run 等を用いること。
        /// </para>
        /// <para>
        /// このメソッドを複数スレッドで同時に呼び出すことはできない。
        /// 最初の呼び出しからの復帰前に他のスレッドから呼び出した場合、
        /// 何も行わず即座に false を返す。
        /// </para>
        /// <para>
        /// 各プロパティの PropertyChanged イベント通知を特定のスレッドで
        /// 受け取りたい場合、このメソッドを呼び出す前に
        /// SynchronizationContext プロパティを設定しておくこと。
        /// </para>
        /// </remarks>
        public async Task<bool> Run()
        {
            if (this.IsBusy || Interlocked.Exchange(ref this.runLock, 1) != 0)
            {
                return false;
            }

            try
            {
                this.IsBusy = true;

                AppInfo info = null;
                using (var webClient = new WebClient())
                {
                    // ユーザーエージェント設定
                    webClient.Headers.Add(
                        HttpRequestHeader.UserAgent,
                        @"Mozilla/5.0 (compatible; " +
                        this.CurrentProduct +
                        @"/" +
                        this.CurrentVersion +
#if DEBUG
                        @"; Debug" +
#endif // DEBUG
                        @")");

                    // キャッシュしない
                    webClient.CachePolicy =
                        new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

                    info =
                        await DownloadAppInfo(
                            webClient,
                            this.BaseUri,
                            this.CurrentProduct,
                            this.CurrentProduct,
                            this.CurrentVersion)
                            .ConfigureAwait(false);
                }

                this.NewestProduct = info.Product;
                this.NewestVersion = info.Version;
                this.PageUri = info.PageUri;
                this.DisplayName = info.DisplayName;

                // CanUpdate は最後に更新する
                this.CanUpdate = info.CanUpdate;
            }
            catch
            {
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref this.runLock, 0);
                this.IsBusy = false;
            }

            return true;
        }
        private int runLock = 0;

        /// <summary>
        /// アプリ情報クラス。
        /// </summary>
        private class AppInfo
        {
            /// <summary>
            /// プロダクト名を取得または設定する。
            /// </summary>
            public string Product { get; set; } = null;

            /// <summary>
            /// バージョンを取得または設定する。
            /// </summary>
            public Version Version { get; set; } = null;

            /// <summary>
            /// ダウンロードページURIを取得または設定する。
            /// </summary>
            public Uri PageUri { get; set; } = null;

            /// <summary>
            /// 表示名を取得または設定する。
            /// </summary>
            public string DisplayName { get; set; } = null;

            /// <summary>
            /// 新しいバージョンのアプリが存在するか否かを取得または設定する。
            /// </summary>
            public bool CanUpdate { get; set; } = false;
        }

        /// <summary>
        /// リダイレクトの最大許容回数。
        /// </summary>
        private const int RedirectCountLimit = 3;

        /// <summary>
        /// アプリ情報をダウンロードする。
        /// </summary>
        /// <param name="webClient">Webクライアント。</param>
        /// <param name="baseUri">ベースURI。</param>
        /// <param name="product">JSONファイル名に用いられるプロダクト名。</param>
        /// <param name="currentProduct">現行プロダクト名。</param>
        /// <param name="currentVersion">現行バージョン。</param>
        /// <param name="redirectCount">リダイレクト回数。</param>
        /// <returns>アプリ情報。ダウンロードできなかった場合は null 。</returns>
        private static async Task<AppInfo> DownloadAppInfo(
            WebClient webClient,
            string baseUri,
            string product,
            string currentProduct,
            Version currentVersion,
            int redirectCount = 0)
        {
            if (webClient == null)
            {
                throw new ArgumentNullException(nameof(webClient));
            }
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
            }
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }
            if (currentProduct == null)
            {
                throw new ArgumentNullException(nameof(currentProduct));
            }
            if (currentVersion == null)
            {
                throw new ArgumentNullException(nameof(currentVersion));
            }

            // リダイレクト回数制限オーバー
            if (redirectCount > RedirectCountLimit)
            {
                return null;
            }

            // アプリ更新情報JSONファイルをダウンロード
            var json =
                await webClient.DownloadDataTaskAsync(baseUri + product + @".json")
                    .ConfigureAwait(false);
            if (json == null || json.Length <= 0)
            {
                return null;
            }

            // JSONを読み取る
            XElement elem = null;
            using (
                var reader =
                    JsonReaderWriterFactory.CreateJsonReader(
                        json,
                        XmlDictionaryReaderQuotas.Max))
            {
                elem = XElement.Load(reader);
            }

            // リダイレクト情報確認
            var redirectBaseUriElem = elem.Element(@"redirect_base_uri");
            var redirectProductElem = elem.Element(@"redirect_product");
            if (redirectBaseUriElem != null || redirectProductElem != null)
            {
                // リダイレクトする
                return
                    await DownloadAppInfo(
                        webClient,
                        (redirectBaseUriElem == null) ?
                            baseUri : (string)redirectBaseUriElem,
                        (redirectProductElem == null) ?
                            product : (string)redirectProductElem,
                        currentProduct,
                        currentVersion,
                        ++redirectCount)
                        .ConfigureAwait(false);
            }

            // 情報取得
            var productElem = elem.Element(@"product");
            var versionElem = elem.Element(@"version");
            var pageUriElem = elem.Element(@"page_uri");
            var displayNameElem = elem.Element(@"display_name");

            if (productElem == null || versionElem == null || pageUriElem == null)
            {
                return null;
            }

            var newProduct = (string)productElem;
            var newVersion = new Version((string)versionElem);
            var pageUri = new Uri((string)pageUriElem);

            // 表示名作成
            string displayName;
            if (displayNameElem == null)
            {
                // プロダクト名は変化している時のみ付ける
                displayName =
                    ((newProduct == currentProduct) ? "" : (newProduct + @" ")) +
                    @"version " + newVersion;
            }
            else
            {
                displayName = (string)displayNameElem;
            }

            // プロダクト名が変化している場合も新バージョン扱い
            bool canUpdate =
                (newVersion > currentVersion || newProduct != currentProduct);

            return
                new AppInfo
                {
                    Product = newProduct,
                    Version = newVersion,
                    PageUri = pageUri,
                    DisplayName = displayName,
                    CanUpdate = canUpdate,
                };
        }
    }
}
