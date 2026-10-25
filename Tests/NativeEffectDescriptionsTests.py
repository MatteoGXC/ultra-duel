"""Check actual registered effect labels and operations through the native Lua API."""
import ctypes as C
import os
from pathlib import Path
import sqlite3
import sys
import zipfile
from NativeCardScriptTests import CardData

ROOT=Path(__file__).resolve().parents[1]
CONTENT=Path(os.environ.get('ULTRA_TEST_CONTENT',ROOT))
db=sqlite3.connect(CONTENT/'Data/locales/en-US/cards.cdb')
cards={r[0]:r for r in db.execute('select id,alias,setcode,type,atk,def,level,race,attribute from datas')}
with zipfile.ZipFile(CONTENT/'Data/script.zip') as z:scripts={Path(n).name:z.read(n) for n in z.namelist() if n.endswith('.lua')}
PROBE=b'''
local descriptions={}
local set_description=Effect.SetDescription
Effect.SetDescription=function(e,desc)
 descriptions[desc]=e
 return set_description(e,desc)
end
Debug.ReloadFieldBegin(DUEL_TEST_MODE,5)
Debug.SetPlayerInfo(0,8000,0,0)
Debug.SetPlayerInfo(1,8000,0,0)
local borger=Debug.AddCard(92895501,0,0,LOCATION_MZONE,0,POS_FACEUP_ATTACK)
Debug.ReloadFieldEnd()
local found={}
for desc,e in pairs(descriptions) do
 local index=desc-92895501*16
 assert(index~=1,"Combined-effect heading must not be an effect choice")
 found[index]=e
end
assert(found[2] and found[3],"Both actual effect descriptions must exist")

local draw,damage=0,0
Duel.GetChainInfo=function() return 0,1 end
Duel.Draw=function(p,n,reason) assert(p==0 and n==1);draw=draw+1 end
found[2]:GetOperation()(found[2],0,nil,0,0,nil,0,0)
assert(draw==1 and damage==0,"DARK choice must draw one card")
Duel.GetChainInfo=function() return 1,1500 end
Duel.Damage=function(p,n,reason) assert(p==1 and n==1500);damage=damage+1 end
found[3]:GetOperation()(found[3],0,nil,0,0,nil,0,0)
assert(draw==1 and damage==1,"EARTH/FIRE choice must deal 1500 damage")
'''

buffers={};errors=[]
directory=Path(os.environ.get('ULTRA_NATIVE_DIR',ROOT/'Assets/Plugins/Windows')).resolve()
dll_path=os.add_dll_directory(str(directory)) if os.name=='nt' else None
core=C.CDLL(str(directory/('ocgcore.dll' if os.name=='nt' else 'libocgcore.dylib')),mode=C.RTLD_GLOBAL)
ScriptReader=C.CFUNCTYPE(C.c_void_p,C.c_char_p,C.POINTER(C.c_int32));CardReader=C.CFUNCTYPE(C.c_uint32,C.c_uint32,C.POINTER(CardData));Logger=C.CFUNCTYPE(C.c_uint32,C.c_void_p,C.c_uint32)
@ScriptReader
def script(name,length):
    name=Path(name.decode().replace('\\','/')).name;body=scripts.get(name)
    if body is None:length[0]=0;return 0
    if name not in buffers:buffers[name]=C.create_string_buffer(body)
    length[0]=len(body);return C.addressof(buffers[name])
@CardReader
def card(code,target):
    if code not in cards:return 0
    r=cards[code];d=target.contents;d.code=code;d.alias=r[1];v=r[2]&((1<<64)-1)
    for i in range(16):d.setcode[i]=(v>>(i*16))&65535 if i<4 else 0
    d.type=r[3];d.attack=r[4];d.defense=r[5];d.level=r[6]&255;d.lscale=(r[6]>>24)&255;d.rscale=(r[6]>>16)&255;d.race=r[7];d.attribute=r[8];d.link_marker=r[5] if r[3]&0x4000000 else 0
    return 0
core.get_log_message.argtypes=[C.c_void_p,C.c_void_p]
@Logger
def log(duel,kind):
    b=C.create_string_buffer(4096);core.get_log_message(duel,b);errors.append(b.value.decode(errors='replace'));return 0
core.set_script_reader.argtypes=[ScriptReader];core.set_card_reader.argtypes=[CardReader];core.set_message_handler.argtypes=[Logger]
core.set_script_reader(script);core.set_card_reader(card);core.set_message_handler(log)
core.create_duel.argtypes=[C.c_uint32];core.create_duel.restype=C.c_void_p
core.preload_script.argtypes=[C.c_void_p,C.c_char_p];core.preload_script.restype=C.c_int32;core.end_duel.argtypes=[C.c_void_p]
scripts['ultra_archetype_probe.lua']=PROBE
duel=core.create_duel(1)
core.preload_script(duel,b'ultra_archetype_probe.lua')
core.end_duel(duel)
assert not errors,errors
labels=db.execute('select str3,str4 from texts where id=92895501').fetchone()
assert labels==('DARK: Draw 1 card','EARTH & FIRE: Inflict 1500 damage to your opponent'),labels
print('PASS Heavy Borger: selectable labels, draw and damage operations match the card database.')

for locale in (CONTENT/'Data/locales').glob('*/cards.cdb'):
    with sqlite3.connect(locale) as translated:
        choices=translated.execute('select str3,str4 from texts where id=92895501').fetchone()
        assert choices and all(choices),('Missing translated effect choice',locale,choices)
print('PASS translated effect choices exist at the registered indexes in every bundled language.')
