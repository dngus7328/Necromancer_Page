"""Create an isolated HUD layout scene without changing gameplay references."""
from pathlib import Path
import re
import uuid

ROOT = Path(__file__).resolve().parents[2]
source = ROOT / 'Assets/Scenes/SampleScene.unity'
target = ROOT / 'Assets/Scenes/CompactHUDPreview.unity'
text = source.read_text(encoding='utf-8-sig')
blocks = re.split(r'(?m)(?=^--- !u!)', text)
by_id = {}
for i, block in enumerate(blocks):
    m = re.match(r'--- !u!(\d+) &(-?\d+)', block)
    if m:
        by_id[m[2]] = i

def field(ident, name, value):
    index = by_id[str(ident)]
    blocks[index], count = re.subn(r'(?m)^  '+re.escape(name)+r': .*$', '  '+name+': '+value, blocks[index])
    assert count == 1, (ident, name, count)

def pair(x, y):
    return '{x: '+str(x)+', y: '+str(y)+'}'

def rect(ident, anchor, pos, size, pivot=(.5,.5), scale=1):
    for name, value in [('m_AnchorMin',anchor),('m_AnchorMax',anchor),('m_AnchoredPosition',pos),('m_SizeDelta',size),('m_Pivot',pivot)]:
        field(ident,name,pair(*value))
    field(ident,'m_LocalScale','{x: '+str(scale)+', y: '+str(scale)+', z: 1}')

def parent(child, new_parent):
    field(child,'m_Father','{fileID: '+str(new_parent)+'}')

# Move the empty layout groups out of the inactive SafeArea placeholder.
for group in (1797845569,54129027,843265271):
    parent(group,1862002209)
field(1754452936,'m_AnchoredPosition',pair(0,0))
rect(1797845569,(0,0),(16,12),(540,208),(0,0))
rect(54129027,(.5,0),(0,8),(760,112),(.5,0))
rect(843265271,(1,0),(-16,12),(360,208),(1,0))

# Keep existing portrait sprites and animated portrait component intact.
for ident in (920222061,410448292,1507980134,2022067328):
    parent(ident,1797845569)
rect(920222061,(0,0),(96,104),(192,192))
rect(410448292,(0,0),(96,104),(160,160))
rect(1507980134,(0,0),(328,108),(900,115))
rect(510256082,(.5,.5),(0,25),(1220,280),scale=.7)
rect(2033795879,(.5,.5),(0,-25),(1220,280),scale=.7)
rect(2022067328,(0,0),(480,66),(88,40))

# Two existing slot layers use identical geometry; keep their scripts and states.
slot_groups = (1156512200,1530421806)
for group in slot_groups:
    parent(group,54129027)
    rect(group,(.5,.5),(0,0),(760,112))
    children=[]
    for ident,index in by_id.items():
        b=blocks[index]
        if b.startswith('--- !u!224 ') and re.search(r'm_Father: \{fileID: '+str(group)+r'\}',b):
            x=float(re.search(r'm_AnchoredPosition: \{x: ([^,]+)',b)[1])
            children.append((x,ident))
    assert len(children)==8, (group,len(children))
    for order,(_,ident) in enumerate(sorted(children)):
        rect(ident,(0,.5),(51+order*94,0),(200,200),scale=.46)

parent(1329396849,843265271)
parent(1050743249,843265271)
rect(1329396849,(1,0),(-116,106),(220,180),scale=1.05)
rect(1050743249,(1,0),(-300,62),(100,100))

# Rebuild child lists in existing order after reparenting.
parents={}
for ident,index in by_id.items():
    b=blocks[index]
    if b.startswith('--- !u!224 ') or b.startswith('--- !u!4 '):
        m=re.search(r'm_Father: \{fileID: (-?\d+)\}',b)
        if m: parents[ident]=m[1]
for ident,index in by_id.items():
    b=blocks[index]
    m=re.search(r'(?m)^  m_Children:(?: \[\])?\n(?:  - \{fileID: -?\d+\}\n)*',b)
    if not m: continue
    old=re.findall(r'fileID: (-?\d+)',m[0])
    children=[c for c in old if parents.get(c)==ident]
    children += [c for c,p in parents.items() if p==ident and c not in children]
    new='  m_Children:\n'+''.join('  - {fileID: '+c+'}\n' for c in children) if children else '  m_Children: []\n'
    blocks[index]=b[:m.start()]+new+b[m.end():]

result=''.join(blocks)
assert re.findall(r'guid: [0-9a-f]+',result)==re.findall(r'guid: [0-9a-f]+',text), 'Asset references changed'
for ident,index in by_id.items():
    if not re.match(r'--- !u!(224|4) ',blocks[index]):
        original=re.search(r'(?ms)^--- !u!\d+ &'+ident+r'\n.*?(?=^--- !u!|\Z)',text)
        assert original and original[0]==blocks[index], 'Non-layout object changed: '+ident
assert len(parents)==len(set(parents))
target.write_text(result,encoding='utf-8',newline='\n')
meta=Path(str(target)+'.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
print('Created '+str(target))
print('Verified: all non-layout objects and asset references unchanged; both 8-slot layers retained.')
