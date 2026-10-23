using HarmonyLib;
using RimWorld;
using Verse;
using VCEFlourAlternatives;
using System.Runtime.CompilerServices;

var wheat=new ThingDef{defName="VCE_Flour"};var rice=new ThingDef{defName="PFA_RiceFlour"};var corn=new ThingDef{defName="PFA_Cornmeal"};
DefDatabase<ThingDef>.AllDefsListForReading.AddRange(new[]{wheat,rice,corn});
var recipe=new RecipeDef{defName="VCE_CookBakeFine"};
DefDatabase<RecipeDef>.AllDefsListForReading.Add(recipe);
foreach(var d in new[]{wheat,rice,corn}){recipe.ingredients[0].filter.SetAllow(d,true);recipe.fixedIngredientFilter.SetAllow(d,true);recipe.defaultIngredientFilter.SetAllow(d,true);}
void Assert(bool b){if(!b)throw new Exception("Adapter assertion failed");}
if(args.Contains("--missing-def")) {
 DefDatabase<ThingDef>.AllDefsListForReading.Remove(wheat);
 RuntimeHelpers.RunClassConstructor(typeof(Bootstrap).TypeHandle);
 Assert(!recipe.ingredients[0].filter.Allowed.Contains(rice)&&!recipe.fixedIngredientFilter.Allowed.Contains(corn));
 Console.WriteLine("PASS fail-closed initialization");return;
}
RuntimeHelpers.RunClassConstructor(typeof(Bootstrap).TypeHandle);
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
Console.WriteLine("6 adapter scenarios passed using real Harmony 3 and a boundary double, NOT RimWorld runtime.");

static class ClonePrefix {
 public static int Calls;
 public static void Before(ref List<Thing> availableThings) {
  if(++Calls>8)throw new InvalidOperationException("Unbounded patch recursion");
  availableThings=new List<Thing>(availableThings);
 }
}
