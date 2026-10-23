"""Run static, policy, Harmony boundary, versioned build and packaging checks."""
from pathlib import Path
import hashlib
import json
import os
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[1]
DOTNET=os.environ.get('DOTNET','dotnet')

def run(*args):
    print('+ '+' '.join(map(str,args)),flush=True)
    subprocess.run(list(map(str,args)),cwd=ROOT,check=True)

def main():
    run(sys.executable,'-m','unittest','discover','-s','tests','-v')
    run(DOTNET,'run','--project','tests/Policy','-p:NuGetAudit=false')
    run(DOTNET,'run','--project','tests/Adapter')
    for scenario in ['--missing-def','--injected-flour','--clone-prefix']:
        run(DOTNET,'run','--project','tests/Adapter','--no-build','--',scenario)
    run(DOTNET,'build','tests/Metadata','-c','Release')
    evidence=[]
    for version,tfm,reference in [('1.6','net472','1.6.4871'),('1.7','net10.0','1.7.4985-unstable')]:
        run(DOTNET,'build','Source','-c','Release',f'-p:RimWorldVersion={version}','-warnaserror')
        assets=json.loads((ROOT/'Source/obj/project.assets.json').read_text())
        folder=next(iter(assets['packageFolders']))
        game=Path(folder)/'krafs.rimworld.ref'/reference/'ref'/tfm/'Assembly-CSharp.dll'
        dll=ROOT/'Source/bin/Release'/tfm/'VCEFlourAlternatives.dll'
        run(DOTNET,'run','--project','tests/Metadata','-c','Release','--no-build','--',game,dll,version)
        evidence.append({'game':reference,'target':tfm,'sha256':hashlib.sha256(dll.read_bytes()).hexdigest()})
    run(sys.executable,'tools/package_mod.py')
    archive=ROOT/'artifacts/VCE-Flour-Alternatives-dev.zip'
    manifest={'builds':evidence,'zip_sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),
              'in_game_verified':False,'status':'development build; not a completed release'}
    (ROOT/'artifacts/build-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('All automated checks passed. In-game verification remains pending.',flush=True)

if __name__=='__main__':main()
