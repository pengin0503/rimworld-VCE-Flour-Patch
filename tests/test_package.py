import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('package_mod',ROOT/'tools/package_mod.py')
package_mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(package_mod)

class PackageTests(unittest.TestCase):
    def inputs(self, root):
        mod=root/'mod'
        for name in ['About/About.xml','Defs/thing.xml','Patches/patch.xml','Textures/test.png','Languages/Japanese/a.xml','loadFolders.xml','README.md','Source/bad.cs','tests/bad.py','Assemblies/Assembly-CSharp.dll']:
            path=mod/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(b'data')
        dlls={}
        for version in ['1.6','1.7']:
            path=root/(version+'.dll');path.write_bytes(b'MZ'+b'\0'*100);dlls[version]=path
        return mod,dlls
    def test_missing_version_never_creates_archive(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);mod,dlls=self.inputs(root);dlls['1.7'].unlink()
            with self.assertRaises((FileNotFoundError,ValueError)):
                package_mod.package(mod,dlls,root/'out.zip')
            self.assertFalse((root/'out.zip').exists())
    def test_only_runtime_content_and_two_own_dlls(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);mod,dlls=self.inputs(root);out=root/'out.zip'
            package_mod.package(mod,dlls,out)
            with zipfile.ZipFile(out) as z:
                self.assertEqual(set(z.namelist()),{
                    'VCE-Flour-Alternatives/'+x for x in ['About/About.xml','Defs/thing.xml','Patches/patch.xml','Textures/test.png','Languages/Japanese/a.xml','loadFolders.xml','README.md','1.6/Assemblies/VCEFlourAlternatives.dll','1.7/Assemblies/VCEFlourAlternatives.dll']})
            first=out.read_bytes();package_mod.package(mod,dlls,out)
            self.assertEqual(first,out.read_bytes())
    def test_invalid_binary_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);mod,dlls=self.inputs(root);dlls['1.6'].write_bytes(b'not an assembly')
            with self.assertRaises(ValueError): package_mod.package(mod,dlls,root/'out.zip')
    def test_symlink_cannot_add_files_outside_mod(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);mod,dlls=self.inputs(root)
            (mod/'Defs/leak.xml').symlink_to(root/'1.6.dll')
            with self.assertRaises(ValueError): package_mod.package(mod,dlls,root/'out.zip')
