"""Checks a release tag against the source version and the built ZIP before publishing."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import zipfile
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
MOD='VCE-Flour-Alternatives'
DLLS={f'{MOD}/{v}/Assemblies/VCEFlourAlternatives.dll' for v in ('1.6','1.7')}
TAG=re.compile(r'v(\d+\.\d+\.\d+)(-[0-9A-Za-z.]+)?')

def parse_tag(tag):
    """Returns (version, prerelease) for tags like v0.2.0 or v0.2.0-beta.1."""
    match=TAG.fullmatch(tag or '')
    if not match: raise ValueError(f'Release tag must look like v1.2.3 or v1.2.3-beta.1: {tag!r}')
    return match.group(1),match.group(2) is not None

def check_versions(tag,root=ROOT):
    version,prerelease=parse_tag(tag)
    csproj=ET.parse(root/'Source/VCEFlourAlternatives.csproj').findtext('.//Version')
    about=ET.parse(root/MOD/'About/About.xml').findtext('modVersion')
    if csproj!=version or about!=version:
        raise ValueError(f'Tag {tag} does not match csproj Version {csproj} and About modVersion {about}')
    notes=root/'.github/releases'/f'{tag}.md'
    if not notes.is_file(): raise FileNotFoundError(f'Release notes missing: {notes.relative_to(root)}')
    return version,prerelease

def expected_names(root=ROOT):
    files=subprocess.check_output(['git','ls-files','--',MOD],cwd=root,text=True).splitlines()
    names={n for n in files if n.endswith(('.xml','.png')) or n==f'{MOD}/README.md'}
    return names|DLLS

def check_zip(archive,manifest,root=ROOT):
    data=Path(archive).read_bytes()
    if hashlib.sha256(data).hexdigest()!=manifest['zip_sha256']: raise ValueError('ZIP does not match build-manifest.json')
    expected=expected_names(root)
    with zipfile.ZipFile(archive) as z:
        names=z.namelist()
        if len(names)!=len(set(names)) or set(names)!=expected:
            raise ValueError(f'Unexpected ZIP contents: {sorted(set(names)^expected)}')
        for name in expected-DLLS:
            if z.read(name)!=(root/name).read_bytes(): raise ValueError(f'ZIP entry differs from source: {name}')
        for build in manifest['builds']:
            path=f"{MOD}/{build['game'][:3]}/Assemblies/VCEFlourAlternatives.dll"
            if hashlib.sha256(z.read(path)).hexdigest()!=build['sha256']: raise ValueError(f'DLL differs from build: {path}')
    return data

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tag',required=True)
    parser.add_argument('--artifacts',type=Path,default=ROOT/'artifacts')
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    version,prerelease=check_versions(args.tag)
    manifest=json.loads((args.artifacts/'build-manifest.json').read_text())
    data=check_zip(args.artifacts/'VCE-Flour-Alternatives-dev.zip',manifest)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_bytes(data)
    print(f'{args.output} {hashlib.sha256(data).hexdigest()} prerelease={str(prerelease).lower()}')
