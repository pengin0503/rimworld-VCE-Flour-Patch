# SDD ledger — plan: docs/superpowers/plans/2026-10-23-handoff.md

2026-10-23: Read original spec/plan, all source/data/tests/build instructions.
Baseline: python3 -m unittest discover -s original/tests -v: 11 passed.
Pre-flight: Task 1's directory relocation requires build/test paths in Tasks 2–3 updated together.
Ruling: Continue under user's approved spec without another design confirmation; implementation details explicitly delegated.
Ruling: Work in a fresh clone/feature branch; no additional worktree is needed for isolation.
Ruling: No subagents; final review will be an explicit self-review per user preference.
Findings: source-string checks do not execute C#; prefix matching also captures third-party names;
abstract-parent XML changes leak to other derived recipes; missing target silently disables enforcement;
manual ingredient-value prediction may diverge from the game's solver and skips alternate successful attempts.
Dependency evidence: krafs/RimRef README confirms net472 (1.6), net10.0 (1.7), signatures-only distribution.
pardeike/HarmonyRimWorld README confirms Harmony 2.4.3 (1.6), Harmony 3 (1.7).
VCE commit 74b2fd7dfb1a9c9aaec2b6108c6a01b5463ff4f1 loads 1.6 definitions for both 1.6 and 1.7.
Ruling: Replace quantity prediction with up to three isolated calls to the original solver. This retains
bill permissions and game-specific calculations; potential cost is up to three solver calls on targeted recipes.
Ruling: Keep in-game checks as a release gate; successful compilation is not runtime verification.

Task 1: parent/prefix leakage regression RED (test_patch_existing_recipes_and_filters), exact concrete patch GREEN.
Task 2: pure policy mixed-shortage regression RED → 15 policy checks GREEN.
API probe: both 1.6.4871 and 1.7.4985-unstable expose static bool AllowMix(List<Thing>, Bill, List<ThingCount>, IntVec3, List<IngredientCount>).
SDK 10.0.402 installed locally; source builds use signatures-only NuGet references (not game implementation).
The supplied source's four string-contract tests were replaced with actual C# and metadata tests; useful loading checks retained.
Task 3: packaging absent RED → four packaging cases GREEN; 15 total Python checks pass.
Final review: self-review per user's no-subagent preference, weaker than independent review.
Final: fixed list-replacement recursion — --clone-prefix RED→GREEN; bill identity guard, restored in finally.
Final: fixed injected mixed flour — --injected-flour RED→GREEN; snapshot allowed Thing identities before invoking solver.
Ruling: Same-bill recursive calls bypass only this patch while an attempt is active; candidate output is validated afterward.
Reason: other Harmony prefixes may replace the candidate List instance. Cost: arbitrary mods re-entering the same bill for a separate search need individual testing.
Ruling: Ordinary no-mix recipes (gourmet) continue through native no-mix dispatch; no global mixing flag mutation.
Cost: no claim that arbitrary external replacements of the native no-mix solver are compatible.
Ruling: Public development CI artifact, no completed Release until mandatory game checks pass; lack of game executable is the remaining blocker.
Automated evidence: 15 Python checks + 15 policy checks + 9 Harmony boundary scenarios + two metadata checks, both target DLLs warning/error-free.
No deferred minor code issues were identified. Runtime verification omissions are explicit release blockers, not minor findings.

2026-10-24 review follow-up (user-supplied RimWorld 1.7.9787.38423 assemblies, Harmony 3.0.0-rc.3, Workshop VCE).
Ruling (user): flour slot shows "grain flour"/穀物粉 via customSummary; README states existing bills always need manual allowance.
Ruling (user): fail-closed keeps fixed/default filters; Bill.ExposeData strips allowances outside fixedIngredientFilter on save.
Ruling (user): flour kinds = defs allowed by the slot that accepts VCE_Flour, not a defName list. Textures prefixed VCE_FPatch_.
Ruling (user): verify.yml '**.md' trigger left as is.
Finding: 1.7.4985-unstable reference requests Assembly-CSharp 1.7.9790; LoadFrom on 1.7.9787 fails (.NET does not bind a lower version).
Ruling: build 1.7 against oldest RimRef 1.7.4977-alpha.1 (1.7.9782); Metadata and GameRuntime checks guard the reference version.
Added opt-in tests/GameRuntime (RIMWORLD_17_MANAGED_DIR): real solver, PatchOperationAdd, XmlInheritance on the built DLL; 33 checks pass on 1.7.9787.
Reason: CI cannot run game assemblies; this caught the version-binding failure that reference-only checks missed.


## 2026-10-24: 焼き料理の材料設定から粉が消える問題

- 原因：3種類の粉は RoughPlantBase の foodType=Plant を継承する。VCE の焼き料理の fixedIngredientFilter は AllowPlantFood を禁止するため、ゲームの Listing_TreeThingFilter.Visible(ThingDef) が粉を隠していた。単一の固定材料から選択可能な粉枠へ変更した際に表面化した。
- 修正：ソルバーパッチの初期化成功時、既存の対象7レシピだけ fixedIngredientFilter.SetAllow(AllowPlantFood, true) を呼ぶ。ほかの特殊フィルター、材料候補、別レシピ、プレイヤーの製造指示設定は変更しない。
- 回帰テスト：実ゲームの表示判定に Plant 型の粉を入力して修正前の失敗を確認。修正後は3種類が表示対象となり、非対象レシピの禁止設定は残る。CIの境界テストでも7レシピと別の特殊フィルターの保持を検査する。
- ローカル検証：RimWorld 1.7.9790.38077でビルド成功、実アセンブリ検査37項目成功。ゲーム画面の描画・実際の調理は未確認。
- 範囲：代替材料の追加は従来どおりVCE本体の焼き料理7レシピ。全MODの小麦粉レシピへの自動展開はこの不具合修正に含めない。

- ユーザーの設計提案を反映し、PFA_GrainFlour カテゴリーを追加。3種の粉の所属と7レシピの粉枠・fixed/defaultフィルターをカテゴリー参照へ統一した。食品種別による非表示判定はカテゴリーと独立しているため、AllowPlantFood の整合処理も保持する。


## 2026-10-24: 製粉レシピを2倍量・8倍量に変更

- 米粉とトウモロコシ粉にそれぞれ2倍量（原料20・完成品20・工数800）と8倍量（原料80・完成品80・工数3200）の製粉指示を用意。
- 従来の PFA_MillRice / PFA_MillCorn は2倍量として定義名を維持し、既存の製造指示を引き継ぐ。8倍量は末尾 Bulk の新規定義。
- かまど・電気コンロ、Cooking/CookSpeed、原料フィルターは従来どおり。日本語・英語の表示と説明を更新。
- 標準ライブラリーによる製粉テスト3件で、修正前の失敗と修正後の成功を確認。全体のCIも実施する。


## 2026-10-24: 正式版 v0.1.0

- ユーザーからゲーム内検証成功の報告と正式版公開の依頼を受領。自動検証とは別の利用者確認として記録した。
- 対象ソースには、中央配置のトウモロコシ粉PNGと日本語製粉レシピ名の x2 / x8 表示を含める。
- 正式版用に同梱READMEを更新し、再ビルド・梱包をCIで検証した成果物だけを公開する。1.6実機の個別検証結果は受領していない。


## 2026-10-24: 製粉の工数を短縮

- ユーザーの依頼で、ゲーム内表示の工数を x2：14→10、x8：54→40 に変更。
- RimWorldの工数表示は workAmount/60 の切り上げ（800→14、3200→54）。workAmount を x2：600、x8：2400 とし、表示値がちょうど10・40になる値を選んだ。
- x2とx8の比率（1:4）は維持。1個当たりの工数は40から30へ下がる。
- 製粉テストの期待値を先に変更して失敗を確認後、定義を更新して成功を確認。
