using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VCEFlourAlternatives
{
    // Saved bills only know the flours that existed when they were saved, so bake bills made before
    // this mod allow wheat flour but not the added flours. Offers, once per save, to allow them too.
    public class GameComponent_FlourBillMigration : GameComponent
    {
        private bool migrated;

        public GameComponent_FlourBillMigration(Game game)
        {
        }

        public override void StartedNewGame() => migrated = true;

        public override void LoadedGame()
        {
            if (migrated || !FlourSelectionPatch.Installed) return;
            migrated = true;
            List<Bill> bills = BillsToMigrate();
            if (bills.Count == 0) return;
            Find.WindowStack.Add(new Dialog_MessageBox(
                "PFA_MigrateBillsText".Translate(bills.Count),
                "PFA_MigrateBillsConfirm".Translate(), () => Migrate(bills),
                "PFA_MigrateBillsSkip".Translate(), null,
                "PFA_MigrateBillsTitle".Translate()));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref migrated, "migrated", false);
        }

        internal static List<Bill> BillsToMigrate()
        {
            var result = new List<Bill>();
            ThingDef wheat = DefDatabase<ThingDef>.GetNamedSilentFail(FlourPolicy.WheatFlour);
            List<ThingDef> added = AddedFlourDefs();
            if (wheat == null || added.Count == 0) return result;
            var allows = new List<bool>(added.Count);
            foreach (Bill bill in BillUtility.GlobalBills())
            {
                if (bill?.recipe == null || bill.ingredientFilter == null || !FlourPolicy.IsTarget(bill.recipe.defName)) continue;
                allows.Clear();
                foreach (ThingDef flour in added) allows.Add(bill.ingredientFilter.Allows(flour));
                if (FlourPolicy.NeedsAddedFlours(bill.ingredientFilter.Allows(wheat), allows)) result.Add(bill);
            }
            return result;
        }

        internal static void Migrate(List<Bill> bills)
        {
            List<ThingDef> added = AddedFlourDefs();
            foreach (Bill bill in bills)
                foreach (ThingDef flour in added)
                    bill.ingredientFilter.SetAllow(flour, true);
            Messages.Message("PFA_MigrateBillsDone".Translate(bills.Count), MessageTypeDefOf.PositiveEvent, false);
        }

        private static List<ThingDef> AddedFlourDefs()
        {
            var defs = new List<ThingDef>();
            foreach (string name in FlourPolicy.AddedFlours)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def != null) defs.Add(def);
            }
            return defs;
        }
    }
}
