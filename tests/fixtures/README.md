# Upstream XML fixtures

Unmodified VCE definitions, retrieved 2026-10-23 from Vanilla-Expanded/VanillaCookingExpanded,
commit 74b2fd7dfb1a9c9aaec2b6108c6a01b5463ff4f1:

- 1.6/Defs/RecipeDefs/Recipes_Bakes.xml (blob f96f2cf927a2468943bc50c04217de4cb8492633)
- 1.6/Defs/ThingDefs_Items/Items_RawPlant.xml (blob f6e1e005c24781685a8574bf1f69a80002d9e2a5)

Source: https://github.com/Vanilla-Expanded/VanillaCookingExpanded
Authorship remains with the upstream project. Used only for compatibility tests; excluded from mod packages.
The upstream loadFolders.xml selects these 1.6 definitions for 1.7 as well.
These tests apply XPath additions before inheritance, and verify the existing parent filters remain intact.
They do not execute RimWorld's XML inheritance resolver; that is an in-game acceptance test.
