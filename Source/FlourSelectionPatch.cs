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
                FlourSelectionPatch.Install();
                Log.Message("[VCE Flour Alternatives] Single-flour solver patch installed.");
            }
            catch (Exception ex)
            {
                FlourSelectionPatch.DisableAddedFlours();
                Log.Error("[VCE Flour Alternatives] Alternate flours disabled: patch setup failed. " + ex);
            }
        }
    }

    internal static class FlourSelectionPatch
    {
        [ThreadStatic] private static Bill activeBill;
        private static ThingDef wheatFlour;

        internal static MethodInfo ResolveTarget()
        {
            MethodInfo method = AccessTools.Method(typeof(WorkGiver_DoBill),
                "TryFindBestBillIngredientsInSet_AllowMix",
                new[] { typeof(List<Thing>), typeof(Bill), typeof(List<ThingCount>),
                        typeof(IntVec3), typeof(List<IngredientCount>) });
            return method != null && method.IsStatic && method.ReturnType == typeof(bool) ? method : null;
        }

        internal static void Install()
        {
            MethodInfo target = ResolveTarget();
            if (target == null) throw new MissingMethodException("Unexpected AllowMix solver signature");
            ThingDef wheat = DefDatabase<ThingDef>.GetNamedSilentFail(FlourPolicy.WheatFlour);
            if (wheat == null)
                throw new InvalidOperationException("Missing required flour definition: " + FlourPolicy.WheatFlour);
            foreach (string name in FlourPolicy.AddedFlours)
                if (DefDatabase<ThingDef>.GetNamedSilentFail(name) == null)
                    throw new InvalidOperationException("Missing required flour definition: " + name);
            wheatFlour = wheat;
            var prefix = new HarmonyMethod(typeof(FlourSelectionPatch), nameof(Prefix));
            prefix.priority = Priority.Last;
            new Harmony(Bootstrap.HarmonyId).Patch(target, prefix: prefix);
        }

        // Defs are resolved before StaticConstructorOnStartup. Only the flour slots lose the
        // added flours, so wheat and all other ingredients stay usable. The fixed/default
        // filters are kept: saving a bill strips allowances missing from fixedIngredientFilter,
        // which would otherwise erase the player's flour settings permanently.
        internal static void DisableAddedFlours()
        {
            ThingDef wheat = DefDatabase<ThingDef>.GetNamedSilentFail(FlourPolicy.WheatFlour);
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (!FlourPolicy.IsTarget(recipe.defName) || recipe.ingredients == null) continue;
                foreach (IngredientCount ingredient in recipe.ingredients)
                {
                    // Without wheat flour the slot cannot be identified; clear every slot instead.
                    if (wheat != null && !ingredient.filter.Allows(wheat)) continue;
                    foreach (string name in FlourPolicy.AddedFlours)
                    {
                        ThingDef flour = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                        if (flour != null) ingredient.filter.SetAllow(flour, false);
                    }
                    // The slot is wheat-only again; show its own label instead of "grain flour".
                    if (wheat != null) ingredient.filter.customSummary = null;
                }
            }
        }

        // The slot that accepts wheat flour defines what counts as flour, so flours other
        // mods add to that slot are kept apart from wheat, rice flour and cornmeal as well.
        private static IngredientCount FlourSlot(RecipeDef recipe)
        {
            if (wheatFlour == null || recipe.ingredients == null) return null;
            foreach (IngredientCount ingredient in recipe.ingredients)
                if (ingredient.filter.Allows(wheatFlour)) return ingredient;
            return null;
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
            IngredientCount slot = FlourSlot(bill.recipe);
            if (slot == null) return true;

            List<IngredientCount> lastMissing = null;
            __result = FlourPolicy.TryChoose(availableThings,
                thing => thing != null && slot.filter.Allows(thing.def) ? thing.def.defName : null,
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
                                if (slot.filter.Allows(item.Thing.def) && !permitted.Contains(item.Thing))
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
