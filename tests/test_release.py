import importlib.util
from pathlib import Path
import hashlib
import shutil
import subprocess
import tempfile
import unittest
import zipfile

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('check_release',ROOT/'tools/check_release.py')
check_release=importlib.util.module_from_spec(spec)
spec.loader.exec_module(check_release)
MOD='VCE-Flour-Alternatives'

class ReleaseTests(unittest.TestCase):
    def test_tag_format(self):
        self.assertEqual(check_release.parse_tag('v0.2.0'),('0.2.0',False))
        self.assertEqual(check_release.parse_tag('v0.2.0-beta.1'),('0.2.0',True))
        for bad in ('0.2.0','v0.2','v0.2.0/x','',None):
            with self.assertRaises(ValueError): check_release.parse_tag(bad)

    def repo(self,root,version='0.2.0',about='0.2.0',notes=('v0.2.0',)):
        (root/'Source').mkdir();(root/MOD/'About').mkdir(parents=True);(root/'.github/releases').mkdir(parents=True)
        (root/'Source/VCEFlourAlternatives.csproj').write_text(f'<Project><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>')
        (root/MOD/'About/About.xml').write_text(f'<ModMetaData><modVersion>{about}</modVersion></ModMetaData>')
        for tag in notes: (root/'.github/releases'/f'{tag}.md').write_text('notes')

    def test_versions_must_agree_and_notes_exist(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);self.repo(root)
            self.assertEqual(check_release.check_versions('v0.2.0',root),('0.2.0',False))
            with self.assertRaises(ValueError): check_release.check_versions('v0.3.0',root)
            with self.assertRaises(FileNotFoundError): check_release.check_versions('v0.2.0-beta.1',root)
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);self.repo(root,about='0.1.0')
            with self.assertRaises(ValueError): check_release.check_versions('v0.2.0',root)

    def test_current_source_versions_agree(self):
        version=check_release.ET.parse(ROOT/'Source/VCEFlourAlternatives.csproj').findtext('.//Version')
        self.assertEqual(check_release.ET.parse(ROOT/MOD/'About/About.xml').findtext('modVersion'),version)
        self.assertIn(f'v{version}',(ROOT/MOD/'README.md').read_text(encoding='utf-8').splitlines()[0])

    def zip_repo(self,root,extra=None,changed=None):
        subprocess.run(['git','init','-q',str(root)],check=True)
        files={f'{MOD}/About/About.xml':b'<a/>',f'{MOD}/README.md':b'readme',f'{MOD}/Defs/d.xml':b'<d/>'}
        for name,data in files.items():
            (root/name).parent.mkdir(parents=True,exist_ok=True);(root/name).write_bytes(data)
        subprocess.run(['git','add','.'],cwd=root,check=True)
        dll=b'MZ'+b'\0'*100
        archive=root/'out.zip'
        with zipfile.ZipFile(archive,'w') as z:
            for name,data in files.items(): z.writestr(name,changed if changed and name.endswith('d.xml') else data)
            for name in check_release.DLLS: z.writestr(name,dll)
            if extra: z.writestr(extra,b'x')
        manifest={'zip_sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),
                  'builds':[{'game':g,'sha256':hashlib.sha256(dll).hexdigest()} for g in ('1.6.4871','1.7.4977-alpha.1')]}
        return archive,manifest

    @unittest.skipUnless(shutil.which('git'),'git required')
    def test_zip_accepts_exact_runtime_content(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);archive,manifest=self.zip_repo(root)
            check_release.check_zip(archive,manifest,root)

    @unittest.skipUnless(shutil.which('git'),'git required')
    def test_zip_rejects_extra_changed_or_unrecorded_content(self):
        for kwargs in ({'extra':f'{MOD}/Source/x.cs'},{'changed':b'<other/>'}):
            with tempfile.TemporaryDirectory() as tmp:
                root=Path(tmp);archive,manifest=self.zip_repo(root,**kwargs)
                with self.assertRaises(ValueError): check_release.check_zip(archive,manifest,root)
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);archive,manifest=self.zip_repo(root);manifest['zip_sha256']='0'*64
            with self.assertRaises(ValueError): check_release.check_zip(archive,manifest,root)

if __name__=='__main__': unittest.main()
