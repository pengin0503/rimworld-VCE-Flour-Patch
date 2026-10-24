# VCE Flour Alternatives / VCE小麦粉代替パッチ

Vanilla Cooking Expanded（VCE）の既存の焼き料理に、**米粉とトウモロコシ粉を小麦粉の代替として追加**するMODです。専用料理や製粉設備は追加しません。

**状態：正式版 v0.1.0。ユーザーからゲーム内検証の成功報告を受け、正式版として公開します。1.6/1.7のビルドと自動テストも確認済みです。**

## 機能

- 米粉・トウモロコシ粉の製粉指示は各2種類：2倍量は原料20個→粉20個・工数10（workAmount 600）、8倍量は原料80個→粉80個・工数40（workAmount 2400）。
- バニラのかまど・電気コンロで製造。原料・完成品・工数を同じ倍率で増やすため、1個当たりのコストは同じです。
- 既存の製粉指示は2倍量へ引き継ぎます。8倍量を使う場合は新しい指示を追加してください。
- VCE標準の焼き料理7レシピで、小麦粉・米粉・トウモロコシ粉から1種類を使用。
- 小麦粉5＋米粉5など、**異なる粉の合算は禁止**。同じ種類の複数スタックは合算可能。
- 肉・野菜などの混合は元のレシピどおり。美食焼き料理の元々の混合禁止も維持。
- 新しい粉の栄養価は小麦粉と同じ0.05。既存の小麦粉・料理の性能、材料費は変更しません。
- 小麦粉・米粉・トウモロコシ粉を「穀物粉」（英語：grain flour）カテゴリーにまとめ、対象料理の粉枠もこのカテゴリーを参照します。別の粉はこのカテゴリーへ登録すれば対象料理で使えます。
- 提供された128×128透過PNGをそのまま使用（ファイル名は `VCE_FPatch_` 接頭辞）。日本語表示に対応。

| 対象レシピ | 製品数 | 粉の必要個数（標準の栄養価） |
|---|---:|---:|
| VCE_CookBakeSimple | 2 | 20 |
| VCE_CookBakeSimpleBulk | 8 | 80 |
| VCE_CookBakeFine | 2 | 10 |
| VCE_CookBakeFineBulk | 8 | 40 |
| VCE_CookBakeLavish | 2 | 20 |
| VCE_CookBakeLavishBulk | 8 | 80 |
| VCE_CookBakeGourmet | 2 | 20 |

VCE Bakeryなど別MODのレシピは対象外です。小麦粉を使う全レシピへ自動適用する実装ではありません。

## 必要MOD・対応バージョン

RimWorld **1.6 / 1.7を対象**に個別ビルドします。ユーザーの1.7環境でゲーム内検証の成功報告を受領しています。1.6の実機確認は個別の報告を受けていません。

必要なものはHarmony、Vanilla Expanded Framework、Vanilla Cooking Expandedです。HarmonyはCoreより上、Framework→VCE→当MODの順に配置してください。

| ビルド対象 | 固定した参照アセンブリ | .NET | コンパイル時Harmony |
|---|---|---|---|
| 1.6 | Krafs.Rimworld.Ref 1.6.4871 | .NET Framework 4.7.2 | 2.4.3 |
| 1.7 | Krafs.Rimworld.Ref 1.7.4977-alpha.1（Assembly-CSharp 1.7.9782） | .NET 10 | 3.0.0-rc.3 |

参照アセンブリはAPIの宣言だけを持つビルド用パッケージです。ゲーム本体の動作試験には使えません。

1.7のDLLは、参照したゲームアセンブリより**古い**ゲームビルドでは読み込めません（.NETは要求より低いバージョンを結び付けないため）。そのため1.7は公開されている最古の参照（1.7.9782）でビルドし、1.7.9782以降のビルドで読み込めるようにしています。1.7.9787.38423では読み込みと下記のゲーム実処理テストを確認済みです。

## 入手・インストール

[正式版 v0.1.0](https://github.com/pengin0503/rimworld-VCE-Flour-Patch/releases/tag/v0.1.0) のAssetsから `VCE-Flour-Alternatives-0.1.0.zip` を入手できます。

**注意**：v0.1.0-beta.1の1.7用DLLは新しい参照（Assembly-CSharp 1.7.9790）でビルドしたため、それより前の1.7ビルドでは読み込めない可能性が高いです。読み込めないと粉の合算防止が働きません。修正を含む正式版 v0.1.0を使ってください。

開発版を検証する場合は、[Actions](https://github.com/pengin0503/rimworld-VCE-Flour-Patch/actions/workflows/verify.yml)の成功した実行から `VCE-Flour-Alternatives-development` artifactを取得してください。中の `VCE-Flour-Alternatives-dev.zip` がMOD本体、`build-manifest.json` がビルド情報です。GitHubへのログインが必要な場合があります。

1. セーブをバックアップし、検証用コピーを用意します。
2. MOD本体ZIPを展開し、`VCE-Flour-Alternatives` フォルダをRimWorldの `Mods` に置きます。
3. `About/About.xml` と、`1.6/Assemblies`・`1.7/Assemblies` のDLLを確認します。フォルダの二重入れ子に注意してください。
4. 必要MODと当MODを有効にし、再起動します。
5. かまどまたは電気コンロで製粉指示を追加します。
6. 焼き料理の材料設定で使用する粉を許可します。
   - **当MODの導入前からある製造指示**は、セーブ済みの材料設定を使うため、米粉・トウモロコシ粉が未許可です。使う場合は各指示で手動で許可してください。導入後に作った指示では最初から許可されています。
   - 導入前は小麦粉が「固定材料」だったため、材料設定で小麦粉のチェックを外しても効果がありませんでした。導入後はこのチェックが有効になります。過去に小麦粉のチェックを外していた指示は小麦粉を使わなくなるので、確認してください。

GitHubの「Download ZIP」や自動生成される「Source code」は開発用ソースで、DLLを含みません。ゲームにそのまま導入しないでください。

## ビルド・テスト

.NET 10 SDK、Python 3.12以上を用意してください。通常のビルドではゲーム本体DLLは不要です。

```sh
python -m pip install -r requirements-test.txt
python tools/verify.py
```

静的テスト、C#選択ポリシー、実Harmony 3を使った境界テスト、両バージョンのDLLビルド、私有APIのシグネチャと参照バージョンの検査、配布ZIP生成を順に実行します。生成先は `artifacts/`。`DOTNET` 環境変数でSDK実行ファイルを指定できます。

### 自分のRimWorld 1.7で確認する（任意・ローカル専用）

自分の正規インストールの1.7アセンブリがあれば、ゲームの実際の材料選択処理・XMLパッチ適用・XML継承解決を、ビルドしたDLLで動かす検査も実行できます。CIにはゲームDLLがないため、環境変数を指定しない場合は省略されます。

```sh
RIMWORLD_17_MANAGED_DIR=/path/to/RimWorld/1.7/Managed \
RIMWORLD_17_HARMONY=/path/to/Harmony/1.7/Assemblies/0Harmony.dll \
python tools/verify.py
```

`RIMWORLD_17_HARMONY` を省略すると `RIMWORLD_17_MANAGED_DIR/0Harmony.dll` を使います。この検査は栄養値の計算を固定値に置き換え、エンジン依存のログ・プロファイラなどを止めて実行するもので、ゲーム内試験の代わりにはなりません（運搬・消費・セーブ・UIは対象外）。1.6はMono上で動くため対象外です。

WindowsでDLLのみをビルド・配置する場合：

```powershell
./build.ps1
# 自分の正規インストールのDLLで確認する場合は各版を個別指定
./build.ps1 -Version 1.6 -RimWorldManagedDir 'C:\path\to\Managed' -HarmonyAssemblyPath 'C:\path\to\Harmony\0Harmony.dll'
```

1.7ではその版のゲームアセンブリがあるディレクトリとHarmony 3を指定してください。ゲームDLL・Harmony DLLはリポジトリにもMOD配布ZIPにも同梱しません。`tools/package_mod.py` 単体は簡易的なファイル検査だけなので、配布前には必ず `tools/verify.py` 全体を通してください。

## 実装と制限

XMLは7つの具体的なレシピにだけ穀物粉カテゴリーを追加し、共通の抽象親は変更しません。穀物粉カテゴリー `PFA_GrainFlour` は `PlantMatter` の子で、小麦粉もこの子カテゴリーへ移します（ほかのカテゴリー所属は保持）。子レシピのfixed/defaultフィルターは元の親フィルターを継承します。粉枠には表示名 `customSummary`（grain flour／穀物粉）を付けます。粉枠が複数の材料を許可するとゲームが汎用の「原料」と表示するためです。

初期化時に、対象7レシピの `fixedIngredientFilter` だけで `AllowPlantFood` を許可します。VCEから継承するこの特殊フィルターは、`RoughPlantBase` の `foodType=Plant` を持つ3種類の粉を材料設定の一覧から隠すためです。材料候補の一覧や、ほかの特殊フィルター・別レシピの制限は維持します。

C#は `WorkGiver_DoBill.TryFindBestBillIngredientsInSet_AllowMix` の正確な5引数シグネチャを確認してパッチします。どれが粉かは名前の一覧ではなく、レシピ内で小麦粉を受け付ける材料枠（粉枠）が許可する定義で判断します。他MODが粉枠に別の粉を追加した場合も、それを独立した1種類として扱い、他の粉との合算を防ぎます。粉を1種類ずつに絞ったリストのコピーを渡し、粉の種類数だけ元の材料選択処理を呼びます。必要量・材料許可・粉以外の選択はゲーム側の判定を使い、`allowMixingIngredients` は変更しません。再帰ガードと例外時の復元、失敗時の一時選択破棄を実装しています。

パッチ初期化に失敗した場合はエラーを記録し、対象料理の粉枠から追加2種類の粉を外します。小麦粉と他材料は残り、粉枠の表示も「小麦粉」に戻ります。fixed/defaultフィルターは変更しません。ゲームはセーブ時に、fixedフィルターにない材料を製造指示の許可設定から削除するためです。これにより、失敗中にセーブしても、プレイヤーの米粉・トウモロコシ粉の許可設定は失われません。DLL自体を入れ忘れるとこの保護も動かないため、配布生成時は両版のDLLを必須としています。

他MODのパッチも元メソッド呼び出し時に実行されますが、すべての調理MODとの互換性を保証するものではありません。対象メソッドを置換・中止するMODや、当MODを継承元にするレシピ追加MODは個別検証が必要です。

## 検証状況

- Python：20件成功（XML、実VCE定義との比較、表示名と日本語訳の対応、画像、ロード構成、梱包）。
- C#選択ポリシー：15件成功。
- 実Harmony 3＋ゲームAPI形状の代替環境：13シナリオ成功。
- 1.6/1.7：警告・エラーなしでDLLビルド、APIシグネチャ／対象ランタイム／参照バージョン検査成功。
- RimWorld 1.7.9787.38423の実アセンブリ（ローカル検査）：33項目成功。ゲームの材料選択処理、XMLパッチ適用・継承解決、DLLの読み込み可否、初期化失敗時の動作。
- 今回の表示不具合：RimWorld 1.7.9790.38077の実アセンブリで修正前の非表示を再現し、修正後に3種類の粉が表示対象となることを確認。カテゴリー経由の追加粉の受け入れ・材料探索などを含む37項目成功（画面描画そのものは未確認）。
- **ゲーム内検証**：2026-10-24、ユーザーから検証成功の報告を受領し、正式版の公開を決定。個別の試験項目・1.6実機の詳細記録は未受領です。

詳細とリリース前の必須手順は [検証記録](docs/VALIDATION.md)、判断理由は [作業記録](docs/WORK_LOG.md) にあります。

## 参照

- [Vanilla Cooking Expanded](https://github.com/Vanilla-Expanded/VanillaCookingExpanded)
- [Vanilla Expanded Framework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework)
- [RimRef](https://github.com/krafs/RimRef)
- [Harmony for RimWorld](https://github.com/pardeike/HarmonyRimWorld)

非公式の互換アドオンです。VCE本体のファイルは改変しません。
