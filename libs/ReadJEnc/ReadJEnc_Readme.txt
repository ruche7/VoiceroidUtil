================================================================================
==
== ReadJEnc C#(.NET)用ファイル文字コード種類自動判別ライブラリ
==   Ver 1.2.2.0309 (2015/03/09)
==
==   Copyright (C) 2014-2015 hnx8(H.Takahashi) 
==    http://hp.vector.co.jp/authors/VA055804/
==
== Released under the MIT license
== http://opensource.org/licenses/mit-license.php
==
================================================================================

【１】ReadJEncについて

C#(.NET Framework)向けテキストファイル文字コード自動判別＆読出ライブラリです。
コンパイル済みDLL版／C#ソースコード版のお好きなほうをお使いいただけます。
また、ライブラリ使用例のサンプルアプリケーションを同梱しています。
（文字コード一括調査ツールとして最低限の機能を備えています）

※自作grepソフト(HNXgrep)の文字コード判別処理をライブラリ化したものです。
　実用的な文字コード一括調査／変換ツールをお探しであれば、
　HNXgrepの検索モード「改行インデント走査」「ハッシュ一括算出」、
　および「ファイル形式変換」機能をお試しください。
　HNXgrepは http://hp.vector.co.jp/authors/VA055804/HNXgrep/ から入手できます。

＜特徴＞

(1)BOMあり/BOMなしUTF、ShiftJIS、EUC/JIS(補助漢字可)のほか、ANSI(CP1252)も判別
　 非テキストファイル(バイナリファイル)の種類判別にも対応
 ※モード切替により中国語繁体字・中国語簡体字・ハングルも判別可能

(2)アプリケーションへの組み込みに適したコンパクトなライブラリ
 ・DLL版サイズ13KB（不要機能を割愛すればさらにコンパクト化）

(3)軽量高速のわりに高精度な文字コード判定
 ・複数の文字コードでデコード可能な場合、どの文字コードとみなすのがより妥当か
　 直前に出てきた文字との整合性をもとに判定、
　 ShiftJIS半角カナやANSIなどの誤判定が起こる可能性を低減
 ・複数ファイル連続読み出し時は、非テキストであることが明らかなファイルの
　 読み込み・判別をスキップしたりと、高速化のための各種チューニングを実施

(4)ファイル読み出し～stringテキスト取り出しまで一括実行
 ・ファイルオープンエラー／テキストのデコード失敗等はライブラリ内でチェック、
　 呼び出しアプリケーション側でのエラーハンドリング処理は原則不要
 ・ファイルではなくByte配列に対しての文字コード判別も可能


＜判別可能なファイル文字コード種類＞

(1)BOMつきUnicode(UTF-8/UTF-16/UTF-16B/UTF-32/UTF-32B)
(2)BOMなしUnicode(UTF-8N、およびASCII文字始まりのUTF-16BE/UTF-16LE)
(3)ASCII    : 非ASCII文字が１文字も登場しないテキストファイル
(4)ANSI欧米 : 欧米版WindowsのISO-8859-1(CP1252)
(5)ShiftJIS : MS版(CP932)
(6)EUCJP    : MS版(CP51932)／0x8F補助漢字ありEUC(CP20932相当)の２種類を識別
(7)JIS      : MS版(CP50221/CP50222)／JIS90補助漢字(CP20932相当)
(8)ISO2022KR(CP50225)
(9)文字コード判別言語（デフォルト文字コード）を切り替えた場合、
　　ShiftJIS(CP932)/EUC-JP(CP51932)の代わりに以下の文字コードについて判別
　　・Big5(台)  : 繁体字中国語 Big5(CP950)／EUC-TW(CP20000)
　　・GB18030   : 簡体字中国語 GB18030(CP54936)／EUC-CN(CP51936)
　　・UHC(韓)   : ハングル     UHC(CP949)／EUC-KR(CP51949)
　　・ANSI欧米  : (UTF/ASCII/ANSI/ISO2022の判定のみ行い、ShiftJIS/EUC等は無視)
(10)非テキストファイル(以下のものを識別)
　・画像ファイル(BMP/GIF/JPEG/PNG/TIFF/ICON)
　・圧縮ファイル(ZIP/GZIP/7z/RAR/CAB)
　・PDFファイル
　・Javaバイナリ(classファイル)
　・Windowsバイナリ(exe,dll等)
　・Windowsショートカットファイル
　・上記いずれにも該当しない非テキストファイル
　・空ファイル、巨大ファイル、読み出しエラーとなったファイル


＜対応.NET Frameworkバージョン＞

 .NET Framework 2.0以降

＜補足＞

 ReadJEncは、「りーどじぇんく」と読みます。


【２】ライブラリの使い方

◎DLL版を使用する場合は、zipファイルから「Hnx8.ReadJEnc.dll」を取り出し、
  Visual Studioプロジェクトの参照設定に追加してください。

◎C#ソースコード版を使用する場合は、zipファイル内の「src\ReadJEnc」フォルダから
  CharCode.cs／FileType.cs／FileReader.cs／ReadJEnc.csの４ファイルを取り出し、
  Visual Studioプロジェクトに追加してください。

◎テキストファイル読み出し・文字コード判別の基本的な手順ですが、
    1) FileReaderオブジェクトを生成
    2) Read()メソッドを呼び出し、ファイル文字コード種類を把握
    3) Textプロパティより、実際に読み出したテキストを取得
  という流れになります。

	--ソースコード例--------------------------------------------------------
	//ファイル文字コード種類判別対象ファイルをFileInfoオブジェクトなどで指定
	void Test(System.IO.FileInfo file) 
	{
	    //文字コード自動判別読み出しクラスを生成
	    using (Hnx8.ReadJEnc.FileReader reader = new FileReader(file))
	    {
	        //判別読み出し実行。判別結果はReadメソッドの戻り値で把握できます
	        Hnx8.ReadJEnc.CharCode c = reader.Read(file);
	        //戻り値のNameプロパティから文字コード名を取得できます
	        string name = c.Name;
	        Console.WriteLine("【" + name + "】" + file.Name);
	        //GetEncoding()を呼び出すと、エンコーディングを取得できます
	        System.Text.Encoding enc = c.GetEncoding(); 
	        //実際に読み出したテキストは、Textプロパティから取得できます
	        //（非テキストファイルの場合は、nullが設定されます）
	        string text = reader.Text;
	        //戻り値の型からは、該当ファイルの大まかな分類を判断できます
	        if (c is CharCode.Text) 
	        {
	            Console.WriteLine("-------------------------------------");
	            Console.WriteLine(text);
	        }
	    }
	}
	------------------------------------------------------------------------

◎Byte配列の内容から文字コードを判別する場合は、
  ReadJEncクラスのインスタンスメソッド『GetEncoding』を呼び出します。
  （引数1：Byte配列、引数2：Byte長を指定します。
    戻り値で文字コード判別結果が、out引数3で取り出せたテキスト内容が返ります）


◎「@IT」の技術情報フォーラムにて、HttpClientクラスでWebページの文字コードを
  推定する方法が詳細に解説されています。こちらも参照ください。

  .NET TIPS：ReadJEncを使って文字エンコーディングを推定するには？［C#、VB］
  http://www.atmarkit.co.jp/ait/articles/1501/20/news073.html


【３】サンプルアプリケーションについて

「ReadJEncSample.exe」が、実際にライブラリを使用したサンプルです。
 （ロジック部100行程度の小さなアプリケーションです。ソースも同梱しています）

ファイルまたはフォルダのフルパスを「Path」に入力、「実行」ボタンを押すと
画面下段に文字コード判別結果が表示されます。

⇒ファイル指定時は、文字コード判別結果・読み出したテキストが表示されます。
  フォルダ指定時は、そのフォルダ配下の全ファイルの文字コード判別結果が
  タブ区切りで一覧表示されます。

⇒「最大サイズ」「デフォルト文字コード」を切り替えることで、
  動作モードを変更できます。

⇒「結果をクリップボードにコピー」ボタンをクリックし、
   テキストエディタやExcelに結果を貼り付けることもできます。


【４】ライブラリの仕様について

 本ライブラリのソースコードは、以下の４ファイルで構成されています。

 (1) FileReader.cs : ファイル読み出し＆文字コード種類自動判別クラス

    ⇒ファイル読み込み、文字コード判別・テキスト取り出しの機能を提供します。
      （内部でReadJEncクラスなどの呼び出しを行うための窓口となるクラスです）

 (2) ReadJEnc.cs   : 文字コード自動判別処理クラス本体

    ⇒バイト配列から文字コード判別を行う機能を提供します。
    （各国語の文字コード判定用内部クラス：SJIS/BIG5TW/GB18030/UHCKR、および
      JIS判定支援Staticクラスを内包しています。
      フィールドANSI/JA/TW/CN/KRが、各クラスを実体化したインスタンスです）

 (3) CharCode.cs   : 文字コード種類定義クラス（列挙相当）

    ⇒文字コードの名前/エンコーディング、BOMプリアンブル情報を保持します。
      本ライブラリで判別する文字コード全種類がstatic(readonly)フィールドで
      定義されており、デコード機能などが利用できます。

 (4) FileType.cs   : 非テキストファイル種類定義クラス（列挙相当）

    ⇒FileReaderクラスで読み込んだファイルがテキストファイルではない場合、
      ファイルの先頭バイト(マジックナンバー)からファイル種類を識別するための
      定義情報を保持し、ファイル種類判別機能を提供します。
      （CharCodeクラスの機能を流用しています）

  ※ファイルからの読込機能を利用しない場合は、FileReader.cs/FileType.csは
    不要です。ReadJEnc.cs/CharCode.cs の２ファイルだけ使用してください。

  ※条件付コンパイルシンボル「JPONLY」を設定すると、
    日本語以外の文字コード定義／文字コード判定処理がコンパイル対象外となります。
    （コンパイルサイズがさらにコンパクトになります）

 ライブラリのAPI/列挙相当フィールド定義などについては、
 同梱の「Hnx8.ReadJEnc.xml」を参照ください。（ドキュメントコメントどおりです）

 仕様詳細ならびに文字コード判定の基準/考え方(アルゴリズム)については、
 ソースコードを直接参照願います。
 （ソースを読み解く助けとなるよう、コメントは多めにしてあります。
   実行速度向上などのため一部トリッキーな書き方をしている箇所もありますが、
   C#特有の構文などは極力使わないようにしており、
   読み解くのも、他言語への移植等も、比較的容易かと思っております）


【５】著作権・使用規定

ReadJEnc、および同梱サンプルアプリケーション(ReadJEncSample)について、
著作権は作者hnx8(H.Takahashi)が有しています。
ライセンス形態は「MITライセンス」に準拠とします。
ライセンスの範囲内であれば無償で自由にお使いいただけます。
（要点は、利用・改造・再配布時に著作権表示を残す、無保証、の２点です）

利用・再配布や改良については、何らかの形でフィードバック連絡をいただけると
助かります。

MITライセンスの範囲を超えた使用（たとえば著作権表記を消す、など）については
現状のところ想定しておりません。
そのような場合には事前に連絡・相談のほどお願いいたします。


本ライブラリは無償で利用できますが寄付も受け付けています。
2015年時点では、このライブラリを利用したGrepソフト「HNXgrep」の補足ページ
（ http://hp.vector.co.jp/authors/VA055804/HNXgrep/#ReadJEnc ）などから
送金できます。


【６】制限事項

※ReadJEncの文字コード識別アルゴリズムは自作ですが、テキストへのデコードには
  Windows(.NET Framework)標準のエンコーディングをそのまま使用しています。
  MS互換ではないShiftJIS,EUC,JIS（csShiftJIS,IBM版CP932など）は、
  正しくマッピングされないことがあります。

※ShiftJIS,EUC,JISの方言を識別する機能は備わっておりません。

※不当な文字コードが混入していないかの最終的なチェックも、
  Windows(.NET Framework)標準のエンコーディングに一任しています。
  文字コードとしておかしいにもかかわらずWindowsの仕様としてデコードできるなら、
  妥当な文字コードであるとみなしてしまいます。


【７】サポート・連絡先

連絡、ご意見ご感想、不具合報告などについては、
作者が開設しているBLOG（ http://d.hatena.ne.jp/hnx8/ ）にて
コメント等いただければと思います。

このライブラリの技術情報・解説なども、ひょっとして気が向いたら
BLOGの記事などで書き起こすかもしれません。


【８】更新履歴

■2015.03.09(Ver 1.2.2.0309)
・Readmeに「＠IT」解説記事へのリンクを追加、その他Readmeの一部手直し
・補助漢字ありJISのCharCode定義を独立させる。また判定ロジックを微調整
・メソッド引数名の大文字始まり(バイナリサイズ削減効果を狙ったもの)を一部取止め

■2014/12/06(Ver 1.2.1.1206)
・JIS(ISO-2022-JP)デコード時にJISX0212補助漢字がまったくデコードできていなかった
  不具合を修正、JIS判定ロジック関連の改善

■2014/08/18(Ver 1.2.0.0818)
・EUC補助漢字がまったくデコードできていなかった不具合を修正
  (0x8FのEUC補助漢字について、CP20932コードテーブルで対応可能な範囲で対応)
・JISX0201のエスケープシーケンスなし7bit半角カナファイルに対応
・ISO-2022-KRの判別に対応
・バイト配列の文字コード自動判別メソッドをprivate⇒public化
・「優先EUC」の機能を「デフォルト文字コード」機能に全面変更
・初回起動時(おそらくコードテーブル読み出しに)時間がかかる問題への対策を実施
・これらに伴う全面的なソースコード構造改善

■2014/07/13(Ver 1.1.2014.0713)
・他プロセスがロックしているファイルが読み出しできない不具合を修正
・ファイル文字コード種類定義にプリアンブル(BOM/マジックナンバー)情報を
  持たせるよう構造変更、プリアンブルのチェック仕様を改善。
・UTF16N判定処理をBOM判定処理内に移動
・判別ファイル種類にWindowsショートカット、Windowsアイコン・TIFF、7z・CABを追加
・その他全般的なソースコード構造改善、
  日本語以外の文字コード定義・文字コード判定処理を条件付コンパイル化

■2014/06/07(Ver 1.0.2014.0607)
・初公開
　(自作grepツール「HNXgrep」Ver1.4で採用しているものとほぼ同一内容)

(以下、HNXgrep開発時の更新履歴)

■2013年末～2014年前半頃
・HNXgrep過去バージョンのソース・開発知見をもとに、
　文字コード自動判別ライブラリを抜本的に作り直し

□2012年後半～2013年前半頃
・自作grepツール向けの文字コード自動判定処理につき中規模改善
　(HNXgrep Ver1.2/Ver1.3にて採用、ソースコードは未公開)

□2012年1月～2月頃
・自作grepツール向けの文字コード自動判定処理として、DOBON.NET様の
　「文字コードを判別する(http://dobon.net/vb/dotnet/string/detectcode.html)」の
　ソースを流用・改善し、以下のURLにて公開
　http://d.hatena.ne.jp/hnx8/20120225/1330157903
　(HNXgrep Ver1.0にて採用)


【９】参考にしたサイト等（メモ、敬称略）

(1)文字コードに関する情報全般
・書籍「文字コード「超」研究」 深沢 千尋/著、ISBN:4899770510
・とほほのWWW入門/漢字コード
   http://www.tohoho-web.com/ex/draft/kanji.htm
・noocyte のプログラミング研究室/文字コードに関する覚え書きと実験
   http://www5d.biglobe.ne.jp/~noocyte/Programming/CharCode.html
・しいしせねっと/文字コードの墓場
   http://siisise.net/charset.html
・euc.JP/文字コードの話
   http://euc.jp/i18n/charcode.ja.html

(2)文字コード体系
・RFC http://tools.ietf.org/html
・通信用語の基礎知識 http://www.wdic.org/
・Wikipedia http://ja.wikipedia.org/wiki
・文字コード表 http://charset.uic.jp/
・ASHホームページ/文字コード体系 http://ash.jp/code/index.htm
・CyberLibrarian/文字コード http://www.asahi-net.or.jp/~ax2s-kmtn/character/

(3)JIS/EUCに関する詳細情報
・森山 将之のホームページ/JIS X 0201 片仮名
   http://www2d.biglobe.ne.jp/~msyk/charcode/jisx0201kana/index.html
・めらんこーど地階/Encode::JP::JIS7 のJIS X 0201片仮名対応
   http://d.hatena.ne.jp/anon_193/20090108/1231428802

・Legacy Encoding Project/[LE-talk-ja 3] ISO-2022-JP-MS について(CP5022xに関する特異なコード範囲)
   http://sourceforge.jp/projects/legacy-encoding/lists/archive/talk-ja/2006-March/000002.html

・汁ムゴ魚/EUC対応について(#1-#7)
   http://wantech.ikuto.com/2006/0219/0037/euc%E5%AF%BE%E5%BF%9C%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6.htm 等
・コードページ932(森山 将之)/eucJP-msとCP51932の違い
   http://msyk.at.webry.info/200511/article_2.html

(4).NET FrameworkのEncoding関連ソースコード
・Encoding.cs
   http://referencesource.microsoft.com/#mscorlib/system/text/encoding.cs
・ISO2022Encoding.cs（大変素敵な実装になっています・・・・・コメントを読んでのけぞってください）
   http://referencesource.microsoft.com/#mscorlib/system/text/iso2022encoding.cs
・EUCJPEncoding.cs（これもまた大変素敵な実装になっています・・・・・コメントを読んでのけぞってください）
   http://referencesource.microsoft.com/#mscorlib/system/text/eucjpencoding.cs

(5)文字コード判別ソースコード
・DOBON.NET 文字コードを判別する
   http://dobon.net/vb/dotnet/string/detectcode.html
・雅階凡の C# プログラミング C#2008 文字コードの判定【リンク切れ】
   http://www.geocities.jp/gakaibon/tips/csharp2008/charset-check.html

 ほか、沢山。

