# VoiceroidUtil

## About

VOICEROID動画制作支援ツール『VoiceroidUtil』のソースです。

* MIT License です。
* ビルドには Visual Studio 2015 が必要です。
* 一部の画像リソースは再配布禁止のため、ダミー画像がコミットされています。
* 使用したライブラリや素材の詳細は data/readme.txt を参照してください。

## Repository rules

version 1.4.0 リリース以降は下記のルールで作業しています。

* `master` には基本的に直接コミットしない。
    * 作業ブランチから Pull Request を通して反映する。
    * README.md(当ファイル)の編集と、 data/manual ディレクトリ以下をリリース直後に微修正する場合のみ例外とする。
* 機能追加、バグ修正等を行う際はまず Issue を立て、それに対応するブランチを作成して作業する。
    * 機能追加のブランチ名は `feature/Issue番号-機能内容` とする。(ex. `feature/5-exo_output`)
    * バグ修正のブランチ名は `fix/Issue番号-バグ内容` とする。(ex. `fix/7-crash_at_save`)
* リリース用のドキュメント類の更新作業は `release/バージョン名` ブランチを作成して行う。(ex. `release/v1.5.0`)
    * 基本的にはリリース直前に `master` へ反映する。
* リリース直後、対象の `master` コミットからバージョン名のタグを付ける。(ex. `v1.5.0`)
    * タグのコミットタイトルは `version バージョン番号` とする。(ex. `version 1.5.0`)
    * タグのコミット説明文には更新履歴を記載する。
