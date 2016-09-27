# VoiceroidUtil

## About

『VOICEROID+』シリーズを自動操作して音声の再生やWAVEファイル保存を行います。  
また、保存したWAVEファイルのパスを『ゆっくりMovieMaker3』に渡すこともできます。

ビルドには Visual Studio 2015 が必要です。

使用したライブラリや素材の詳細は data/readme.txt を参照してください。

## Repository rules

version 1.4.0 リリース以降は下記のルールで作業しています。

* `master` には基本的に直接コミットしない。
    * 作業ブランチから Pull Request を通して rebase + merge する。
    * README.md(当ファイル)の編集と、 data/manual ディレクトリ以下をリリース直後に微修正する場合のみ例外とする。
* 機能追加、バグ修正等を行う際はまず Issue を立て、それに対応するブランチを作成して作業する。
    * 機能追加のブランチ名は `feature/Issue番号` とする。(ex. `feature/5`)
    * バグ修正のブランチ名は `hotfix/Issue番号` とする。(ex. `hotfix/7`)
* リリース用のドキュメント類の更新作業は `release/バージョン名` ブランチを作成して行う。(ex. `release/v1.5.0`)
    * 基本的にはリリース直前に `master` へ反映する。
* リリース直後、対象の `master` コミットからバージョン名のタグを付ける。(ex. `v1.5.0`)
    * タグのコミットタイトルは `version バージョン番号` とする。(ex. `version 1.5.0`)
    * タグのコミット説明文には更新履歴を記載する。
