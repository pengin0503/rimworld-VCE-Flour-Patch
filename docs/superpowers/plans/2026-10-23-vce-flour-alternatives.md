# VCE Flour Alternatives Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make rice flour and cornmeal function as mutually exclusive alternative flour ingredients in VCE recipes, with all other ingredient mixing preserved.

**Architecture:** XML expands item and recipe Defs and ingredient filters. A Harmony Prefix limits only the three flour types passed to RimWorld's mixed-ingredient solver, preserving its original non-flour behavior. Separate game-version assembly builds.

**Tech Stack:** RimWorld 1.6/1.7 XML, Harmony 2/3, C#, Python unittest/lxml/Pillow static checks.

**Spec:** `docs/superpowers/specs/2026-10-23-vce-flour-alternatives-design.md`

## Global Constraints
- No dedicated meals; reuse VCE's existing bake recipes.
- Single powder type per bill execution; no restriction on other slots.
- Existing VCE material definitions and harvest unchanged.
- Image assets untouched; 128x128 RGBA.
- No game/runtime verification without RimWorld and reference assemblies.

## Review Focus
- Different flour types separately insufficient but sufficient when mixed must NOT cook.
- Two stacks of the SAME flour must remain usable.
- Other recipe ingredient slots may still mix.
- No flour candidates allowed by bill settings must fail gracefully.
- Harmony target absent/changed on 1.7 must log explicitly rather than silently changing all recipes.

---

### Task 1: XML correctness and existing recipe filtering
**Files:** `tests/test_mod.py`, `VCE-Flour-Alternatives/Patches/VCE_FlourAlternatives.xml`
- [x] Write tests modelling actual master Defs XML wrapper; verify 7 bake recipes and parent filters; remove false-positive test fixture.
- [x] Run tests and verify RED.
- [x] Correct any patch structure and pass the static tests.

### Task 2: Strict single-powder behavior with Harmony
**Files:** `VCE-Flour-Alternatives/Source/FlourSelectionPatch.cs`, `VCE-Flour-Alternatives/Source/VCEFlourAlternatives.csproj`, `tests/test_source.py`
- [x] Write a source contract test for scoped patching, no default mix toggle, and selection of sufficient single flour type.
- [x] Verify RED.
- [x] Implement minimal Harmony Prefix scoped to VCE bake recipe; preserve non-flour candidates and matching bill permissions.
- [x] Pass static tests.
- [ ] Validate C# compilation (external RimWorld/Harmony DLL and .NET SDK unavailable in this environment).

### Task 3: Deliverable, setup and checks
**Files:** `README.md`, `VCE-Flour-Alternatives/README.md`, `build.ps1`, ZIP archive.
- [x] Validate textures, XML, and recorded version assumptions.
- [x] Document C# build inputs for 1.6/1.7, mod loading, and remaining game verification.
- [x] Run tests, package source without test caches, then inspect archive contents.
