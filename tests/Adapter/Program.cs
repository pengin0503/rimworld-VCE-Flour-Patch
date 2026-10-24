using HarmonyLib;
using RimWorld;
using Verse;
using VCEFlourAlternatives;
using System.Runtime.CompilerServices;

var wheat=new ThingDef{defName="VCE_Flour"};var rice=new ThingDef{defName="PFA_RiceFlour"};var corn=new ThingDef{defName="PFA_Cornmeal"};
DefDatabase<ThingDef>.AllDefsListForReading.AddRange(new[]{wheat,rice,corn});
var meat=new ThingDef{defName="Meat_Test"};var thirdParty=new ThingDef{defName="RC2_Flour"};
DefDatabase<ThingDef>.AllDefsListForReading.AddRange(new[]{meat,thirdParty});
// A non-flour slot comes first so the adapter has to locate the flour slot itself.
var recipe=new RecipeDef{defName="VCE_CookBakeFine",ingredients=new(){new(),new()}};
DefDatabase<RecipeDef>.AllDefsListForReading.Add(recipe);
var other=recipe.ingredients[0];var flourSlot=recipe.ingredients[1];
other.filter.SetAllow(meat,true);flourSlot.filter.customSummary="grain flour";
foreach(var d in new[]{wheat,rice,corn}){flourSlot.filter.SetAllow(d,true);recipe.fixedIngredientFilter.SetAllow(d,true);recipe.defaultIngredientFilter.SetAllow(d,true);}
void Assert(bool b){if(!b)throw new Exception("Adapter assertion failed");}
if(args.Contains("--missing-def")) {
 DefDatabase<ThingDef>.AllDefsListForReading.Remove(rice);
 RuntimeHelpers.RunClassConstructor(typeof(Bootstrap).TypeHandle);
 Assert(Harmony.GetPatchInfo(FlourSelectionPatch.ResolveTarget())==null);
 // Rice's def is the injected failure, so only cornmeal can be looked up and removed.
 Assert(flourSlot.filter.Allowed.Contains(wheat)&&!flourSlot.filter.Allowed.Contains(corn)&&flourSlot.filter.customSummary==null&&other.filter.Allowed.SetEquals(new[]{meat}));
 // fixed/default filters keep the added flours so saving bills cannot strip the player's allowances.
 Assert(recipe.fixedIngredientFilter.Allowed.Contains(corn)&&recipe.defaultIngredientFilter.Allowed.Contains(corn));
 Console.WriteLine("PASS fail-closed initialization keeps wheat and saved allowances");return;
}
if(args.Contains("--missing-wheat")) {
 DefDatabase<ThingDef>.AllDefsListForReading.Remove(wheat);
 RuntimeHelpers.RunClassConstructor(typeof(Bootstrap).TypeHandle);
 Assert(!flourSlot.filter.Allowed.Contains(rice)&&!flourSlot.filter.Allowed.Contains(corn)&&recipe.fixedIngredientFilter.Allowed.Contains(corn));
 Console.WriteLine("PASS fail-closed initialization without wheat flour clears added flours from all slots");return;
}
var plantFood=new SpecialThingFilterDef{defName="AllowPlantFood"};
var anotherRestriction=new SpecialThingFilterDef{defName="OtherRestriction"};
DefDatabase<SpecialThingFilterDef>.AllDefsListForReading.AddRange(new[]{plantFood,anotherRestriction});
var targetRecipes=new[]{recipe}.Concat(new[]{"VCE_CookBakeSimple","VCE_CookBakeSimpleBulk","VCE_CookBakeFineBulk","VCE_CookBakeLavish","VCE_CookBakeLavishBulk","VCE_CookBakeGourmet"}.Select(name=>new RecipeDef{defName=name})).ToArray();
var unrelatedRecipe=new RecipeDef{defName="OtherBake"};
DefDatabase<RecipeDef>.AllDefsListForReading.AddRange(targetRecipes.Skip(1).Append(unrelatedRecipe));
foreach(var r in targetRecipes.Append(unrelatedRecipe)) {
 r.fixedIngredientFilter.SetAllow(plantFood,false);
 r.fixedIngredientFilter.SetAllow(anotherRestriction,false);
}
RuntimeHelpers.RunClassConstructor(typeof(Bootstrap).TypeHandle);
Assert(targetRecipes.All(r=>!r.fixedIngredientFilter.DisallowedSpecialFilters.Contains(plantFood)));
Assert(targetRecipes.All(r=>r.fixedIngredientFilter.DisallowedSpecialFilters.Contains(anotherRestriction)));
Assert(unrelatedRecipe.fixedIngredientFilter.DisallowedSpecialFilters.Contains(plantFood));
Console.WriteLine("PASS all seven target filters allow plant flour while retaining other restrictions and unrelated recipes");
Assert(Harmony.GetPatchInfo(FlourSelectionPatch.ResolveTarget()).Owners.Contains(Bootstrap.HarmonyId));
var bill=new Bill{recipe=recipe};var chosen=new List<ThingCount>();var missing=new List<IngredientCount>();
Thing T(ThingDef def,int n)=>new(){def=def,stackCount=n};
bool Run(params Thing[] input)=>WorkGiver_DoBill.TryFindBestBillIngredientsInSet_AllowMix(input.ToList(),bill,chosen,new(),missing);
if(args.Contains("--injected-flour")) {
 WorkGiver_DoBill.Inject=T(rice,10);
 Assert(!Run(T(wheat,10)) && chosen.Count==0);
 Console.WriteLine("PASS foreign solver cannot introduce another flour type");return;
}
if(args.Contains("--clone-prefix")) {
 new Harmony("test.candidate-copy").Patch(FlourSelectionPatch.ResolveTarget(),prefix:new HarmonyMethod(typeof(ClonePrefix),nameof(ClonePrefix.Before)){priority=Priority.First});
 Assert(Run(T(wheat,10)) && ClonePrefix.Calls==2);
 Console.WriteLine("PASS candidate-list replacement cannot cause recursive patch calls");return;
}
Assert(!Run(T(wheat,5),T(rice,5))&&chosen.Count==0&&missing.Count==1);Console.WriteLine("PASS Harmony mixed shortage and diagnostics");
Assert(Run(T(rice,4),T(rice,6))&&chosen.Count==2&&missing.Count==0);Console.WriteLine("PASS Harmony split stacks");
bill.Forbidden=wheat.defName;Assert(Run(T(wheat,10),T(corn,10))&&chosen.Single().Thing.def==corn);bill.Forbidden=null;Console.WriteLine("PASS Harmony retry and bill settings");
recipe.defName="OtherBake";Assert(Run(T(wheat,5),T(rice,5)));recipe.defName="VCE_CookBakeFine";Console.WriteLine("PASS Harmony unrelated recipes bypass");
recipe.allowMixingIngredients=false;Assert(Run(T(wheat,5),T(rice,5)));recipe.allowMixingIngredients=true;Console.WriteLine("PASS adapter leaves no-mix recipes to native dispatch");
WorkGiver_DoBill.Throw=true;try{Run(T(wheat,10));throw new Exception("swallowed");}catch(InvalidOperationException){}finally{WorkGiver_DoBill.Throw=false;}
Assert(!Run(T(wheat,5),T(rice,5)));Console.WriteLine("PASS exception guard restoration");
flourSlot.filter.SetAllow(thirdParty,true);
Assert(!Run(T(wheat,5),T(thirdParty,5))&&chosen.Count==0);
Assert(Run(T(thirdParty,4),T(thirdParty,6))&&chosen.All(c=>c.Thing.def==thirdParty));
flourSlot.filter.SetAllow(thirdParty,false);Console.WriteLine("PASS flour added to the slot by another mod is its own kind");
flourSlot.filter.SetAllow(wheat,false);Assert(Run(T(rice,5),T(corn,5)));flourSlot.filter.SetAllow(wheat,true);
Console.WriteLine("PASS recipe without a wheat flour slot is left to the original solver");
Console.WriteLine("9 adapter scenarios passed using real Harmony 3 and a boundary double, NOT RimWorld runtime.");

static class ClonePrefix {
 public static int Calls;
 public static void Before(ref List<Thing> availableThings) {
  if(++Calls>8)throw new InvalidOperationException("Unbounded patch recursion");
  availableThings=new List<Thing>(availableThings);
 }
}
