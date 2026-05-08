# 高配当株ポートフォリオ管理 Web アプリ — CLAUDE.md
> このファイルは Claude Code がプロジェクトを理解するための指示書です。
> セッション開始時に必ず読み込み、すべての実装・修正・判断の基準としてください。

---

## 1. プロジェクト概要

| 項目 | 内容 |
|---|---|
| アプリ名 | 高配当株ポートフォリオ管理 Web アプリ |
| 形式 | **ブラウザ完結型（単一 HTML ファイル）— サーバー不要** |
| 主データソース | 楽天証券CSV（保有株）/ 楽天証券 配当金CSV / J-Quants API V2（株価・配当予想） |
| 管理対象 | 国内株式のみ（投資信託・ETF 除外） |
| データ保存 | LocalStorage（最大 60 ヶ月スナップショット） |
| 対応銘柄数 | 〜 100 銘柄（実績：約 90 銘柄） |
| 対応口座 | NISA 成長投資枠 / 特定口座（源泉徴収あり） |
| 対象ブラウザ | Chrome / Edge 最新版 |

---

## 2. ファイル構成

```
dividend-app/
├── CLAUDE.md          ← このファイル（Claude Code 指示書）
├── app.html           ← アプリ本体（単一ファイル・すべてのHTML/CSS/JSを内包）
└── requirements_dividend_app_v4.2.html  ← 要件定義書（参照用）
```

**重要：app.html は単一ファイルで完結させること。外部JSファイル・CSSファイルへの分割禁止。**

---

## 3. 技術スタック

- **HTML / CSS / Vanilla JavaScript のみ**（フレームワーク不使用）
- Chart.js（CDN）：グラフ描画
- Google Fonts（CDN）：BIZ UDPGothic / Roboto
- **Node.js・npm・ビルドツール一切不使用**

---

## 4. データソース仕様

### 4-1. 楽天証券 保有株CSV

| 項目 | 内容 |
|---|---|
| 文字コード | UTF-8 |
| 対象行フィルタ | 種別 = `"国内株式"` |
| 使用カラム | 種別 / 銘柄コード・ティッカー / 銘柄 / 口座 / 保有数量 / 平均取得価額 / 現在値 / 時価評価額[円] / 評価損益[円] / 評価損益[％] |
| 数値形式 | カンマ区切り・符号付き（`+1,640` 等）→ `parseFloat(str.replace(/[^0-9.-]/g, ''))` で処理 |
| 株価フォールバック | J-Quants API 失敗時に「現在値」列を代替使用 |

### 4-2. 楽天証券 配当金CSV

| 項目 | 内容 |
|---|---|
| 文字コード | **CP932（Shift-JIS）** ← UTF-8 ではない |
| 使用カラム | 入金日 / 口座 / 銘柄コード / 銘柄 / 数量[株/口] / 配当・分配金合計（税引前）/ 税額合計 / 受取金額 |
| 1株配当計算 | 配当合計（税引前） ÷ 数量[株/口] |
| 文字コード変換 | `TextDecoder('shift-jis')` で読み込む |

### 4-3. J-Quants API V2

| 項目 | 内容 |
|---|---|
| 認証方式 | V2：`x-api-key` ヘッダーに API キーを付与 |
| 株価エンドポイント | `GET https://api.jquants.com/v2/equities/bars/daily` |
| 株価取得タイミング | 1日1回（初回起動時のみ・キャッシュ制御） |
| 株価取得方式 | 日付指定で全銘柄一括取得 → ページング対応必須 |
| 配当予想エンドポイント | `GET https://api.jquants.com/v1/fins/statements` |
| 配当予想取得タイミング | 週1回 または CSVインポート時 |
| 配当予想取得方式 | 銘柄ごとに順次取得・3秒間隔・バックグラウンド実行 |
| ページング | `pagination_key` がレスポンスにある場合はループして全件取得 |
| Gzip | `Accept-Encoding: gzip` を省略し未圧縮で受信 |
| フォールバック（株価） | API 失敗時 → CSV の現在値列を使用 |
| フォールバック（配当予想） | API 失敗時 → 配当金CSV の直近実績を使用 |

---

## 5. データ構造（LocalStorage）

```javascript
// ストレージキー一覧
const STORAGE_KEYS = {
  SNAPSHOTS:      'hdp_snapshots',       // 月次スナップショット配列
  SETTINGS:       'hdp_settings',        // アプリ設定
  STOCK_MASTER:   'hdp_stock_master',    // 銘柄マスタ
  DIVIDEND_HIST:  'hdp_dividend_hist',   // 配当金実績履歴
  PRICE_CACHE:    'hdp_price_cache',     // 株価キャッシュ
  LAST_FETCH:     'hdp_last_fetch',      // 最終API取得日
};

// スナップショット1件の構造
const snapshot = {
  id:        'YYYY-MM',                  // 例: '2026-05'
  importedAt: '2026-05-07T10:00:00',     // インポート日時
  positions: [                           // 保有銘柄配列
    {
      code:          '7203',             // 銘柄コード（4桁）
      name:          'トヨタ自動車',
      account:       'NISA成長投資枠',   // または '特定口座'
      shares:        100,                // 保有株数
      avgCost:       2500.0,             // 平均取得単価
      currentPrice:  3200.0,             // 現在値（API or CSVから）
      priceSource:   'api',              // 'api' | 'csv' | 'none'
      evalAmount:    320000,             // 時価評価額
      evalGain:      70000,              // 評価損益[円]
      evalGainPct:   28.0,               // 評価損益[%]
    }
  ]
};

// 銘柄マスタ1件の構造
const stockMaster = {
  code:              '7203',
  name:              'トヨタ自動車',
  sector33:          '輸送用機器',
  sectorType:        'cyclical',         // 'cyclical' | 'defensive'
  dividendForecast:  95.0,               // 配当予想額（円/株・税引前）
  dividendSource:    'api',              // 'api' | 'csv' | 'manual'
  dividendManual:    false,              // 手動上書きフラグ
  payMonths:         [3, 9],             // 入金月（配当金CSVから自動取得）
  alertMode:         'B',                // 視点②モード 'A' | 'B' | 'C'
  alertThreshold:    4.0,                // 税引き後利回り目標(%)
  dropRateThreshold: 10.0,               // 下落率閾値(%)
};

// アプリ設定の構造
const settings = {
  jquantsApiKey:       '',               // J-Quants API キー
  lastDividendFetchDate: null,           // 配当予想最終取得日
  storageWarningShown: false,
};

// 株価キャッシュの構造
const priceCache = {
  date:   '2026-05-07',                  // 取得日
  prices: { '7203': 3200.0, ... }        // 銘柄コード → 株価
};

// 配当金実績1件の構造
const dividendHistory = {
  code:       '7203',
  name:       'トヨタ自動車',
  account:    'NISA成長投資枠',
  payDate:    '2026-03-25',              // 入金日
  shares:     100,
  grossAmt:   9500,                      // 税引前合計
  taxAmt:     0,                         // 税額（NISA=0）
  netAmt:     9500,                      // 受取金額
  perShare:   95.0,                      // 1株あたり配当（税引前）
};
```

---

## 6. 税率ロジック

```javascript
const TAX_COEF = {
  'NISA成長投資枠': 1.0,
  '特定口座':       0.79685,   // 源泉徴収あり (20.315%)
};

// 税引き後利回り（%）
const afterTaxYield = (dividendForecast * coef) / currentPrice * 100;

// 手取り配当合計（ポートフォリオ全体）
const totalAfterTax = positions.reduce((sum, p) => {
  const coef = TAX_COEF[p.account] ?? 0.79685;
  return sum + (master[p.code]?.dividendForecast ?? 0) * p.shares * coef;
}, 0);
```

---

## 7. 買い時アラート判定ロジック

```javascript
// 視点①：目標利回り超え
const isAlert1 = afterTaxYield >= master.alertThreshold;

// 視点②：モードA（利回り向上）
const isAlert2A = currentPrice <= avgCost;

// 視点②：モードB（下落率超え）
const dropRate = (avgCost - currentPrice) / avgCost * 100;
const isAlert2B = dropRate >= master.dropRateThreshold;

// 視点②：モードC（AかつB）
const isAlert2C = isAlert2A && isAlert2B;

// バッジ判定
const alert2 = { A: isAlert2A, B: isAlert2B, C: isAlert2C }[master.alertMode];
if (isAlert1 && alert2)      badge = 'double';   // 🔥 ダブル買い時
else if (isAlert1)           badge = 'target';   // 🟡 目標超え
else if (alert2)             badge = 'yield';    // 🟢 利回り向上
else                         badge = 'none';
```

---

## 8. APIアクセス制御

```javascript
// 株価取得：1日1回キャッシュ制御
const cache = JSON.parse(localStorage.getItem(STORAGE_KEYS.PRICE_CACHE) || 'null');
const today = new Date().toISOString().slice(0, 10);
if (!cache || cache.date !== today) {
  await fetchAllPrices();   // J-Quants API で一括取得
  // キャッシュ保存
} else {
  usePriceCache(cache.prices);
}

// 配当予想取得：週1回
const lastFetch = settings.lastDividendFetchDate;
const daysSince = lastFetch
  ? (Date.now() - new Date(lastFetch)) / 86400000
  : Infinity;
if (daysSince >= 7) {
  await fetchAllDividendForecasts();   // 3秒間隔・バックグラウンド
}

// ページング対応
async function fetchWithPaging(url, headers) {
  let results = [];
  let paginationKey = null;
  do {
    const sep = url.includes('?') ? '&' : '?';
    const fullUrl = paginationKey ? `${url}${sep}pagination_key=${paginationKey}` : url;
    const res = await fetch(fullUrl, { headers });
    const data = await res.json();
    results = results.concat(data.daily_quotes ?? data.statements ?? []);
    paginationKey = data.pagination_key ?? null;
  } while (paginationKey);
  return results;
}
```

---

## 9. エラー・空状態仕様

| ID | 種別 | 対応 |
|---|---|---|
| ERR-01 | API取得失敗 | 原因別メッセージ + 株価列に「取得失敗」（赤）+ CSVフォールバック |
| ERR-02 | 銘柄マスタ未設定 | 利回り・判定に「－」表示・スキップ・設定画面でハイライト |
| ERR-03 | ストレージ上限（80%超） | 警告バナー + 古いスナップショットから自動削除 |
| ERR-04 | CSVインポート失敗 | 行・列・原因の詳細表形式 + 部分インポート or 全件キャンセル選択 |

---

## 10. UIデザイン仕様

```css
/* カラー変数（必ずこの値を使うこと） */
--bg:           #0d0d1a;   /* メイン背景 */
--bg2:          #13132a;   /* カード背景 */
--bg3:          #1a1a35;   /* 入力欄・コードブロック */
--border:       #2a2a4a;   /* ボーダー */
--gold:         #c8a96e;   /* アクセント・見出し */
--gold-dim:     #8a6e3e;   /* 淡いゴールド */
--green:        #2e6b4f;   /* ポジティブ */
--green-light:  #3d9668;
--blue:         #4a7fd4;   /* インフォ */
--blue-light:   #6a9fe4;
--text:         #e8e8f0;   /* メインテキスト */
--text-muted:   #8888aa;   /* サブテキスト */
--red:          #e05555;   /* エラー・マイナス */
--yellow:       #f0c040;   /* 警告 */
```

```
フォント：
  日本語テキスト → BIZ UDPGothic
  数値・英字・コード → Roboto / Roboto Mono

レイアウト：
  ナビゲーション → 上部固定ダークナビバー（5画面切り替え）
  PC優先（1200px 基準）・タブレット横向き対応
  ダッシュボード重視：KPIカード → アラートバナー → 保有株一覧 → カレンダー + セクタードーナツ → 簡易グラフ
```

---

## 11. 画面構成（5画面）

| # | 画面ID | 画面名 | 主なコンポーネント |
|---|---|---|---|
| 1 | `dashboard` | ダッシュボード | KPI×4 / アラートバナー上位5件 / 保有株一覧テーブル / 配当カレンダー / セクタードーナツ / 簡易推移グラフ |
| 2 | `charts` | 推移グラフ | G-1〜G-4（保有株数・評価額・配当累積・利回り）月次/年次切替・銘柄フィルター |
| 3 | `dividend` | 配当サマリー | 年間配当（税引前・後）/ 月次予想 / 予想vs実績 / 銘柄別明細 |
| 4 | `settings` | 設定 | セットアップ状況（F-OB）/ APIキー / 銘柄別設定テーブル |
| 5 | `import` | CSVインポート | 2ファイルD&D / 月選択 / インポート履歴 / ERR-04詳細 / JSONバックアップ |

---

## 12. オンボーディング（設定画面・F-OB）

設定画面の最上部に「セットアップ状況」セクションを表示。

| Step | 内容 | 完了条件 |
|---|---|---|
| 1 | J-Quants API キー登録 | `settings.jquantsApiKey` が空でない |
| 2 | 保有株CSV インポート | スナップショットが1件以上ある |
| 3 | 配当予想額の確認 | 全保有銘柄の `dividendForecast > 0` |
| 4 | 利回り目標の設定 | 1件以上の銘柄で `alertThreshold > 0` |

全完了 → セクションを折りたたみ。問題発生時に自動再表示。

---

## 13. 開発フェーズとSTEP

### Phase 1（MVP）— 現在実装対象

| STEP | 内容 | 依存 |
|---|---|---|
| 1 | データ構造定義・LocalStorage ユーティリティ | なし |
| 2 | HTML骨格・CSS変数・ナビゲーション（5画面切替） | STEP 1 |
| 3 | CSVインポート（保有株CSV + 配当金CSV + ERR-04） | STEP 1, 2 |
| 4 | 株価自動取得（J-Quants V2・キャッシュ・フォールバック） | STEP 3 |
| 5 | 配当予想取得（J-Quants /fins/statements・週1回・バックグラウンド） | STEP 3 |
| 6 | 保有株サマリー画面（F-02・税引き後利回り・ソート・ERR-02） | STEP 4, 5 |
| 7 | 買い時アラート（F-03/04・2視点・バッジ3種・バナー上位5件） | STEP 6 |
| 8 | 統合テスト・F-OB・ERR-01/03 | 全STEP |

### Phase 2（グラフ・分析）
F-05 推移グラフ / F-06 配当金サマリー / F-07 セクター分散

### Phase 3（拡張）
F-08 データエクスポート / 予想vs実績比較強化 / J-Quants 有料移行対応

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

## 15. 実装時の注意事項

1. **CP932 読み込み**：配当金CSVは `FileReader` ではなく `ArrayBuffer + TextDecoder('shift-jis')` で読む
2. **ページング**：J-Quants API は `pagination_key` が消えるまでループすること
3. **口座合算**：同一銘柄コードが複数口座にある場合は株数を合算してポートフォリオを構成
4. **フォールバック優先順位**：株価は `api > csv > none`、配当予想は `api > csv > manual`
5. **LocalStorage 使用量**：`navigator.storage.estimate()` で監視。80% 超で ERR-03 を発動
6. **CORS 検証**：STEP 4 実装前に無料プランの API キーでブラウザからの直接アクセス可否を検証
7. **手動上書きフラグ**：`dividendManual: true` の銘柄は自動取得で上書きしない
8. **未設定銘柄**：`dividendForecast === 0` または `null` の場合は利回り「－」・判定スキップ

---

*要件定義書の完全版は `requirements_dividend_app_v4.2.html` を参照。*
*このファイルは仕様変更のたびに更新すること。*
