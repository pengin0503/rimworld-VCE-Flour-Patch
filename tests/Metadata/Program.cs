using Mono.Cecil;
if(args.Length!=3)throw new ArgumentException("game reference, mod DLL, game version required");
using var game=ModuleDefinition.ReadModule(args[0]);
using var mod=ModuleDefinition.ReadModule(args[1]);
var type=game.Types.Single(t=>t.FullName=="RimWorld.WorkGiver_DoBill");
var method=type.Methods.Single(m=>m.Name=="TryFindBestBillIngredientsInSet_AllowMix");
string[] types={"System.Collections.Generic.List`1<Verse.Thing>","RimWorld.Bill","System.Collections.Generic.List`1<Verse.ThingCount>","Verse.IntVec3","System.Collections.Generic.List`1<Verse.IngredientCount>"};
string[] names={"availableThings","bill","chosen","rootCell","missingIngredients"};
if(!method.IsStatic || method.ReturnType.FullName!="System.Boolean" ||
 !method.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(types) ||
 !method.Parameters.Select(p=>p.Name).SequenceEqual(names))throw new Exception("Unsupported private solver signature");
int major=args[2]=="1.6"?2:3;
if(mod.AssemblyReferences.Single(r=>r.Name=="0Harmony").Version.Major!=major)throw new Exception("Wrong Harmony major");
string framework=(string)mod.Assembly.CustomAttributes.Single(a=>a.AttributeType.FullName=="System.Runtime.Versioning.TargetFrameworkAttribute").ConstructorArguments[0].Value;
if(framework!=(args[2]=="1.6"?".NETFramework,Version=v4.7.2":".NETCoreApp,Version=v10.0"))throw new Exception("Wrong target framework");
if(mod.Assembly.CustomAttributes.Any(a=>a.AttributeType.Name=="ReferenceAssemblyAttribute"))throw new Exception("Reference assembly cannot be distributed");
if(!mod.Types.Any(t=>t.FullName=="VCEFlourAlternatives.FlourSelectionPatch"))throw new Exception("Missing patch type");
Console.WriteLine($"PASS {args[2]}: exact solver signature, Harmony {major}, {framework}, executable mod assembly");
