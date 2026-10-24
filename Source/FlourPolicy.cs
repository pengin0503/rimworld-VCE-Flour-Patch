using System;
using System.Collections.Generic;

namespace VCEFlourAlternatives
{
    // No game-specific quantity math: the supplied solver remains authoritative.
    internal static class FlourPolicy
    {
        internal const string WheatFlour = "VCE_Flour";

        internal static readonly string[] AddedFlours = { "PFA_RiceFlour", "PFA_Cornmeal" };

        internal static readonly string[] TargetRecipes =
        {
            "VCE_CookBakeSimple", "VCE_CookBakeSimpleBulk", "VCE_CookBakeFine", "VCE_CookBakeFineBulk",
            "VCE_CookBakeLavish", "VCE_CookBakeLavishBulk", "VCE_CookBakeGourmet",
        };

        internal static bool IsTarget(string name) => Array.IndexOf(TargetRecipes, name) >= 0;

        // A bill saved before the mod only knows wheat; one that allows wheat is meant to use flour.
        // Bills without wheat are the player's explicit choice and are never touched.
        internal static bool NeedsAddedFlours(bool allowsWheat, IList<bool> allowsAdded)
        {
            if (!allowsWheat) return false;
            for (int i = 0; i < allowsAdded.Count; i++)
                if (!allowsAdded[i]) return true;
            return false;
        }

        // Problems that leave a target recipe without exactly one flour slot, for the startup log.
        internal static string SlotProblem(bool recipeExists, int slotsAcceptingWheat)
        {
            if (!recipeExists) return "recipe not found";
            if (slotsAcceptingWheat == 0) return "no ingredient slot accepts wheat flour";
            if (slotsAcceptingWheat > 1) return slotsAcceptingWheat + " ingredient slots accept wheat flour";
            return null;
        }

        internal static bool TryChoose<T, C>(IReadOnlyList<T> available,
            Func<T, string> flourKind, Func<List<T>, List<C>, bool> solve, List<C> chosen)
        {
            chosen.Clear();
            var kinds = new List<string>();
            for (int i = 0; i < available.Count; i++)
            {
                string kind = flourKind(available[i]);
                if (kind != null && !kinds.Contains(kind)) kinds.Add(kind);
            }
            // Still call the solver with no flour so it can report missing ingredients.
            if (kinds.Count == 0) kinds.Add(null);
            foreach (string selected in kinds)
            {
                var candidates = new List<T>();
                for (int i = 0; i < available.Count; i++)
                {
                    string kind = flourKind(available[i]);
                    if (kind == null || kind == selected) candidates.Add(available[i]);
                }
                var attempt = new List<C>();
                if (!solve(candidates, attempt)) continue;
                chosen.AddRange(attempt);
                return true;
            }
            return false;
        }
    }
}
