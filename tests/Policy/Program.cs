using VCEFlourAlternatives;
using System.Linq;

int passed=0;
void Check(string name, Action test) { test(); passed++; Console.WriteLine("PASS "+name); }
void Assert(bool value) { if(!value) throw new Exception("Assertion failed"); }
Stack S(string kind,int count=10)=>new(kind,count);
// The adapter derives flour kinds from the recipe's flour slot; this stands in for that slot.
bool IsFlour(string kind)=>kind is "VCE_Flour" or "PFA_RiceFlour" or "PFA_Cornmeal";
bool Choose(List<Stack> input, List<Stack> chosen, int need=10, string forbidden=null, int meat=0) =>
 FlourPolicy.TryChoose(input, x=>IsFlour(x.Kind)?x.Kind:null, (items,result)=> {
  // Boundary double for the unavailable game solver: actual policy controls its inputs.
  var flour=items.Where(x=>IsFlour(x.Kind)&&x.Kind!=forbidden).ToList();
  if(flour.Sum(x=>x.Count)<need || items.Where(x=>x.Kind.StartsWith("meat")).Sum(x=>x.Count)<meat) return false;
  result.AddRange(flour);result.AddRange(items.Where(x=>!IsFlour(x.Kind)));return true;
 },chosen);
foreach(var kind in new[]{"VCE_Flour","PFA_RiceFlour","PFA_Cornmeal"})
 Check("single "+kind,()=>{var c=new List<Stack>();Assert(Choose(new(){S(kind)},c)&&c.Single().Kind==kind);});
Check("mixed shortage rejected",()=>{var c=new List<Stack>();Assert(!Choose(new(){S("VCE_Flour",5),S("PFA_RiceFlour",5)},c)&&c.Count==0);});
Check("split same-type stacks",()=>{var c=new List<Stack>();Assert(Choose(new(){S("PFA_RiceFlour",4),S("PFA_RiceFlour",6)},c)&&c.Count==2);});
Check("insufficient first type retries",()=>{var c=new List<Stack>();Assert(Choose(new(){S("VCE_Flour",5),S("PFA_Cornmeal")},c)&&c.Single().Kind=="PFA_Cornmeal");});
Check("bill-forbidden type retries",()=>{var c=new List<Stack>();Assert(Choose(new(){S("VCE_Flour"),S("PFA_RiceFlour")},c,forbidden:"VCE_Flour")&&c.Single().Kind=="PFA_RiceFlour");});
Check("all forbidden fails",()=>{var c=new List<Stack>();Assert(!Choose(new(){S("VCE_Flour")},c,forbidden:"VCE_Flour"));});
Check("other ingredient mixing retained",()=>{var c=new List<Stack>();Assert(Choose(new(){S("VCE_Flour"),S("meatA",3),S("meatB",7)},c,meat:10)&&c.Count==3);});
Check("other ingredient shortage",()=>{var c=new List<Stack>();Assert(!Choose(new(){S("VCE_Flour"),S("meatA",3)},c,meat:10));});
Check("original list never mutated",()=>{var original=new List<Stack>{S("VCE_Flour"),S("PFA_RiceFlour")};var c=new List<Stack>();FlourPolicy.TryChoose(original,x=>x.Kind,(a,b)=>{a.Clear();return false;},c);Assert(original.Count==2);});
Check("failed attempt output discarded",()=>{var c=new List<Stack>{S("stale")};int n=0;bool ok=FlourPolicy.TryChoose(new List<Stack>{S("VCE_Flour"),S("PFA_RiceFlour")},x=>x.Kind,(a,b)=>{b.Add(a[0]);return ++n==2;},c);Assert(ok&&c.Count==1&&c[0].Kind=="PFA_RiceFlour");});
Check("no flour still delegates for diagnostics",()=>{int n=0;var c=new List<Stack>();Assert(!FlourPolicy.TryChoose(new List<Stack>{S("meatA")},x=>(string)null,(a,b)=>{n++;return false;},c)&&n==1);});
Check("solver exception propagates without publishing",()=>{var c=new List<Stack>();try {FlourPolicy.TryChoose(new List<Stack>{S("VCE_Flour")},x=>x.Kind,(a,b)=>{b.Add(a[0]);throw new InvalidOperationException();},c);throw new Exception("exception swallowed");}catch(InvalidOperationException){Assert(c.Count==0);}});
Check("exact recipe scope",()=>{Assert(FlourPolicy.IsTarget("VCE_CookBakeFine"));Assert(!FlourPolicy.IsTarget("VCE_CookBakeThirdParty"));Assert(!FlourPolicy.IsTarget("CookMealSimple"));Assert(!FlourPolicy.IsTarget(null));});
Check("migration only adds flours to bills that allow wheat",()=>{
 Assert(FlourPolicy.NeedsAddedFlours(true,new[]{false,false}));Assert(FlourPolicy.NeedsAddedFlours(true,new[]{true,false}));
 Assert(!FlourPolicy.NeedsAddedFlours(true,new[]{true,true}));Assert(!FlourPolicy.NeedsAddedFlours(false,new[]{false,false}));});
Check("flour slot diagnostics",()=>{Assert(FlourPolicy.SlotProblem(true,1)==null);Assert(FlourPolicy.SlotProblem(false,0)=="recipe not found");
 Assert(FlourPolicy.SlotProblem(true,0).Contains("no ingredient slot"));Assert(FlourPolicy.SlotProblem(true,2).StartsWith("2 "));});
Check("target list matches the seven VCE recipes",()=>{Assert(FlourPolicy.TargetRecipes.Length==7&&FlourPolicy.TargetRecipes.Distinct().Count()==7&&FlourPolicy.TargetRecipes.All(FlourPolicy.IsTarget));});
Console.WriteLine($"{passed} policy tests passed; game solver itself is NOT tested here.");
record Stack(string Kind,int Count);
