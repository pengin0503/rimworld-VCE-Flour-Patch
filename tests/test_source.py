"""Loading layout checks; former source-string assertions replaced by executable C# tests."""
from pathlib import Path
from lxml import etree
import unittest
ROOT=Path(__file__).resolve().parents[1]
class TestLayout(unittest.TestCase):
    def test_versioned_assembly_loading(self):
        mod=ROOT/'VCE-Flour-Alternatives'
        xml=etree.parse(str(mod/'loadFolders.xml'))
        for version in ('1.6','1.7'):
            self.assertEqual(xml.xpath(f'/loadFolders/v{version}/li/text()'), ['/',version])
        self.assertFalse((mod/'Assemblies').exists())
    def test_development_code_is_outside_mod(self):
        mod=ROOT/'VCE-Flour-Alternatives'
        self.assertFalse(list(mod.rglob('*.cs')))
        self.assertFalse(list(mod.rglob('*.csproj')))
        self.assertTrue((ROOT/'Source/FlourSelectionPatch.cs').is_file())
