// Local check against the player's own RimWorld 1.7 assemblies. It runs the game's real
// WorkGiver_DoBill solvers, PatchOperationAdd and XmlInheritance with the built mod DLL.
// Replaced: the nutrition getter (fixed 0.05) and engine services the solvers do not use
// (profiler, mod-config lookup, type scan). ThingDefs skip their constructor, which loads graphics.
// This is NOT an in-game test: hauling, consumption, saving and rendered UI are not exercised.
// The real ingredient-list visibility predicate is exercised with Plant-typed flour defs.
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using RimWorld;
using Verse;

static class Program
{
    static int failures, nextId = 1;
    static string flourCategoryName;
    static readonly MethodInfo AllowMix = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet_AllowMix");
    static readonly MethodInfo Dispatch = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet");

    static int Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("mod DLL, VCE Recipes_Bakes.xml and mod patch XML required");
        ResolveFromManagedDir();
        var stubs = new Harmony("tests.gameruntime.stubs");
        Stub(stubs, AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.AnomalyActive)), nameof(Stubs.False));
        Stub(stubs, AccessTools.PropertyGetter(typeof(GenTypes), nameof(GenTypes.AllTypes)), nameof(Stubs.AllTypes));
        Stub(stubs, AccessTools.Method(typeof(DeepProfiler), nameof(DeepProfiler.Start)), nameof(Stubs.Skip));
        Stub(stubs, AccessTools.Method(typeof(DeepProfiler), nameof(DeepProfiler.End)), nameof(Stubs.Skip));

        string summary = CheckXml(args[1], args[2]);
        // RimWorld loads mods with Assembly.LoadFrom; a reference newer than the game cannot bind.
        Version requested;
        using (var pe = new PEReader(File.OpenRead(args[0])))
        {
            MetadataReader md = pe.GetMetadataReader();
            requested = md.AssemblyReferences.Select(h => md.GetAssemblyReference(h))
                .Single(r => md.GetString(r.Name) == "Assembly-CSharp").Version;
        }
        Version game = typeof(Def).Assembly.GetName().Version;
        Check($"mod's Assembly-CSharp reference {requested} binds to game {game}", requested <= game);
        if (failures > 0) { Console.WriteLine("game-runtime checks stopped: the mod DLL cannot load on this game build"); return 1; }
        CheckSolver(Assembly.LoadFrom(Path.GetFullPath(args[0])), summary);
        Console.WriteLine(failures == 0 ? "All game-runtime checks passed (RimWorld 1.7 assemblies, not an in-game test)."
                                        : failures + " game-runtime check(s) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // The engine logs through Brutal/Microsoft.Extensions.Logging, which are not compile references;
    // without this the first Log call fails before any check runs.
    static void ResolveFromManagedDir()
    {
        string dir = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RimWorldManagedDir")?.Value;
        if (string.IsNullOrEmpty(dir)) return;
        System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            string path = Path.Combine(dir, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
    }

    static void Stub(Harmony h, MethodBase m, string prefix) => h.Patch(m, prefix: new HarmonyMethod(typeof(Stubs), prefix));

    static void Check(string name, bool ok, string detail = "")
    {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + name + (detail.Length > 0 ? "  [" + detail + "]" : ""));
        if (!ok) failures++;
    }

    // Returns the flour slot's customSummary so the solver checks use the shipped label.
    static string CheckXml(string recipesPath, string patchPath)
    {
        var doc = new XmlDocument(); doc.Load(recipesPath);
        var patch = new XmlDocument(); patch.Load(patchPath);
        // ApplyWorker directly: Apply() only adds success bookkeeping that needs Prefs.
        MethodInfo apply = AccessTools.Method(typeof(PatchOperationAdd), "ApplyWorker");
        foreach (XmlNode li in patch.SelectNodes("/Patch/Operation[@Class='PatchOperationSequence']/operations/li[@Class='PatchOperationAdd']"))
        {
            var op = new PatchOperationAdd();
            AccessTools.Field(typeof(PatchOperationPathed), "xpath").SetValue(op, li["xpath"].InnerText);
            AccessTools.Field(typeof(PatchOperationAdd), "value").SetValue(op, new XmlContainer { node = li["value"] });
            // Fallback alternatives may match nothing; the game counts them as succeeded (success=Always).
            bool always = li["success"]?.InnerText == "Always";
            bool applied = (bool)apply.Invoke(op, new object[] { doc });
            Check("patch operation " + (applied ? "adds" : "skips") + " <" + li["value"].FirstChild.Name + "> at " + Truncate(li["xpath"].InnerText),
                applied || always);
        }
        foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            if (node.NodeType == XmlNodeType.Element) XmlInheritance.TryRegister(node, null);
        XmlInheritance.Resolve();
        string summary = null;
        foreach (XmlNode node in doc.DocumentElement.SelectNodes("RecipeDef[defName]"))
        {
            XmlNode r = XmlInheritance.GetResolvedNodeFor(node);
            string L(string xpath) => string.Join(",", r.SelectNodes(xpath).Cast<XmlNode>().Select(x => x.InnerText));
            XmlNode slot = r.SelectSingleNode("ingredients/li[filter/thingDefs/li='VCE_Flour']");
            summary = slot?.SelectSingleNode("filter/customSummary")?.InnerText;
            flourCategoryName = slot?.SelectSingleNode("filter/categories/li")?.InnerText;
            Check("inherited filters: " + r["defName"].InnerText,
                L("fixedIngredientFilter/thingDefs/li") == "VCE_Flour" && L("defaultIngredientFilter/thingDefs/li") == "VCE_Flour"
                && L("ingredients/li/filter/thingDefs[li='VCE_Flour']/li") == "VCE_Flour"
                && L("ingredients/li/filter[thingDefs/li='VCE_Flour']/categories/li") == "PFA_GrainFlour"
                && L("fixedIngredientFilter/categories/li").Contains("PFA_GrainFlour")
                && L("defaultIngredientFilter/categories/li").Contains("PFA_GrainFlour")
                && r.SelectNodes("fixedIngredientFilter").Count == 1 && r.SelectNodes("defaultIngredientFilter").Count == 1
                && L("fixedIngredientFilter/categories/li").Contains("FoodRaw")
                && L("fixedIngredientFilter/specialFiltersToDisallow/li") == "AllowPlantFood"
                && L("defaultIngredientFilter/disallowedThingDefs/li").Contains("Meat_Human")
                && summary == "grain flour" && r.SelectNodes("//customSummary").Count == 1,
                "mix=" + L("allowMixingIngredients"));
        }
        return summary;
    }

    static string Truncate(string xpath) => xpath.Substring(xpath.LastIndexOf(']') + 1);

    static ThingDef Def(string name)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = name; d.label = name; d.useHitPoints = false; d.virtualDefs = new List<ThingDef>();
        DefDatabase<ThingDef>.Add(d);
        return d;
    }

    static ThingFilter Filter(params ThingDef[] defs)
    {
        var f = new ThingFilter();
        foreach (ThingDef d in defs) f.SetAllow(d, true);
        return f;
    }

    static void CheckSolver(Assembly mod, string summary)
    {
        ThingDef wheat = Def("VCE_Flour"), rice = Def("PFA_RiceFlour"), corn = Def("PFA_Cornmeal");
        ThingDef meat = Def("Meat_Test"), veg = Def("RawPotato_Test"), thirdParty = Def("RC2_Flour_Test");
        // RoughPlantBase gives the shipped flours foodType=Plant, which VCE's
        // inherited AllowPlantFood restriction hides in the bill UI.
        foreach (ThingDef flour in new[] { wheat, rice, corn })
            flour.ingestible = new IngestibleProperties { foodType = FoodTypeFlags.Plant };
        var plantFood = new SpecialThingFilterDef
        {
            defName = "AllowPlantFood", workerClass = typeof(SpecialThingFilterWorker_PlantFood),
        };
        DefDatabase<SpecialThingFilterDef>.Add(plantFood);
        var grainCategory = new ThingCategoryDef
        {
            defName = flourCategoryName, childThingDefs = new List<ThingDef> { wheat, rice, corn, thirdParty },
        };
        RecipeDef Recipe(string name, bool mix)
        {
            ThingDef[] all = { meat, veg, wheat, rice, corn, thirdParty };
            var flourSlot = new IngredientCount { filter = new ThingFilter() };
            flourSlot.filter.SetAllow(grainCategory, true, null, null);
            flourSlot.filter.customSummary = summary;
            flourSlot.SetBaseCount(0.5f);
            var meatSlot = new IngredientCount { filter = Filter(meat) }; meatSlot.SetBaseCount(0.25f);
            var vegSlot = new IngredientCount { filter = Filter(veg) }; vegSlot.SetBaseCount(0.25f);
            var r = new RecipeDef
            {
                defName = name, allowMixingIngredients = mix, fixedIngredientFilter = Filter(all), defaultIngredientFilter = Filter(all),
                ingredients = new List<IngredientCount> { meatSlot, vegSlot, flourSlot },
            };
            AccessTools.Field(typeof(RecipeDef), "ingredientValueGetterClass").SetValue(r, typeof(FixedNutrition));
            r.fixedIngredientFilter.SetAllow(plantFood, false);
            DefDatabase<RecipeDef>.Add(r);
            return r;
        }
        RecipeDef fine = Recipe("VCE_CookBakeFine", true), other = Recipe("OtherBake", true), gourmet = Recipe("VCE_CookBakeGourmet", false);
        Bill MakeBill(RecipeDef r, params ThingDef[] allowed)
        {
            var b = (Bill_Production)RuntimeHelpers.GetUninitializedObject(typeof(Bill_Production));
            b.recipe = r;
            b.ingredientFilter = Filter(allowed.Length > 0 ? allowed : r.defaultIngredientFilter.AllowedThingDefs.ToArray());
            return b;
        }
        Thing T(ThingDef def, int count, int x = 0)
        {
            var t = new Thing { def = def, stackCount = count, thingIDNumber = nextId++ };
            AccessTools.Field(typeof(Thing), "positionInt").SetValue(t, new IntVec3(x, 0, 0));
            return t;
        }
        List<Thing> With(params Thing[] flour) => new List<Thing> { T(meat, 5, 1), T(veg, 5, 1) }.Concat(flour).ToList();
        var chosen = new List<ThingCount>();
        var missing = new List<IngredientCount>();
        bool Run(List<Thing> things, Bill bill, bool reportMissing = true) =>
            (bool)AllowMix.Invoke(null, new object[] { things, bill, chosen, IntVec3.Zero, reportMissing ? missing : null });
        bool RunDispatch(List<Thing> things, Bill bill) =>
            (bool)Dispatch.Invoke(null, new object[] { things, bill, chosen, IntVec3.Zero, true, missing });
        string Chosen() => string.Join(",", chosen.Select(c => c.Thing.def.defName + "x" + c.Count));
        bool Only(ThingDef flour) => chosen.Where(c => fine.ingredients[2].filter.Allows(c.Thing.def)).All(c => c.Thing.def == flour);

        Check("recipe category accepts an additional registered flour", fine.ingredients[2].filter.Allows(thirdParty));
        Check("flour slot label", fine.ingredients[2].Summary == "0.5x grain flour", fine.ingredients[2].Summary);
        Check("control: unpatched game mixes wheat 5 + rice 5", Run(With(T(wheat, 5), T(rice, 5)), MakeBill(fine)), Chosen());

        bool Visible(RecipeDef r, ThingDef flour)
        {
            var listing = (Listing_TreeThingFilter)RuntimeHelpers.GetUninitializedObject(typeof(Listing_TreeThingFilter));
            AccessTools.Field(typeof(Listing_TreeThingFilter), "parentFilter").SetValue(listing, r.fixedIngredientFilter);
            return (bool)AccessTools.Method(typeof(Listing_TreeThingFilter), "Visible", new[] { typeof(ThingDef) })
                .Invoke(listing, new object[] { flour });
        }
        Check("control: inherited plant restriction hides all three flours",
            new[] { wheat, rice, corn }.All(f => !Visible(fine, f)));

        // Game.FillComponents creates every GameComponent subclass through its (Game) constructor.
        Type migration = mod.GetType("VCEFlourAlternatives.GameComponent_FlourBillMigration", true);
        Check("bill migration is a GameComponent the game can create",
            typeof(GameComponent).IsAssignableFrom(migration) && migration.GetConstructor(new[] { typeof(Game) }) != null);

        Type patch = mod.GetType("VCEFlourAlternatives.FlourSelectionPatch", true);
        // Same call Bootstrap makes; Bootstrap itself also logs, which needs the full engine.
        patch.GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Check("patch installed on the game's solver", Harmony.GetPatchInfo(AllowMix)?.Owners.Contains("pengin0503.vce.flouralternatives") == true);

        Check("target bill UI shows wheat, rice flour and cornmeal",
            new[] { fine, gourmet }.All(r => new[] { wheat, rice, corn }.All(f => Visible(r, f))));
        Check("unrelated recipe keeps its plant restriction", !Visible(other, rice));

        Check("wheat 5 + rice 5 rejected", !Run(With(T(wheat, 5), T(rice, 5)), MakeBill(fine)) && chosen.Count == 0);
        Check("missing reports the flour slot", missing.Count == 1 && missing[0] == fine.ingredients[2]);
        Check("rejected without a missing list", !Run(With(T(wheat, 5), T(rice, 5)), MakeBill(fine), reportMissing: false));
        Check("rice 4 + rice 6 accepted", Run(With(T(rice, 4, 2), T(rice, 6, 9)), MakeBill(fine)) && Only(rice)
            && chosen.Where(c => c.Thing.def == rice).Sum(c => c.Count) == 10, Chosen());
        Check("near short wheat skipped, far rice used", Run(With(T(wheat, 3, 0), T(rice, 10, 50)), MakeBill(fine)) && Only(rice), Chosen());
        Check("bill forbids wheat -> rice", Run(With(T(wheat, 10), T(rice, 10)), MakeBill(fine, meat, veg, rice, corn)) && Only(rice), Chosen());
        Check("bill made before install cannot use rice", !Run(With(T(rice, 10)), MakeBill(fine, meat, veg, wheat)));
        Check("bill made before install still uses wheat", Run(With(T(wheat, 10)), MakeBill(fine, meat, veg, wheat)), Chosen());
        Check("meat shortage still reported", !Run(new List<Thing> { T(meat, 2), T(veg, 5), T(wheat, 10), T(rice, 10) }, MakeBill(fine))
            && missing.Contains(fine.ingredients[0]));
        var input = With(T(rice, 10, 30), T(wheat, 10, 0));
        var snapshot = input.ToList();
        Run(input, MakeBill(fine));
        Check("caller list neither mutated nor re-sorted", input.SequenceEqual(snapshot));
        Check("other recipes keep vanilla mixing", Run(With(T(wheat, 5), T(rice, 5)), MakeBill(other)));
        Check("gourmet (no-mix) rejects wheat 5 + rice 5", !RunDispatch(With(T(wheat, 5), T(rice, 5)), MakeBill(gourmet)));
        Check("gourmet (no-mix) accepts rice 4 + rice 6", RunDispatch(With(T(rice, 4), T(rice, 6)), MakeBill(gourmet)), Chosen());
        fine.ingredients[2].filter.SetAllow(thirdParty, true);
        Check("another mod's flour in the slot is its own kind", !Run(With(T(wheat, 5), T(thirdParty, 5)), MakeBill(fine))
            && Run(With(T(thirdParty, 10)), MakeBill(fine)), Chosen());
        fine.ingredients[2].filter.SetAllow(thirdParty, false);

        // Fail-closed path, run last because it changes the recipes.
        Bill existing = MakeBill(fine);
        patch.GetMethod("DisableAddedFlours", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        ThingFilter slot = fine.ingredients[2].filter;
        Check("disabled: flour slot is wheat only with its own label", slot.Allows(wheat) && !slot.Allows(rice) && !slot.Allows(corn)
            && fine.ingredients[2].IsFixedIngredient && slot.customSummary == null);
        Check("disabled: other recipes untouched", other.ingredients[2].filter.Allows(rice));
        // Bill.ExposeData strips allowances not in fixedIngredientFilter on save; keeping them preserves settings.
        Check("disabled: saved bill allowances survive", fine.fixedIngredientFilter.Allows(rice) && existing.ingredientFilter.Allows(rice));
        Check("disabled: rice no longer used, wheat still works",
            !Run(With(T(rice, 10)), existing) && Run(With(T(wheat, 10)), existing), Chosen());
    }
}

public class FixedNutrition : IngredientValueGetter
{
    public override float ValuePerUnitOf(ThingDef t) => 0.05f;
    public override string BillRequirementsDescription(RecipeDef r, IngredientCount ing) => "";
}

static class Stubs
{
    public static bool Skip() => false;
    public static bool False(ref bool __result) { __result = false; return false; }
    // XmlInheritance only scans types for [XmlInheritanceAllowDuplicateNodes]; the full scan needs the mod manager.
    public static bool AllTypes(ref List<Type> __result)
    {
        Type[] types;
        try { types = typeof(Def).Assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { types = e.Types; }
        __result = types.Where(t => t != null).ToList();
        return false;
    }
}
