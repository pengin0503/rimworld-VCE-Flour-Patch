"""Package only runtime assets and the two explicitly supplied mod DLLs."""
from pathlib import Path
import argparse
import os
import zipfile

PREFIX='VCE-Flour-Alternatives/'

def package(mod, dlls, output):
    mod,output=Path(mod),Path(output)
    if set(dlls)!={'1.6','1.7'}:
        raise ValueError('Both 1.6 and 1.7 assemblies are required')
    payload={}
    for version,path in dlls.items():
        path=Path(path)
        data=path.read_bytes()
        if len(data)<64 or data[:2]!=b'MZ':
            raise ValueError(f'Invalid assembly: {path}')
        payload[f'{version}/Assemblies/VCEFlourAlternatives.dll']=data
    for name in ('loadFolders.xml','README.md'):
        path=mod/name
        if path.is_symlink(): raise ValueError(f'Symlink rejected: {path}')
        payload[name]=path.read_bytes()
    if not (mod/'About/About.xml').is_file(): raise FileNotFoundError('About/About.xml')
    for directory,extensions in {'About':{'.xml','.png'},'Defs':{'.xml'},'Patches':{'.xml'},'Textures':{'.png'},'Languages':{'.xml'}}.items():
        base=mod/directory
        if not base.is_dir(): raise FileNotFoundError(base)
        if base.is_symlink(): raise ValueError(f'Symlink rejected: {base}')
        for path in sorted(base.rglob('*')):
            if path.is_symlink(): raise ValueError(f'Symlink rejected: {path}')
            if path.is_file() and path.suffix.lower() in extensions:
                payload[path.relative_to(mod).as_posix()]=path.read_bytes()
    output.parent.mkdir(parents=True,exist_ok=True)
    temp=output.with_suffix(output.suffix+'.tmp')
    try:
        with zipfile.ZipFile(temp,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
            for name,data in sorted(payload.items()):
                info=zipfile.ZipInfo(PREFIX+name,date_time=(2026,1,1,0,0,0))
                info.compress_type=zipfile.ZIP_DEFLATED
                info.external_attr=0o100644 << 16
                archive.writestr(info,data)
        os.replace(temp,output)
    finally:
        if temp.exists(): temp.unlink()
    return output

if __name__=='__main__':
    root=Path(__file__).resolve().parents[1]
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,default=root/'artifacts/VCE-Flour-Alternatives-dev.zip')
    args=parser.parse_args()
    print(package(root/'VCE-Flour-Alternatives',{
        v:root/f'Source/bin/Release/{tfm}/VCEFlourAlternatives.dll'
        for v,tfm in [('1.6','net472'),('1.7','net10.0')]},args.output))
