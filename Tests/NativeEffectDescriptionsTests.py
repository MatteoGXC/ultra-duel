"""Check actual registered effect labels and operations through the native Lua API."""
import ctypes as C
import json
import hashlib
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

-- Reproduce both reported choice menus through their real registered effects.
Debug.ReloadFieldBegin(DUEL_TEST_MODE,5)
Debug.SetPlayerInfo(0,8000,0,0)
Debug.SetPlayerInfo(1,8000,0,0)
local zalen=Debug.AddCard(4891376,0,0,LOCATION_MZONE,0,POS_FACEUP_ATTACK)
local cue=Debug.AddCard(16387555,0,0,LOCATION_MZONE,1,POS_FACEUP_ATTACK)
local first=Debug.AddCard(89631139,1,1,LOCATION_DECK,0,POS_FACEDOWN_DEFENSE)
local second=Debug.AddCard(69247929,1,1,LOCATION_DECK,1,POS_FACEDOWN_DEFENSE)
Debug.ReloadFieldEnd()
local ze=descriptions[4891376*16+1]
assert(ze,"Zalen registered effect missing")
local choice,options=0,nil
Duel.SelectOption=function(tp,...) options={...};return choice end
Duel.IsChainDisablable=function() return true end
Duel.GetChainInfo=function() return ze end
Duel.SetOperationInfo=function() end
Card.IsRelateToChain=function() return true end
local negated,destroyed=nil,0
Duel.NegateEffect=function(link) negated=link;return true end
Duel.Destroy=function() destroyed=destroyed+1;return 1 end
local eg=Group.FromCards(zalen)
for selected=0,1 do
 choice=selected
 ze:GetTarget()(ze,0,eg,0,3,ze,0,1,1)
 assert(options[1]==4891376*16+2 and options[2]==4891376*16+3,"Wrong Zalen option IDs")
 ze:GetOperation()(ze,0,eg,0,3,ze,0,1)
 assert(negated==(selected==0 and 3 or 2),"Zalen label selected the wrong Chain Link")
end
assert(destroyed==2,"Zalen's two choices must both destroy the negated card")

local ce=descriptions[16387555*16+1]
assert(ce,"Cue registered banish effect missing")
Duel.IsPlayerCanRemove=function() return true end
Duel.GetFieldGroupCount=function() return 2 end
Duel.ConfirmDecktop=function() end
Duel.GetDecktopGroup=function() return Group.FromCards(first,second) end
Group.FilterSelect=function() return Group.FromCards(first) end
Duel.RevealSelectDeckSequence=function() end
Duel.DisableShuffleCheck=function() end
Duel.Remove=function() return 1 end
local bottom=0
Duel.MoveSequence=function(card,sequence) assert(card==second and sequence==SEQ_DECKBOTTOM);bottom=bottom+1 end
for selected=0,1 do
 choice=selected
 ce:GetOperation()(ce,0,nil,0,0,nil,0,0)
 assert(options[1]==16387555*16+2 and options[2]==16387555*16+3,"Wrong Cue top/bottom option IDs")
 assert(bottom==selected,"Cue top/bottom choice executed the wrong placement")
end
'''

buffers={};errors=[]
directory=Path(os.environ.get('ULTRA_NATIVE_DIR',ROOT/'Assets/Plugins/Windows')).resolve()
dll_path=os.add_dll_directory(str(directory)) if os.name=='nt' else None
core=C.CDLL(str(directory/('ocgcore.dll' if os.name=='nt' else 'libocgcore.dylib' if __import__('sys').platform=='darwin' else 'libocgcore.so')),mode=C.RTLD_GLOBAL)
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

audit=json.loads((CONTENT/'Data/popup-label-audit.json').read_text(encoding='utf-8'))
assert audit['scriptsSha256']==hashlib.sha256((CONTENT/'Data/script.zip').read_bytes()).hexdigest(),'Popup description audit does not match the installed scripts'
assert audit['selectableDescriptions']>=900
for locale in (CONTENT/'Data/locales').glob('*/cards.cdb'):
    with sqlite3.connect(locale) as translated:
        rows={r[0]:r for r in translated.execute('select * from texts')}
        for choice in audit['choices']:
            label=rows[choice['card']][3+choice['index']]
            assert label and label.strip()!='???',(locale,choice)
            if locale.parent.name=='en-US':assert label==choice['english'],(choice,label)
cue=db.execute('select str3,str4 from texts where id=16387555').fetchone()
assert cue==('Place on top of the Deck','Place on the bottom of the Deck'),cue
zalen=db.execute('select str3,str4 from texts where id=4891376').fetchone()
assert zalen==('Negate the chained effect','Negate the effect that was chained to'),zalen
print('PASS Zalen latest/previous Chain Link operations, Cue top/bottom placement, and all '+str(audit['selectableDescriptions'])+' choice descriptions in every bundled language.')
