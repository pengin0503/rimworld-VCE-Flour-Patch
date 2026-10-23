# VCE Flour Alternatives — 設計仕様 (2026-10-23)

## 目的
RimWorld 1.6/1.7 + Vanilla Cooking Expanded (VCE) において、小麦粉と等価な米粉・トウモロコシ粉を追加する。専用料理は作らない。

## 固定要件
- 既存小麦粉はそのまま維持。
- 米10 → 米粉10、トウモロコシ10 → トウモロコシ粉10。かまどと電気コンロで加工。
- 提供された透過 PNG を使用（双方 128×128）。
- VCEの小麦粉レシピの粉枠は3種から選択可能。
- 1回の製造の粉は同一種類に限定（2スタック以上は可）。肉・野菜など別スロットは従来どおり混合可能。
- 1.6/1.7を想定。既存セーブへの途中追加を考慮。元MODファイルは改変しない。

## 実装方針
XMLでThingDef/RecipeDefとVCE既存焼き料理のingredientsおよび共通抽象親のfixed/default ingredient filtersをパッチする。XMLのみでは単一スロットの粉種の統一と他材料の混合を両立できないため、Harmonyの限定PrefixでVCEのミックス許容レシピの可用候補を、条件を満たす単一粉種に限定してから元の選択処理に渡す。粉がどの1種でも不足なら製造不可とする。ゲーム DLL に直接リンクした C# ビルドは両バージョン別に行う。

## 参照
- VCE: https://github.com/Vanilla-Expanded/VanillaCookingExpanded/blob/main/1.6/Defs/RecipeDefs/Recipes_Bakes.xml
- VCE 1.7 loadFolders は 1.6 defsを使用: https://github.com/Vanilla-Expanded/VanillaCookingExpanded/blob/main/loadFolders.xml

## 検証制限
この実行環境にはRimWorld本体・.NET SDK・Harmony参照 DLL がないため、XML等の静的試験は可能だがC#コンパイルとゲーム内実機試験は別途必要。

## 引き継ぎ時の改訂（2026-10-23）

ユーザーの確定仕様を維持し、実装方式だけを変更した。
- 抽象親は触らず、7つの具体的レシピにfixed/defaultフィルターを追加して元の親設定を継承する。
- 手動の必要量予測を廃止し、1種類ずつの候補コピーで元のAllowMix判定を最大3回実行する。
- 両版の私有APIシグネチャを参照DLLのメタデータで確認する。
- Harmony初期化失敗時は追加粉を対象料理から無効化する。
- ビルド・自動試験は可能になった。実ゲーム試験は依然未実施のため完成版公開を保留する。
詳細計画：../plans/2026-10-23-handoff.md。
