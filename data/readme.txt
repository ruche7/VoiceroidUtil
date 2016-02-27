================================================================================
<< VoiceroidUtil >>

   Update : 2016-03-xx
  Version : 1.0.0
       By : ルーチェ

================================================================================
■はじめに

『VoiceroidUtil』は、VOICEROID+シリーズソフトウェアにおける音声ファイルの
作成操作を自動化し、VOICEROID素材の作成を支援するツールです。
また、ゆっくりMovieMaker3のタイムラインへの自動入力もサポートします。

当ツールはすべてのVOICEROID+シリーズに対応しているわけではありません。
対応シリーズについては動作環境の項をご覧ください。

最新版は私のWebサイトからどうぞ。
- http://www.ruche-home.net/

VOICEROID+は、株式会社AHSから発売されている合成音声ソフトウェアです。
動画投稿サイト向けの実況動画作成等に利用されています。
詳しくは公式サイトをご覧ください。
- http://www.ah-soft.com/

ゆっくりMovieMaker3 は、饅頭遣い氏が開発した動画作成補助ツールです。
音声ファイルを指定することで、ゆっくりボイス以外の音声にも対応可能です。
詳しくは公式サイトをご覧ください。
- http://manjubox.net/YukkuriMovieMaker.php

================================================================================
■動作環境

下記の環境が必要です。

- Windows 7, Windows 8.1, Windows 10
    - Vista, 8 でも恐らく動きますが保証しません。
    - XP では動きません。

- .NET Framework 4.5 以降
    - Windows 7 以前の場合、 Windows Update よりインストールしてください。

- VOICEROID+シリーズソフトウェア
    - 現在は下記のシリーズのみ対応しています。
      [ ] 内は動作確認済みバージョンです。
        - VOICEROID+ 結月ゆかり EX           [1.7.3]
        - VOICEROID+ 民安ともえ(弦巻マキ) EX [1.7.3]
        - VOICEROID+ 東北ずん子 EX           [1.7.3]
        - VOICEROID+ 琴葉茜・葵              [1.7.3]

- ゆっくりMovieMaker3
    - 動作確認済みバージョンは 3.4.8.1 です。
    - 必須ではありません。無くとも動作します。

================================================================================
■インストール手順

展開したファイル群を、フォルダ構成を崩さずに適当な場所に置いてください。
"VoiceroidUtil.exe" (結月ゆかりのアイコン)がツール本体です。

================================================================================
■利用方法

私のWebサイトでマニュアルを公開しています。
- http://www.ruche-home.net/doc/voiceroid-util

================================================================================
■アンインストール手順

インストール時に配置したファイル群を削除してください。

一度でも使用すると、ローカルアプリケーションフォルダに設定が保存されます。
ローカルアプリケーションフォルダは Windows 7 以降の標準では次の場所になります。

- "C:\Users\[ユーザ名]\AppData\Local"

"AppData" フォルダは隠しフォルダです。
このフォルダ内の次のフォルダに設定が保存されます。

- "ruche-home\VoiceroidUtil"

完全に削除するには、この "VoiceroidUtil" フォルダも削除してください。
大したサイズにはならないはずなので、無理に削除する必要はありません。

================================================================================
■使用ライブラリ・素材

下記のライブラリを利用しています。

『ReactiveProperty』
- Copyright (C) 2015 neuecc, xin9le, okazuki
- The MIT License
- https://github.com/runceel/ReactiveProperty

『Rx (Reactive Extensions)』
- Copyright (C) Microsoft
- Apache License 2.0
- http://rx.codeplex.com/

『Livet』
- Copyright (C) 2010 Livet Project
- zlib/libpng License
- http://ugaya40.hateblo.jp/entry/Livet

『Extended WPF Toolkit Community Edition』
- Copyright (C) Xceed
- Microsoft Public License (Ms-PL)
- http://wpftoolkit.codeplex.com/

『Windows API Code Pack』
- Copyright (C) Microsoft
- The MIT License
- https://github.com/devkimchi/Windows-API-Code-Pack-1.1

『ReadJEnc』
- Copyright (C) 2014 hnx8(H.Takahashi)
- The MIT License
- http://hp.vector.co.jp/authors/VA055804/

また、阿国氏のVOICEROIDキャラクター画像素材を利用しています。
- http://seiga.nicovideo.jp/seiga/im4538340
- http://seiga.nicovideo.jp/seiga/im4608092
- http://seiga.nicovideo.jp/seiga/im4654903
- http://seiga.nicovideo.jp/seiga/im4681154

ライブラリ開発者、素材作成者各位には厚く御礼申し上げます。

================================================================================
■免責事項

ツールの利用は自己責任でお願いします。
当ツール作者(ルーチェ)は、当ツールによって発生した損害等の責を一切負いません。

================================================================================
■連絡先

作者Webサイト
- http://www.ruche-home.net/

作者メールアドレス
- webmaster@ruche-home.net

作者Twitter
- @ruche7

================================================================================
■更新履歴

------------------------------------------------------------
◆2016-03-xx [1.0.0]

- 初版公開。

================================================================================
EOF
