# 高配当株ポートフォリオ管理 Web アプリ — CLAUDE.md
> このファイルは Claude Code がプロジェクトを理解するための指示書です。
> セッション開始時に必ず読み込み、すべての実装・修正・判断の基準としてください。
> 最終更新：2026-05-12 / Session 8 完了時点

---

## 1. プロジェクト概要

| 項目 | 内容 |
|---|---|
| アプリ名 | 高配当株ポートフォリオ管理 Web アプリ（高配当PF） |
| アプリURL | https://nittatta.github.io/dividend-app/ |
| 形式 | **ブラウザ完結型（単一 HTML ファイル）— サーバー不要** |
| 主データソース | 楽天証券CSV（保有株）/ 楽天証券 配当金CSV / J-Quants API V2（株価・配当予想） |
| 管理対象 | 国内株式のみ（投資信託・ETF 除外） |
| データ保存 | LocalStorage（最大 60 ヶ月スナップショット） |
| 対応銘柄数 | 〜100 銘柄（実績：約 90 銘柄） |
| 対応口座 | NISA 成長投資枠 / 特定口座（源泉徴収あり） |
| 対象ブラウザ | Chrome / Edge 最新版 / PWA（iOS Safari・Android Chrome） |

---

## 2. ファイル構成

```
C:\Users\drums\dividend-app\
├── CLAUDE.md          ← このファイル（Claude Code 指示書）
├── app.html           ← 開発用ソース（編集はここ）
├── index.html         ← GitHub Pages 配信用（minify.ps1 で生成）
├── minify.ps1         ← app.html → index.html の minify 生成スクリプト
├── sw.js              ← Service Worker（キャッシュ管理）
├── manifest.json      ← PWA設定
└── icons/
    ├── icon-192.png
    └── icon-512.png
```

**重要ルール：**
- `app.html` は単一ファイルで完結させること。外部JS・CSSファイルへの分割禁止。
- 編集は必ず `app.html` に行うこと。`index.html` は直接編集しない。
- `git push` 前に必ず `minify.ps1` を実行して `index.html` を更新すること。

---

## 3. git push 手順

```powershell
cd C:\Users\drums\dividend-app
powershell -ExecutionPolicy Bypass -File minify.ps1
git add -A
git commit -m "変更内容の説明"
git push origin main
```

**注意事項：**
- Claude Code から git は直接実行不可。PowerShell から手動実行すること。
- PWAキャッシュ更新が必要な場合は `sw.js` の `CACHE_NAME` を +1 すること（現在 **v16**）。

---

## 4. 技術スタック

- **HTML / CSS / Vanilla JavaScript のみ**（フレームワーク不使用）
- Chart.js（CDN）：グラフ描画
- Google Fonts（CDN）：BIZ UDPGothic / Roboto
- **Node.js・npm・ビルドツール一切不使用**

---

## 5. インフラ構成

| 役割 | サービス | 詳細 |
|---|---|---|
| ホスティング | GitHub Pages | `nittatta.github.io/dividend-app/` |
| API プロキシ | Cloudflare Workers | `jquants-proxy.drumsbaka.workers.dev` |
| 株価・配当API | J-Quants API V2 | Light プラン（60リクエスト/分） |
| PWA | Service Worker | sw.js v16・Network First 戦略 |

**Cloudflare Workers の役割：**
J-Quants API の CORS ブロックを回避するプロキシ。APIキーは Workers の Secret（`JQUANTS_API_KEY`）に保存。`index.html` のソースには含まれないため GitHub Public リポジトリで安全。

---

## 6. 画面構成（7タブ・並び替え可能）

| 画面ID | 画面名 | 主なコンポーネント |
|---|---|---|
| `dashboard` | ダッシュボード | KPI・月次配当バーグラフ・セクター横棒・アラートバナー・保有株一覧 |
| `holdings` | 保有株一覧 | 全銘柄テーブル（PC12列／スマホ4列）・行タップで詳細カードモーダル |
| `charts` | 推移グラフ | G-1〜G-4（評価額・配当累積・利回り・銘柄数）月次/年次切替 |
| `dividend` | 配当サマリー | 年間KPI・月次バーチャート・銘柄別明細・予想vs実績 |
| `sector` | セクター分散 | 3モード・集中警告・セクター一括取得ボタン |
| `evaluation` | 銘柄評価 | スコアランキングテーブル・詳細モーダル（レーダーチャート） |
| `settings` | 設定 | APIキー・CSVインポート・銘柄別設定テーブル・データ管理 |

**タブ並び替え：** PCはドラッグ＆ドロップ（`hdp_nav_order`）、スマホは長押し+ドラッグ（`hdp_bnav_order`）で独立保存。

---

## 7. LocalStorage キー一覧

| キー | 内容 |
|---|---|
| `hdp_snapshots` | 月次保有スナップショット |
| `hdp_settings` | APIキー・アプリ設定 |
| `hdp_stock_master` | 銘柄マスタ（配当予想・sector33・payMonths等） |
| `hdp_dividend_hist` | 配当金実績履歴 |
| `hdp_price_cache` | 株価キャッシュ |
| `hdp_last_fetch` | 最終株価取得日時 |
| `hdp_eval_master` | 銘柄評価スコア（F-EV） |
| `hdp_nav_order` | PC版ナビ並び順 |
| `hdp_bnav_order` | スマホ版ナビ並び順（PCと独立） |

---

## 8. データソース仕様

### 8-1. 楽天証券 保有株CSV

| 項目 | 内容 |
|---|---|
| 文字コード | UTF-8 |
| 対象行フィルタ | 種別 = `"国内株式"` |
| 使用カラム | 種別 / 銘柄コード・ティッカー / 銘柄 / 口座 / 保有数量 / 平均取得価額 / 現在値 / 時価評価額[円] / 評価損益[円] / 評価損益[％] |
| 数値形式 | カンマ区切り・符号付き（`+1,640` 等）→ `parseFloat(str.replace(/[^0-9.-]/g, ''))` で処理 |
| 株価フォールバック | J-Quants API 失敗時に「現在値」列を代替使用 |

### 8-2. 楽天証券 配当金CSV

| 項目 | 内容 |
|---|---|
| 文字コード | **CP932（Shift-JIS）← UTF-8 ではない** |
| 使用カラム | 入金日 / 口座 / 銘柄コード / 銘柄 / 数量[株/口] / 配当・分配金合計（税引前）/ 税額合計 / 受取金額 |
| 1株配当計算 | 配当合計（税引前） ÷ 数量[株/口] |
| 文字コード変換 | `TextDecoder('shift-jis')` で読み込む |

### 8-3. J-Quants API V2

| エンドポイント | 用途 | 備考 |
|---|---|---|
| `/v2/equities/bars/daily` | 株価取得 | 1日1回・キャッシュ制御 |
| `/v2/fins/summary` | 財務サマリー | 配当予想・評価スコア計算に使用 |
| `/v2/equities/master` | 銘柄名・セクター | コード省略で全上場銘柄一括取得可 |

**実証済みフィールド名（fins/summary）：**

| 意味 | フィールド名 | 備考 |
|---|---|---|
| 売上高 | `Sales` | NetSales ではない |
| 営業利益 | `OP` | OperatingProfit ではない |
| EPS | `EPS` | BasicEarningsPerShare ではない |
| 自己資本比率 | `EqAR` | 0〜1の小数（0.498 = 49.8%） |
| 配当性向 | `PayoutRatioAnn` | 0〜1の小数 |
| 現金・同等物 | `CashEq` | CashAndDeposits ではない |
| 配当金/株 | `DivAnn` / `FDivAnn` / `NxFDivAnn` | 実績/今期予想/来期予想 |
| 決算期末日 | `CurFYEn` | FiscalYear フィールドは存在しない |
| 四半期/通期区別 | `CurPerType` | "FY"=通期・"1Q"等=四半期 |

**equities/master フィールド名：**

| 意味 | フィールド名 |
|---|---|
| 銘柄名（日本語） | `CoName` |
| 33業種セクター名 | `S33Nm` |
| 銘柄コード（5桁） | `Code` |

**レートリミット：** Lightプラン 60リクエスト/分。429発生時は自動リトライ（10秒→30秒→60秒）。

---

## 9. 税率ロジック

```javascript
const TAX_COEF = {
  'NISA成長投資枠': 1.0,
  '特定口座':       0.79685,   // 源泉徴収あり (20.315%)
};

// 税引き後利回り（%）
const afterTaxYield = (dividendForecast * coef) / currentPrice * 100;
```

---

## 10. 買い時アラート判定ロジック

```javascript
// 視点①：目標利回り超え
const isAlert1 = afterTaxYield >= master.alertThreshold;

// 視点②：モードA（現在値 ≤ 平均取得単価）
const isAlert2A = currentPrice <= avgCost;

// 視点②：モードB（下落率 ≥ 閾値）
const dropRate = (avgCost - currentPrice) / avgCost * 100;
const isAlert2B = dropRate >= master.dropRateThreshold;

// 視点②：モードC（AかつB）
const isAlert2C = isAlert2A && isAlert2B;

// バッジ判定
if (isAlert1 && alert2)  badge = 'double';  // 🔥 ダブル買い時
else if (isAlert1)       badge = 'target';  // 🟡 目標超え
else if (alert2)         badge = 'yield';   // 🟢 利回り向上
else                     badge = 'none';
```

---

## 11. F-EV 銘柄評価システム

### スコア構成（最大26点）

| # | 項目 | 取得方法 | 満点 |
|---|---|---|---|
| 1 | 売上高トレンド | `fins/summary` `Sales` 自動 | 3点 |
| 2 | EPS成長 | `fins/summary` `EPS` 自動 | 3点 |
| 3 | 営業利益率 | `fins/summary` `OP/Sales` 自動 | 3点 |
| 4 | 自己資本比率 | `fins/summary` `EqAR` 自動 | 3点 |
| 5 | 現金・預金トレンド | `fins/summary` `CashEq` 自動 | 3点 |
| 6 | 配当金/株トレンド | `fins/summary` `DivAnn` 自動 | 3点 |
| 7 | 配当性向 | `fins/summary` `PayoutRatioAnn` 自動 | 3点 |
| 8 | 営業CF評価 | 手動入力（3/2/1点） | 3点 |
| 9 | コロナ禍減配なし | 手動ボーナス | 1点 |
| 10 | 株主還元意識 | 手動ボーナス | 1点 |

### データ保存先
`hdp_eval_master`（`hdp_stock_master` とは独立したキー）

---

## 12. スマホUI仕様

### 下部ナビゲーション（max-width: 600px）

| 項目 | 仕様 |
|---|---|
| 構造 | `#bottom-nav` → `#bnav-scroll`（横スクロール）→ `.bnav-item`（動的生成） |
| 高さ固定 | `--bottom-nav-h: 64px` + `env(safe-area-inset-bottom)` |
| 並び替え | 長押し（500ms）→ 並び替えモード → ドラッグで移動 → `hdp_bnav_order` に保存 |

### 保有株一覧スマホ表示（4列）

| 列 | PC | スマホ |
|---|---|---|
| コード/銘柄 | ✅ | ✅（45%幅） |
| 時価評価額 | ✅ | ✅（22%幅） |
| 税引後利回り | ✅ | ✅（18%幅） |
| アラート | ✅ | ✅（15%幅・絵文字のみ） |
| その他8列 | ✅ | ❌ `.pc-only` で非表示 |

**詳細カードモーダル：** 行タップで下からスライドアップ。10項目を2列グリッドで表示。

### PWA

| 項目 | 内容 |
|---|---|
| インストール方法 | iOS: Safari → 共有 → ホーム画面に追加 / Android: Chrome → メニュー → ホーム画面に追加 |
| LocalStorage | PWAとブラウザは独立。設定タブの「📤 エクスポート」→「📥 インポート」でデータ移行 |
| sw.js 戦略 | Network First。`workers.dev` へのリクエストはキャッシュ除外 |

---

## 13. 残課題

| # | 内容 | 優先度 |
|---|---|---|
| BUG-04 | スマホブラウザ版・保有株一覧タブの横幅縮小問題（PWAでは正常） | 中（後回し） |
| Phase 3 | F-08 データエクスポート強化 | 低 |
| Phase 3 | 予想vs実績比較強化 | 低 |

---

## 14. コーディング規約

- **コメント**：日本語で記述（変数名・関数名は英語）
- **関数名**：camelCase。例：`fetchAllPrices()` / `parseHoldingsCsv()` / `calcAfterTaxYield()`
- **エラーハンドリング**：すべての async 関数に try-catch を付ける
- **DOM 操作**：`getElementById` / `querySelector` を使用。jQuery 禁止
- **モジュール分割禁止**：`<script>` タグ内にすべて記述
- **LocalStorage**：直接アクセス禁止。必ず `getStorage()` / `setStorage()` ユーティリティを経由
- **数値パース**：`parseFloat(str.replace(/[^0-9.-]/g, ''))` を共通関数化

---

## 15. 環境情報

| 項目 | 内容 |
|---|---|
| OS | Windows 11 |
| ターミナル | PowerShell |
| GitHub アカウント | Nittatta |
| Cloudflare アカウント | Drumsbaka@gmail.com |
| J-Quants プラン | Light |
| sw.js キャッシュバージョン | **v16**（更新時は +1 すること） |

---

*Session 8 完了時点の内容をすべて含んでいます。*
*このファイルを読み込むことで Claude Code が完全に文脈を引き継げます。*
