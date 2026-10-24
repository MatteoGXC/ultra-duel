"""Exercise the reported activation/search filters through the actual native Lua API."""
import ctypes as C
import os
from pathlib import Path
import sqlite3
import sys
import zipfile
from NativeCardScriptTests import CardData

ROOT=Path(__file__).resolve().parents[1]
db=sqlite3.connect(ROOT/'Data/locales/en-US/cards.cdb')
cards={r[0]:r for r in db.execute('select id,alias,setcode,type,atk,def,level,race,attribute from datas')}
with zipfile.ZipFile(ROOT/'Data/script.zip') as z:scripts={Path(n).name:z.read(n) for n in z.namelist() if n.endswith('.lua')}
PROBE=b'''
Debug.ReloadFieldBegin(DUEL_TEST_MODE,5)
Debug.SetPlayerInfo(0,8000,0,0)
Debug.SetPlayerInfo(1,8000,0,0)
local opening=Debug.AddCard(36637374,0,0,LOCATION_HAND,0,POS_FACEDOWN)
local aluber=Debug.AddCard(62962630,0,0,LOCATION_DECK,0,POS_FACEDOWN)
local ecclesia=Debug.AddCard(60303688,0,0,LOCATION_DECK,1,POS_FACEDOWN)
local albaz=Debug.AddCard(68468459,0,0,LOCATION_DECK,2,POS_FACEDOWN)
local titan=Debug.AddCard(41373230,0,0,LOCATION_GRAVE,0,POS_FACEUP)
local fusion=Debug.AddCard(87746184,0,0,LOCATION_EXTRA,0,POS_FACEDOWN)
DISCARD
Debug.ReloadFieldEnd()
local e=opening:GetActivateEffect()
assert(aluber:IsSetCard(0x164),"Aluber must be recognized as Despia")
assert(ecclesia:IsSetCard(0x145),"Ecclesia must be recognized as Dogmatika")
assert(opening:IsSetCard(0x15d),"Opening must display as Branded")
assert(c41373230.thfilter(ecclesia,e,0),"Titaniklad must offer Ecclesia")
assert(c41373230.thfilter(albaz,e,0),"Titaniklad must offer Fallen of Albaz")
assert(not c41373230.thfilter(fusion,e,0),"Titaniklad cannot offer arbitrary Albaz-related Fusions")
assert(c36637374.target(e,0,nil,0,0,nil,0,0,0)==CAN_ACTIVATE,"Opening activation depends on available discard")
'''
buffers={};errors=[]
directory=ROOT/'Assets/Plugins/Windows';dll_path=os.add_dll_directory(str(directory));core=C.CDLL(str(directory/'ocgcore.dll'))
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
for discard in (True,False):
    scripts['ultra_archetype_probe.lua']=PROBE.replace(b'DISCARD',b'Debug.AddCard(89631139,0,0,LOCATION_HAND,1,POS_FACEDOWN)' if discard else b'').replace(b'CAN_ACTIVATE',b'true' if discard else b'false')
    buffers.pop('ultra_archetype_probe.lua',None);duel=core.create_duel(1)
    core.preload_script(duel,b'ultra_archetype_probe.lua');core.end_duel(duel)
assert not errors,errors
print('PASS native Opening activation with discard, rejection without discard, Titaniklad Ecclesia/Albaz choices and archetype membership.')
