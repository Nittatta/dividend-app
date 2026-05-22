# AGENTS.md — Codex / AIエージェント向け指示書

> このファイルは **OpenAI Codex** など AI コーディングエージェントが本リポジトリを扱うための指示書です。
> 詳細なプロジェクト仕様は **`CLAUDE.md` を必ず先に読んでください**。本ファイルはその要約 + エージェント運用上の注意です。
> 最終更新：2026-05-19

---

## 1. プロジェクト要約

- **名称**：高配当株ポートフォリオ管理 Web アプリ（高配当PF）
- **公開URL**：https://nittatta.github.io/dividend-app/
- **形式**：単一HTMLファイル完結のブラウザアプリ（サーバー不要）
- **技術**：Vanilla HTML / CSS / JavaScript のみ。**Node.js・npm・ビルドツール一切不使用**
- **主データ**：楽天証券CSV / J-Quants API V2（Cloudflare Workers プロキシ経由）
- **ストレージ**：LocalStorage（最大60ヶ月スナップショット）

詳細は [CLAUDE.md](CLAUDE.md) を参照。

---

## 2. ファイル構成と編集ルール

```
dividend-app/
├── CLAUDE.md       ← 詳細仕様書（必読・正典）
├── AGENTS.md       ← このファイル（Codex向け要約）
├── app.html        ← 開発用ソース【ここだけ編集する】
├── index.html      ← GitHub Pages 配信用【minify.ps1 の出力・直接編集禁止】
├── minify.ps1      ← app.html → index.html 変換スクリプト
├── sw.js           ← Service Worker（CACHE_NAME を更新する）
├── manifest.json   ← PWA設定
└── icons/          ← PWAアイコン
```

### 厳守ルール

1. **編集は `app.html` のみ**。`index.html` は自動生成物。
2. **単一ファイル構成を維持**。外部 JS/CSS への分割禁止。
3. **フレームワーク追加禁止**（React・Vue・jQuery 等）。Vanilla JS で実装。
4. **コメントは日本語、変数/関数名は英語（camelCase）**。
5. **LocalStorage アクセスは `getStorage()` / `setStorage()` 経由**。直接 `localStorage.xxx` 禁止。

---

## 3. Codex 向けセットアップ手順

### 初回セットアップ

```powershell
# 1. リポジトリ取得
cd C:\Users\drums\Documents\GitWork
git clone https://github.com/Nittatta/dividend-app.git
cd dividend-app

# 2. ローカル動作確認（任意・サーバー不要だが CORS 回避用）
#   ブラウザで app.html を直接開くか、簡易サーバーで起動
python -m http.server 8000
# → http://localhost:8000/app.html
```

**必要なツール**：PowerShell（Windows 標準）のみ。npm・Node 不要。

### 開発フロー（必須手順）

```powershell
cd C:\Users\drums\Documents\GitWork\dividend-app

# 1. app.html を編集

# 2. minify 実行（index.html を再生成）
powershell -ExecutionPolicy Bypass -File minify.ps1

# 3. sw.js キャッシュバージョンを +1（PWA更新が必要な場合）
#    → CACHE_NAME = 'hdp-cache-v16' を 'v17' へ

# 4. コミット & push
git add -A
git commit -m "変更内容の説明"
git push origin main
```

**注意**：
- `minify.ps1` を実行せず push すると **本番（GitHub Pages）に反映されない**
- 機密ファイル（`.env`, APIキー含むファイル, `.claude/`, `.codex/` 等）を絶対にコミットしない
- `.gitignore` を確認・維持すること

---

## 4. 外部サービス連携

| サービス | 用途 | 認証情報の置き場所 |
|---|---|---|
| GitHub Pages | ホスティング | リポジトリ public・認証不要 |
| Cloudflare Workers | J-Quants API プロキシ（CORS回避） | Workers Secret `JQUANTS_API_KEY`（**コードに含めない**） |
| J-Quants API V2 | 株価・配当・財務データ | Light プラン・60req/min |

**APIキーをソースコードに書かない**。`jquants-proxy.drumsbaka.workers.dev` 経由でアクセスする設計。

---

## 5. テスト・検証

自動テストフレームワークは導入していません。検証は以下で行います：

1. **ブラウザで `app.html` を直接開く**（Chrome / Edge 最新版）
2. **DevTools Console でエラー確認**
3. **スマホUI確認**：DevTools の Device Mode（max-width: 600px でレイアウト切替）
4. **PWA動作確認**：本番デプロイ後にスマホでホーム画面追加→動作テスト

実装変更時は必ず以下を手動確認：
- 7タブの表示・並び替え（PC: D&D / スマホ: 長押しD&D）
- CSVインポート（保有株CSV: UTF-8 / 配当金CSV: **CP932**）
- LocalStorage キー（`hdp_*`）が壊れていないか

---

## 6. よくある落とし穴

| 症状 | 原因 | 対処 |
|---|---|---|
| 本番に反映されない | minify.ps1 未実行 | `minify.ps1` を実行して index.html を再生成 |
| PWAで古いまま | sw.js の CACHE_NAME が同じ | `CACHE_NAME` を +1 |
| 配当金CSV文字化け | UTF-8 で読んでいる | `TextDecoder('shift-jis')` を使う |
| J-Quants データなし | フィールド名違い | `CLAUDE.md §8-3` の実証済みフィールド表を参照（`Sales`/`OP`/`EPS`/`EqAR` 等） |
| 429エラー | レート超過 | 自動リトライ（10s→30s→60s）実装済み・追加処理不要 |
| デプロイ失敗 | `.claude/` `.codex/` 等が混入 | `.gitignore` で除外を維持 |

---

## 7. エージェント運用上の注意

- **破壊的操作（`git push --force`, `reset --hard`, ブランチ削除）はユーザー確認後**
- **コミットメッセージは日本語可**（既存履歴に合わせる）
- **大規模リファクタは事前にユーザー合意**を取る（単一ファイル構成のため影響範囲が広い）
- **CLAUDE.md と AGENTS.md の整合性**：仕様変更時は両方更新するか、AGENTS.md から CLAUDE.md を参照する形を維持
- 機密ファイルの誤コミット防止のため、**`git add .` より `git add -A` の前に `git status` で確認**

---

## 8. 参考

- 詳細仕様：[CLAUDE.md](CLAUDE.md)
- リポジトリ：https://github.com/Nittatta/dividend-app
- 公開URL：https://nittatta.github.io/dividend-app/
