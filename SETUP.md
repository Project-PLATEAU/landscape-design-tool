# 開発初期設定ガイド

このドキュメントは、本プロジェクトの初期セットアップ手順です。

本プロジェクトは、[景観まちづくりDX v2.0](https://github.com/Synesthesias/landscape-design-tool) を上流リポジトリとし GitHub 上で `Fork` するのではなく、`mirror` 方式（完全なクローン）により派生させた inhouse 専用リポジトリです。

これは、社内向けの開発を非公開で進める必要があるため、GitHub の公開フォーク機能ではなく、独立したプライベートリポジトリとして管理していることに起因します。

そのため、上流リポジトリとは `remote` を通じて手動で同期を行い、必要に応じて `Pull Request` を通じてフィードバックを返す運用となっています。

| リポジトリ             | 用途                 | リモート名 | URL                                             |
|------------------|----------------------|------------|--------------------------------------------------|
| inhouse リポジトリ | 本プロジェクトのリポジトリ        | `origin`   | `git@github.com:Synesthesias/inhouse-landscape-design-tool.git` |
 | 上流リポジトリ    | 国交省向けに共有しているリポジトリ | `upstream` | `git@github.com:Synesthesias/landscape-design-tool.git` |

| 操作                         | リモート名 | 説明                                     |
|------------------------------|------------|------------------------------------------|
| 開発ブランチ作成・push       | origin    | 通常の開発作業はこちら                   |
| 上流への変更反映（PR）       | upstream     | cherry-pickしてPRを送る                   |
| 上流の更新を取り込む         | upstream → origin | mainをpullしoriginにpush         |


## 初期セットアップ手順（clone時）

1. `inhouse` リポジトリを clone します

```bash
git clone git@github.com:Synesthesias/inhouse-landscape-design-tool.git
cd inhouse-landscape-design-tool
```

2. 上流（public）リポジトリを `upstream` として追加します

```bash
git remote add upstream git@github.com:Synesthesias/landscape-design-tool.git
```

3. リモート設定の確認

```bash
git remote -v
# origin  git@github.com:Synesthesias/inhouse-landscape-design-tool.git (fetch)
# origin  git@github.com:Synesthesias/inhouse-landscape-design-tool.git (push)
# upstream        git@github.com:Synesthesias/landscape-design-tool.git (fetch)
# upstream        git@github.com:Synesthesias/landscape-design-tool.git (push)
```

## 開発フロー

### 通常の開発の場合（inhouse向け）

```bash
git checkout -b feature/your-feature
# コーディング＆コミット
git push origin feature/your-feature
```

### 上流リポジトリへフィードバックする場合（PR）

```bash
# upstream/main から作業用ブランチを作成
git checkout -b upstream/fix-issue upstream/main

# 必要なコミットを cherry-pick
git cherry-pick <コミットID>

# 上流へ push
git push upstream upstream/fix-issue

# → GitHub上でPR作成
```

### 上流リポジトリの更新を origin に反映する場合

```bash
git checkout main
git pull upstream main
git push origin main
```

## 補足

- `origin` や `upstream` という名前はあくまでローカルでの呼び名です。リモート先を間違えないよう注意してください。
- 初回 `clone` 時の `origin` は自動的に `inhouse` リポジトリとして設定されます。