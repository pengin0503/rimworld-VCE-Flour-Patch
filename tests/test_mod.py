from pathlib import Path
from lxml import etree
from PIL import Image
import unittest

ROOT=Path(__file__).resolve().parents[1]
MOD=ROOT/'VCE-Flour-Alternatives'

RECIPE_NAMES=['VCE_CookBakeSimple','VCE_CookBakeSimpleBulk','VCE_CookBakeFine','VCE_CookBakeFineBulk','VCE_CookBakeLavish','VCE_CookBakeLavishBulk','VCE_CookBakeGourmet']
# VCE currently has seven public recipe defs: simple/fine/lavish as singles/bulk, plus gourmet.

def fixture():
    root=etree.parse(str(ROOT/'tests/fixtures/Recipes_Bakes.xml')).getroot()
    for name, parent in [('OtherBake','VCE_CookBakeMealBase'),
                         ('VCE_CookBakeThirdParty','VCE_CookGourmetBakeMealBase')]:
        rec=etree.SubElement(root,'RecipeDef',ParentName=parent)
        etree.SubElement(rec,'defName').text=name
        holder=etree.SubElement(etree.SubElement(etree.SubElement(etree.SubElement(rec,'ingredients'),'li'),'filter'),'thingDefs')
        etree.SubElement(holder,'li').text='VCE_Flour'
    return root

def run_add_patch(root,patchroot):
    """Applies the sequence like RimWorld: in order, and an unmatched XPath only fails without success=Always."""
    ops=patchroot.xpath('//Operation[@Class="PatchOperationSequence"]/operations/li')
    assert len(ops)==8, f'Expected 8 patch operations, found {len(ops)}'
    for op in ops:
        assert op.get('Class')=='PatchOperationAdd'
        matches=etree.ElementTree(root).xpath("/" + op.findtext("xpath").lstrip("/"))
        assert matches or op.findtext('success')=='Always', f'Unmatched XPath: {op.findtext("xpath")}'
        for match in matches:
            for node in op.find('value'):
                match.append(etree.fromstring(etree.tostring(node)))

class TestMod(unittest.TestCase):
    def test_xml_parse_and_package_dependency(self):
        for path in MOD.rglob('*.xml'):
            etree.parse(str(path))
        about=etree.parse(str(MOD/'About'/'About.xml'))
        self.assertEqual(about.findtext('.//packageId'),'pengin0503.vce.flouralternatives')
        self.assertIn('VanillaExpanded.VCookE',about.xpath('//modDependencies/li/packageId/text()'))
        self.assertIn('brrainz.harmony',about.xpath('//modDependencies/li/packageId/text()'))
        self.assertEqual(set(about.xpath('//supportedVersions/li/text()')), {'1.6','1.7'})
        # README lists Harmony, Vanilla Expanded Framework and VCE as required, in that load order.
        self.assertEqual(about.xpath('//modDependencies/li/packageId/text()'),
                         ['brrainz.harmony','OskarPotocki.VanillaFactionsExpanded.Core','VanillaExpanded.VCookE'])
        self.assertEqual(about.xpath('//loadAfter/li/text()'),
                         ['brrainz.harmony','OskarPotocki.VanillaFactionsExpanded.Core','VanillaExpanded.VCookE'])
        self.assertEqual(about.findtext('.//author'),'pengin0503')
        self.assertTrue(about.findtext('.//url').startswith('https://github.com/pengin0503/'))
        version=etree.parse(str(ROOT/'Source/VCEFlourAlternatives.csproj')).findtext('.//Version')
        self.assertEqual(about.findtext('.//modVersion'),version)

    def test_about_images(self):
        for name,size in (('Preview.png',(640,360)),('ModIcon.png',(128,128))):
            with Image.open(MOD/'About'/name) as im:
                self.assertEqual(im.size,size)

    def test_images(self):
        for name in ('VCE_FPatch_Rice_Flour.png','VCE_FPatch_Cornmeal.png'):
            path=MOD/'Textures'/'Things'/'Items'/'Raw'/name
            with Image.open(path) as im:
                self.assertEqual(im.size,(128,128))
                self.assertEqual(im.mode,'RGBA')
                self.assertEqual(im.getpixel((0,0))[3],0)

    def test_ingredient_defs(self):
        defs=etree.parse(str(MOD/'Defs'/'ThingDefs'/'Flour.xml'))
        for defname,src in [('PFA_RiceFlour','VCE_FPatch_Rice_Flour'),('PFA_Cornmeal','VCE_FPatch_Cornmeal')]:
            things=defs.xpath(f'/Defs/ThingDef[defName="{defname}"]')
            self.assertEqual(len(things),1)
            thing=things[0]
            self.assertEqual(thing.get('ParentName'),'RoughPlantBase')
            self.assertEqual(thing.findtext('./graphicData/texPath'),f'Things/Items/Raw/{src}')
            self.assertEqual(thing.findtext('./statBases/Nutrition'),'0.05')
            self.assertEqual(thing.findtext('./comps/li/daysToRotStart'),'100')
            self.assertEqual(thing.xpath('./thingCategories/li/text()'), ['PFA_GrainFlour'])

    def test_grain_category_and_wheat_membership(self):
        categories=etree.parse(str(MOD/'Defs/ThingCategoryDefs/GrainFlour.xml'))
        grain=categories.xpath('/Defs/ThingCategoryDef[defName="PFA_GrainFlour"]')
        self.assertEqual(len(grain), 1)
        self.assertEqual(grain[0].findtext('parent'), 'PlantMatter')
        wheat=etree.parse(str(ROOT/'tests/fixtures/Items_RawPlant.xml'))
        op=etree.parse(str(MOD/'Patches/PFA_GrainFlour.xml')).getroot().find('Operation')
        self.assertEqual(op.get('Class'), 'PatchOperationReplace')
        matches=wheat.xpath('/'+op.findtext('xpath'))
        self.assertEqual(len(matches), 1)
        for match in matches:
            match.getparent().replace(match, etree.fromstring(etree.tostring(op.find('value')[0])))
        self.assertEqual(wheat.xpath('/Defs/ThingDef[defName="VCE_Flour"]/thingCategories/li/text()'), ['PFA_GrainFlour'])
        japanese=etree.parse(str(MOD/'Languages/Japanese/DefInjected/ThingCategoryDef/GrainFlour.xml'))
        self.assertEqual(japanese.findtext('PFA_GrainFlour.label'), '穀物粉')

    def test_patches_do_not_toggle_global_mixing(self):
        patch = etree.parse(str(MOD/'Patches'/'VCE_FlourAlternatives.xml'))
        self.assertFalse(patch.xpath('//allowMixingIngredients'))

    def test_gourmet_parent_still_preserves_no_mix(self):
        recipe = fixture()
        run_add_patch(recipe, etree.parse(str(MOD/'Patches'/'VCE_FlourAlternatives.xml')))
        self.assertEqual(recipe.xpath('/Defs/RecipeDef[defName="VCE_CookBakeGourmet"]/allowMixingIngredients/text()'), ['false'])

    def test_patch_existing_recipes_and_filters(self):
        recipes=fixture()
        patch=etree.parse(str(MOD/'Patches'/'VCE_FlourAlternatives.xml'))
        run_add_patch(recipes,patch)
        for node in recipes.xpath('/Defs/RecipeDef[@Abstract="True"]/*[self::fixedIngredientFilter or self::defaultIngredientFilter]/thingDefs'):
            self.assertEqual(node.xpath('./li/text()'), ['VCE_Flour'])
        for name in RECIPE_NAMES:
            rec=recipes.xpath(f'/Defs/RecipeDef[defName="{name}"]')[0]
            node=rec.xpath('./ingredients/li/filter/thingDefs[li="VCE_Flour"]')[0]
            self.assertEqual(node.xpath('./li/text()'), ['VCE_Flour'])
            self.assertEqual(node.getparent().xpath('./categories/li/text()'), ['PFA_GrainFlour'])
            for tag in ('fixedIngredientFilter','defaultIngredientFilter'):
                self.assertEqual(rec.xpath(f'./{tag}/categories/li/text()'), ['PFA_GrainFlour'])
                self.assertFalse(rec.xpath(f'./{tag}/thingDefs'))
        for name in ('OtherBake', 'VCE_CookBakeThirdParty'):
            rec=recipes.xpath(f'/Defs/RecipeDef[defName="{name}"]')[0]
            self.assertEqual(rec.xpath('./ingredients/li/filter/thingDefs/li/text()'), ['VCE_Flour'])
            self.assertFalse(rec.xpath('./fixedIngredientFilter|./defaultIngredientFilter'))

    def test_recipe_with_its_own_filters_keeps_one_merged_filter(self):
        # A later VCE version may write fixed/default filters on the concrete recipe instead of the parent.
        recipes=fixture()
        rec=recipes.xpath('/Defs/RecipeDef[defName="VCE_CookBakeFine"]')[0]
        own=etree.SubElement(rec,'fixedIngredientFilter')
        etree.SubElement(etree.SubElement(own,'categories'),'li').text='FoodRaw'
        etree.SubElement(etree.SubElement(rec,'defaultIngredientFilter'),'thingDefs')
        run_add_patch(recipes, etree.parse(str(MOD/'Patches/VCE_FlourAlternatives.xml')))
        for tag,expected in (('fixedIngredientFilter',['FoodRaw','PFA_GrainFlour']),('defaultIngredientFilter',['PFA_GrainFlour'])):
            self.assertEqual(len(rec.findall(tag)),1)
            self.assertEqual(rec.xpath(f'./{tag}/categories/li/text()'),expected)
        for name in RECIPE_NAMES:
            if name=='VCE_CookBakeFine': continue
            other=recipes.xpath(f'/Defs/RecipeDef[defName="{name}"]')[0]
            for tag in ('fixedIngredientFilter','defaultIngredientFilter'):
                self.assertEqual(other.xpath(f'./{tag}/categories/li/text()'),['PFA_GrainFlour'])

    def test_flour_slot_named_grain_flour(self):
        recipes=fixture()
        run_add_patch(recipes, etree.parse(str(MOD/'Patches/VCE_FlourAlternatives.xml')))
        for name in RECIPE_NAMES:
            rec=recipes.xpath(f'/Defs/RecipeDef[defName="{name}"]')[0]
            for li in rec.xpath('./ingredients/li'):
                expected=['grain flour'] if li.xpath('./filter/thingDefs/li[.="VCE_Flour"]') else []
                self.assertEqual(li.xpath('./filter/customSummary/text()'), expected)
        for name in ('OtherBake', 'VCE_CookBakeThirdParty'):
            self.assertFalse(recipes.xpath(f'/Defs/RecipeDef[defName="{name}"]//customSummary'))

    def test_keyed_translations_match_and_slot_label_has_no_index(self):
        # The slot label is applied in code to the slot that accepts wheat, never by ingredient index.
        self.assertFalse((MOD/'Languages/Japanese/DefInjected/RecipeDef/VCE_Bakes.xml').exists())
        keys={}
        for language in ('English','Japanese'):
            root=etree.parse(str(MOD/f'Languages/{language}/Keyed/VCEFlourAlternatives.xml')).getroot()
            keys[language]={e.tag:e.text for e in root if isinstance(e.tag,str)}
        self.assertEqual(set(keys['English']),set(keys['Japanese']))
        self.assertEqual(keys['English']['PFA_GrainFlourSlot'],'grain flour')
        self.assertEqual(keys['Japanese']['PFA_GrainFlourSlot'],'穀物粉')
        for key in ('PFA_MigrateBillsText','PFA_MigrateBillsDone'):
            for language in keys:
                self.assertIn('{0}',keys[language][key])

    def test_upstream_costs_and_other_slots_unchanged(self):
        recipes=fixture()
        original=etree.tostring(recipes)
        run_add_patch(recipes, etree.parse(str(MOD/'Patches/VCE_FlourAlternatives.xml')))
        for node in recipes.xpath('//filter/categories[li="PFA_GrainFlour"]'):
            node.getparent().remove(node)
        for node in list(recipes.xpath('/Defs/RecipeDef[defName]/*[self::fixedIngredientFilter or self::defaultIngredientFilter]|//filter/customSummary')):
            node.getparent().remove(node)
        # Added elements have formatting whitespace; compare normalized element trees.
        parser=etree.XMLParser(remove_blank_text=True)
        self.assertEqual(etree.tostring(etree.fromstring(etree.tostring(recipes),parser)),
                         etree.tostring(etree.fromstring(original,parser)))

    def test_flour_balance_matches_upstream(self):
        original=etree.parse(str(ROOT/'tests/fixtures/Items_RawPlant.xml')).getroot()[0]
        for thing in etree.parse(str(MOD/'Defs/ThingDefs/Flour.xml')).xpath('/Defs/ThingDef'):
            self.assertEqual(thing.get('ParentName'), original.get('ParentName'))
            for field in ('statBases/Nutrition','statBases/MarketValue','statBases/Flammability',
                          'comps/li/daysToRotStart','ingestible/preferability'):
                self.assertEqual(thing.findtext(field), original.findtext(field))

if __name__=='__main__': unittest.main()

