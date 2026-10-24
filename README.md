# VCE Flour Alternatives / VCE小麦粉代替パッチ

Vanilla Cooking Expanded（VCE）の既存の焼き料理に、**米粉とトウモロコシ粉を小麦粉の代替として追加**するMODです。専用料理や製粉設備は追加しません。

**状態：開発版。1.6/1.7用DLLのビルドと自動テストは実施済みですが、RimWorld実ゲームでの検証は未完了です。[初回ベータ版 v0.1.0-beta.1](https://github.com/pengin0503/rimworld-VCE-Flour-Patch/releases/tag/v0.1.0-beta.1)をプレリリースとして配布します。**

## 機能

- 米10個 → 米粉10個、トウモロコシ10個 → トウモロコシ粉10個。
- バニラのかまど・電気コンロで製造。作業量は400。
- VCE標準の焼き料理7レシピで、小麦粉・米粉・トウモロコシ粉から1種類を使用。
- 小麦粉5＋米粉5など、**異なる粉の合算は禁止**。同じ種類の複数スタックは合算可能。
- 肉・野菜などの混合は元のレシピどおり。美食焼き料理の元々の混合禁止も維持。
- 新しい粉の栄養価は小麦粉と同じ0.05。既存の小麦粉・料理の性能、材料費は変更しません。
- 提供された128×128透過PNGをそのまま使用。日本語表示に対応。

| 対象レシピ | 製品数 | 粉の必要個数（標準の栄養価） |
|---|---:|---:|
| VCE_CookBakeSimple | 2 | 20 |
| VCE_CookBakeSimpleBulk | 8 | 80 |
| VCE_CookBakeFine | 2 | 10 |
| VCE_CookBakeFineBulk | 8 | 40 |
| VCE_CookBakeLavish | 2 | 20 |
| VCE_CookBakeLavishBulk | 8 | 80 |
| VCE_CookBakeGourmet | 2 | 20 |

VCE Bakeryなど別MODのレシピは対象外です。

## 必要MOD・対応バージョン

RimWorld **1.6 / 1.7を対象**に個別ビルドします。実機での対応確認は保留中です。

必要なものはHarmony、Vanilla Expanded Framework、Vanilla Cooking Expandedです。HarmonyはCoreより上、Framework→VCE→当MODの順に配置してください。

| ビルド対象 | 固定した参照アセンブリ | .NET | コンパイル時Harmony |
|---|---|---|---|
| 1.6 | Krafs.Rimworld.Ref 1.6.4871 | .NET Framework 4.7.2 | 2.4.3 |
| 1.7 | Krafs.Rimworld.Ref 1.7.4985-unstable | .NET 10 | 3.0.0-rc.3 |

参照アセンブリはAPIの宣言だけを持つビルド用パッケージです。ゲーム本体の動作試験には使えません。1.7参照はunstable版であり、別ビルドとの実動作互換性を保証するものではありません。

## 入手・インストール

[ベータ版 v0.1.0-beta.1](https://github.com/pengin0503/rimworld-VCE-Flour-Patch/releases/tag/v0.1.0-beta.1) のAssetsから `VCE-Flour-Alternatives-0.1.0-beta.1.zip` を入手できます。実ゲーム未検証のプレリリースであり、完成版ではありません。

開発版を検証する場合は、[Actions](https://github.com/pengin0503/rimworld-VCE-Flour-Patch/actions/workflows/verify.yml)の成功した実行から `VCE-Flour-Alternatives-development` artifactを取得してください。中の `VCE-Flour-Alternatives-dev.zip` がMOD本体、`build-manifest.json` がビルド情報です。GitHubへのログインが必要な場合があります。

1. セーブをバックアップし、検証用コピーを用意します。
2. MOD本体ZIPを展開し、`VCE-Flour-Alternatives` フォルダをRimWorldの `Mods` に置きます。
3. `About/About.xml` と、`1.6/Assemblies`・`1.7/Assemblies` のDLLを確認します。フォルダの二重入れ子に注意してください。
4. 必要MODと当MODを有効にし、再起動します。
5. かまどまたは電気コンロで製粉指示を追加します。
6. 焼き料理の材料設定で使用する粉を許可します。既存の製造指示は保存済みフィルターを保持するため、追加の粉を手動で許可する必要がある場合があります。

GitHubの「Download ZIP」や自動生成される「Source code」は開発用ソースで、DLLを含みません。ゲームにそのまま導入しないでください。

## ビルド・テスト

.NET 10 SDK、Python 3.12以上を用意してください。通常のビルドではゲーム本体DLLは不要です。

```sh
python -m pip install -r requirements-test.txt
python tools/verify.py
```

静的テスト、C#選択ポリシー、実Harmony 3を使った境界テスト、両バージョンのDLLビルド、私有APIのシグネチャ検査、配布ZIP生成を順に実行します。生成先は `artifacts/`。`DOTNET` 環境変数でSDK実行ファイルを指定できます。

WindowsでDLLのみをビルド・配置する場合：

```powershell
./build.ps1
# 自分の正規インストールのDLLで確認する場合は各版を個別指定
./build.ps1 -Version 1.6 -RimWorldManagedDir 'C:\path\to\Managed' -HarmonyAssemblyPath 'C:\path\to\Harmony\0Harmony.dll'
```

1.7ではその版のゲームアセンブリがあるディレクトリとHarmony 3を指定してください。ゲームDLL・Harmony DLLはリポジトリにもMOD配布ZIPにも同梱しません。`tools/package_mod.py` 単体は簡易的なファイル検査だけなので、配布前には必ず `tools/verify.py` 全体を通してください。

## 実装と制限

XMLは7つの具体的なレシピにだけ材料候補を追加し、共通の抽象親は変更しません。子レシピのfixed/defaultフィルターは元の親フィルターを継承します。

C#は `WorkGiver_DoBill.TryFindBestBillIngredientsInSet_AllowMix` の正確な5引数シグネチャを確認してパッチします。粉を1種類ずつに絞ったリストのコピーを渡し、最大3回、元の材料選択処理を呼びます。必要量・材料許可・粉以外の選択はゲーム側の判定を使い、`allowMixingIngredients` は変更しません。再帰ガードと例外時の復元、失敗時の一時選択破棄を実装しています。

パッチ初期化に失敗した場合はエラーを記録し、対象料理で追加2種類の粉を使えなくします。小麦粉と他材料は残します。DLL自体を入れ忘れるとこの保護も動かないため、配布生成時は両版のDLLを必須としています。

他MODのパッチも元メソッド呼び出し時に実行されますが、すべての調理MODとの互換性を保証するものではありません。対象メソッドを置換・中止するMODや、当MODを継承元にするレシピ追加MODは個別検証が必要です。

## 検証状況

- Python：15件成功（XML、実VCE定義との比較、画像、ロード構成、梱包）。
- C#選択ポリシー：15件成功。
- 実Harmony 3＋ゲームAPI形状の代替環境：9シナリオ成功。
- 1.6/1.7：警告・エラーなしでDLLビルド、APIシグネチャ／対象ランタイム検査成功。
- **未検証**：ゲーム起動とXML継承解決、実際の材料探索・運搬・消費、製粉・料理、Harmony 2のゲーム内適用、VEFとの実動作、既存セーブ追加。

詳細とリリース前の必須手順は [検証記録](docs/VALIDATION.md)、判断理由は [作業記録](docs/WORK_LOG.md) にあります。

## 参照

- [Vanilla Cooking Expanded](https://github.com/Vanilla-Expanded/VanillaCookingExpanded)
- [Vanilla Expanded Framework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework)
- [RimRef](https://github.com/krafs/RimRef)
- [Harmony for RimWorld](https://github.com/pardeike/HarmonyRimWorld)

非公式の互換アドオンです。VCE本体のファイルは改変しません。
