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
