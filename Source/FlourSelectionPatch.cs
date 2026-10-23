using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VCEFlourAlternatives
{
    [StaticConstructorOnStartup]
    internal static class Bootstrap
    {
        internal const string HarmonyId = "pengin0503.vce.flouralternatives";

        static Bootstrap()
        {
            try
            {
                MethodInfo target = FlourSelectionPatch.ResolveTarget();
                if (target == null) throw new MissingMethodException("Unexpected AllowMix solver signature");
                foreach (string name in new[] { "VCE_Flour", "PFA_RiceFlour", "PFA_Cornmeal" })
                    if (DefDatabase<ThingDef>.GetNamedSilentFail(name) == null)
                        throw new InvalidOperationException("Missing required flour definition: " + name);
                var prefix = new HarmonyMethod(typeof(FlourSelectionPatch), nameof(FlourSelectionPatch.Prefix));
                prefix.priority = Priority.Last;
                new Harmony(HarmonyId).Patch(target, prefix: prefix);
                Log.Message("[VCE Flour Alternatives] Single-flour solver patch installed.");
            }
            catch (Exception ex)
            {
                // Defs are resolved before StaticConstructorOnStartup. Remove only the new
                // flours from target slots, leaving wheat and all other ingredients usable.
                foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                {
                    if (!FlourPolicy.IsTarget(recipe.defName)) continue;
                    foreach (string name in new[] { "PFA_RiceFlour", "PFA_Cornmeal" })
                    {
                        ThingDef flour = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                        if (flour == null) continue;
                        foreach (IngredientCount ingredient in recipe.ingredients)
                            ingredient.filter.SetAllow(flour, false);
                        recipe.fixedIngredientFilter.SetAllow(flour, false);
                        if (recipe.defaultIngredientFilter != null)
                            recipe.defaultIngredientFilter.SetAllow(flour, false);
                    }
                }
                Log.Error("[VCE Flour Alternatives] Alternate flours disabled: patch setup failed. " + ex);
            }
        }
    }

    internal static class FlourSelectionPatch
    {
        [ThreadStatic] private static Bill activeBill;

        internal static MethodInfo ResolveTarget()
        {
            MethodInfo method = AccessTools.Method(typeof(WorkGiver_DoBill),
                "TryFindBestBillIngredientsInSet_AllowMix",
                new[] { typeof(List<Thing>), typeof(Bill), typeof(List<ThingCount>),
                        typeof(IntVec3), typeof(List<IngredientCount>) });
            return method != null && method.IsStatic && method.ReturnType == typeof(bool) ? method : null;
        }

        // The inner invocation intentionally includes other mods' patches. A reverse patch
        // would bypass them. A bill-identity guard survives candidate-list replacement by other prefixes.
        internal static bool Prefix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen,
            IntVec3 rootCell, List<IngredientCount> missingIngredients, MethodBase __originalMethod,
            bool __runOriginal, ref bool __result)
        {
            if (!__runOriginal || ReferenceEquals(activeBill, bill) ||
                bill == null || bill.recipe == null || !bill.recipe.allowMixingIngredients ||
                !FlourPolicy.IsTarget(bill.recipe.defName)) return true;

            List<IngredientCount> lastMissing = null;
            __result = FlourPolicy.TryChoose(availableThings,
                thing => thing != null && FlourPolicy.IsFlour(thing.def.defName) ? thing.def.defName : null,
                (candidates, attempt) =>
                {
                    var missing = missingIngredients == null ? null : new List<IngredientCount>();
                    Bill previous = activeBill;
                    var permitted = new HashSet<Thing>(candidates);
                    try
                    {
                        activeBill = bill;
                        bool result = (bool)__originalMethod.Invoke(null,
                            new object[] { candidates, bill, attempt, rootCell, missing });
                        lastMissing = missing;
                        // Reject another patch introducing flour that was not in this attempt.
                        if (result)
                        {
                            foreach (ThingCount item in attempt)
                                if (FlourPolicy.IsFlour(item.Thing.def.defName) && !permitted.Contains(item.Thing))
                                    return false;
                        }
                        return result;
                    }
                    catch (TargetInvocationException ex)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                        throw;
                    }
                    finally { activeBill = previous; }
                }, chosen);
            if (missingIngredients != null)
            {
                missingIngredients.Clear();
                if (!__result && lastMissing != null) missingIngredients.AddRange(lastMissing);
            }
            return false;
        }
    }
}
