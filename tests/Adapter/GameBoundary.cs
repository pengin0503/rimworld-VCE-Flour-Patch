// Minimal API-shaped boundary double. NOT a copy of the RimWorld solver.
using System.Runtime.CompilerServices;
namespace Verse {
 public class StaticConstructorOnStartup:Attribute {}
 public class SpecialThingFilterDef {public string defName;}
 public class ThingDef {public string defName;}
 public class Thing {public ThingDef def;public int stackCount;}
 public struct ThingCount {public Thing Thing; public int Count;public ThingCount(Thing t,int n){Thing=t;Count=n;}}
 public struct IntVec3 {}
 public class IngredientCount {public ThingFilter filter=new();}
 public class ThingFilter {public HashSet<SpecialThingFilterDef> DisallowedSpecialFilters=new();public void SetAllow(SpecialThingFilterDef d,bool allow){if(allow)DisallowedSpecialFilters.Remove(d);else DisallowedSpecialFilters.Add(d);}public HashSet<ThingDef> Allowed=new();public string customSummary;public bool Allows(ThingDef d)=>Allowed.Contains(d);public void SetAllow(ThingDef d,bool allow){if(allow)Allowed.Add(d);else Allowed.Remove(d);}}
 public class RecipeDef {public string defName;public bool allowMixingIngredients=true;public List<IngredientCount> ingredients=new(){new()};public ThingFilter fixedIngredientFilter=new();public ThingFilter defaultIngredientFilter=new();}
 public static class DefDatabase<T> where T:class {public static List<T> AllDefsListForReading=new();public static T GetNamedSilentFail(string name)=>AllDefsListForReading.FirstOrDefault(x=>(string)x.GetType().GetField("defName").GetValue(x)==name);}
 public static class Log {public static void Error(string s)=>Console.WriteLine(s); public static void Message(string s)=>Console.WriteLine(s);}
}
namespace RimWorld {
 using Verse;
 public class Bill {public RecipeDef recipe;public string Forbidden;}
 public class WorkGiver_DoBill {
  public static int Calls;public static bool Throw;public static Thing Inject;
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static bool TryFindBestBillIngredientsInSet_AllowMix(List<Thing> availableThings,Bill bill,List<ThingCount> chosen,IntVec3 rootCell,List<IngredientCount> missingIngredients) {
   Calls++; if(Throw)throw new InvalidOperationException("solver error");
   chosen.Clear();missingIngredients?.Clear();
   if(Inject!=null)availableThings.Add(Inject);
   var flour=availableThings.Where(x=>bill.recipe.ingredients[^1].filter.Allows(x.def)&&x.def.defName!=bill.Forbidden).ToList();
   if(flour.Sum(x=>x.stackCount)<10){missingIngredients?.Add(bill.recipe.ingredients[^1]);return false;}
   chosen.AddRange(flour.Select(x=>new ThingCount(x,x.stackCount)));return true;
  }
 }
}
