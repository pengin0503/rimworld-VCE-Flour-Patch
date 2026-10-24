from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
MOD = ROOT / 'VCE-Flour-Alternatives'
EXPECTED = {
    'PFA_MillRice': ('RawRice', 'PFA_RiceFlour', 20, 600, ' x2'),
    'PFA_MillRiceBulk': ('RawRice', 'PFA_RiceFlour', 80, 2400, ' x8'),
    'PFA_MillCorn': ('RawCorn', 'PFA_Cornmeal', 20, 600, ' x2'),
    'PFA_MillCornBulk': ('RawCorn', 'PFA_Cornmeal', 80, 2400, ' x8'),
}

class MillingTests(unittest.TestCase):
    def recipes(self):
        nodes = ET.parse(MOD / 'Defs/RecipeDefs/Milling.xml').getroot().findall('RecipeDef')
        recipes = {node.findtext('defName'): node for node in nodes}
        self.assertEqual(len(nodes), 4)
        self.assertEqual(set(recipes), set(EXPECTED))
        return recipes

    def test_two_batch_sizes_scale_ingredients_products_and_work(self):
        for name, recipe in self.recipes().items():
            source, product, count, work, _ = EXPECTED[name]
            with self.subTest(recipe=name):
                self.assertEqual(len(recipe.findall('ingredients/li')), 1)
                self.assertEqual(recipe.findtext('ingredients/li/filter/thingDefs/li'), source)
                self.assertEqual(recipe.findtext('ingredients/li/count'), str(count))
                self.assertEqual([(p.tag, p.text) for p in recipe.find('products')], [(product, str(count))])
                self.assertEqual(recipe.findtext('workAmount'), str(work))

    def test_batches_keep_stoves_cooking_and_source_filters(self):
        for name, recipe in self.recipes().items():
            source = EXPECTED[name][0]
            with self.subTest(recipe=name):
                self.assertEqual([n.text for n in recipe.findall('recipeUsers/li')], ['FueledStove', 'ElectricStove'])
                self.assertEqual(recipe.findtext('workSkill'), 'Cooking')
                self.assertEqual(recipe.findtext('workSpeedStat'), 'CookSpeed')
                for tag in ('fixedIngredientFilter', 'defaultIngredientFilter'):
                    self.assertEqual([n.text for n in recipe.findall(tag + '/thingDefs/li')], [source])

    def test_japanese_labels_and_descriptions_match_both_batches(self):
        recipes = self.recipes()
        language = ET.parse(MOD / 'Languages/Japanese/DefInjected/RecipeDef/Milling.xml').getroot()
        entries = {node.tag: node.text for node in language}
        self.assertEqual(len(entries), len(language))
        self.assertEqual(set(entries), {name + '.' + field for name in EXPECTED for field in ('label', 'description', 'jobString')})
        for name, (_, _, count, _, label) in EXPECTED.items():
            with self.subTest(recipe=name):
                self.assertIn(label, entries[name + '.label'])
                self.assertEqual(entries[name + '.description'].count(str(count) + '個'), 2)
                self.assertTrue(entries[name + '.jobString'])
                self.assertIn('x2' if count == 20 else 'x8', recipes[name].findtext('label'))

if __name__ == '__main__':
    unittest.main()

