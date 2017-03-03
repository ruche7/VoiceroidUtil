# VoiceroidUtil

## About

VOICEROID動画制作支援ツール『VoiceroidUtil』のソースです。

* MIT License です。
* ビルドには Visual Studio 2015 が必要です。
* RucheHomeLib および libs/Windows-API-Code-Pack-1.1 は submodule です。
* 使用ライブラリの一部はNuGetパッケージです。
* 画像リソースの一部は再配布禁止のため、ダミー画像がコミットされています。
* 使用したライブラリや素材の詳細は [data/readme.txt](data/readme.txt) を参照してください。

## Repository rules

version 1.4.0 リリース以降は下記のルールで作業しています。

* `master` には基本的に直接コミットしない。
    * 作業ブランチから Pull Request を通して反映する。
    * [README.md](README.md)(当ファイル)の編集と、 [data/manual](data/manual) ディレクトリ以下をリリース直後に微修正する場合のみ例外とする。
* 機能追加、バグ修正等を行う際はまず Issue を立て、それに対応するブランチを作成して作業する。
    * 機能追加のブランチ名は `feature/Issue番号-機能内容` とする。(ex. `feature/5-exo_output`)
    * バグ修正のブランチ名は `fix/Issue番号-バグ内容` とする。(ex. `fix/7-crash_at_save`)
* リリース用のドキュメント類の更新作業は `release/vバージョン番号` ブランチを作成して行う。(ex. `release/v1.5.0`)
    * 基本的にはリリース直前に `master` へ反映する。

## Release work

リリース時に行う作業まとめ。

1. ローカルの `release/vバージョン番号` ブランチで下記作業を行う。
    1. [data/readme.txt](data/readme.txt) を更新する。
    2. [data/manual](data/manual) ディレクトリ以下を更新する。
    3. [VoiceroidUtil/Properties/AssemblyInfo.cs](VoiceroidUtil/Properties/AssemblyInfo.cs) の `AssemblyVersion` 属性を書き換える。
    4. `git push` を実施する。
2. `release/vバージョン番号` ブランチの Pull Request を行い、 `master` へマージする。
    * Pull Request のタイトルは `version バージョン番号` とする。
3. ローカルの `master` ブランチで下記作業を行う。
    1. `git pull` および `git submodule update` を実施する。
    2. [build_and_pack.bat](build_and_pack.bat) でビルドする。
    3. __release/VoiceroidUtil ディレクトリを __release/VoiceroidUtil-日付 ディレクトリに改名してZIP圧縮する。
4. Draft a new release する。
    * タグは `vバージョン番号` とする。(ex. `v1.5.1`)
    * タイトルは `version バージョン番号` とする。(ex. `version 1.5.1`)
    * 説明文には更新履歴を簡潔に記載する。
    * ZIPファイルを添付する。
5. Webサイトで下記作業を行う。
    1. ダウンロードページにZIPファイルを添付し、リンクを更新する。
    2. マニュアルページを [data/manual](data/manual) ディレクトリ以下のデータで更新する。
    3. トップページを更新する。
    4. アプリ更新情報JSONファイルを更新する。
6. Twitterでの告知等を行う。
