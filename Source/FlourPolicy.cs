using System;
using System.Collections.Generic;

namespace VCEFlourAlternatives
{
    // No game-specific quantity math: the supplied solver remains authoritative.
    internal static class FlourPolicy
    {
        internal const string WheatFlour = "VCE_Flour";

        internal static readonly string[] AddedFlours = { "PFA_RiceFlour", "PFA_Cornmeal" };

        internal static bool IsTarget(string name)
        {
            switch (name)
            {
                case "VCE_CookBakeSimple": case "VCE_CookBakeSimpleBulk":
                case "VCE_CookBakeFine": case "VCE_CookBakeFineBulk":
                case "VCE_CookBakeLavish": case "VCE_CookBakeLavishBulk":
                case "VCE_CookBakeGourmet": return true;
                default: return false;
            }
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
