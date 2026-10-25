"""Native regressions for Beyond the Brave's new scripts and shared count limits."""
import argparse,ctypes as C,json,os,sqlite3,sys,zipfile
from pathlib import Path
from NativeCardScriptTests import CardData
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--database',type=Path,default=ROOT/'Data/locales/en-US/cards.cdb');p.add_argument('--scripts',type=Path,default=ROOT/'Data/script.zip');args=p.parse_args()
db=sqlite3.connect(args.database);cards={r[0]:r for r in db.execute('select id,alias,setcode,type,atk,def,level,race,attribute from datas')}
with zipfile.ZipFile(args.scripts) as z:scripts={Path(n).name:z.read(n) for n in z.namelist() if n.endswith('.lua')}
buffers={};errors=[]
directory=Path(os.environ.get('ULTRA_NATIVE_DIR',ROOT/'Assets/Plugins/Windows'));dll_path=os.add_dll_directory(str(directory)) if os.name=='nt' else None;core=C.CDLL(str(directory/('ocgcore.dll' if os.name=='nt' else 'libocgcore.dylib' if sys.platform=='darwin' else 'libocgcore.so')))
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

BASE=r"""
Debug.ReloadFieldBegin(DUEL_TEST_MODE,5)
Debug.SetPlayerInfo(0,8000,0,0)
Debug.SetPlayerInfo(1,8000,0,0)
local effects={}
local register=Card.RegisterEffect
Card.RegisterEffect=function(c,e,...) effects[c]=effects[c] or {};table.insert(effects[c],e);return register(c,e,...) end
"""
PROBES={
 'archetype-and-shared-limit':r"""
local a=Debug.AddCard(59062857,0,0,LOCATION_HAND,0,POS_FACEDOWN)
local b=Debug.AddCard(59062857,0,0,LOCATION_HAND,1,POS_FACEDOWN)
local monster=Debug.AddCard(50284408,0,0,LOCATION_EXTRA,0,POS_FACEDOWN)
Debug.ReloadFieldEnd()
assert(a:IsSetCard(0x1e2) and monster:IsSetCard(0x1e2),"Angelechy native archetype mismatch")
assert(not a:IsSetCard(0x1dc),"Angelechy collides with unrelated Clown Crew")
local ae,be=effects[a][1],effects[b][1]
assert(ae:CheckCountLimit(0) and be:CheckCountLimit(0))
ae:UseCountLimit(0)
assert(not be:CheckCountLimit(0),"Second copy bypasses shared once-per-turn")
assert(effects[a][2]:CheckCountLimit(0),"Different effect must have a separate count limit")
assert(c59062857.tdfilter(monster,0)==false,"Extra Deck monster is not a Spell Zone card candidate")
""",
 'castellan-columns':r"""
local c=Debug.AddCard(50284408,0,0,LOCATION_MZONE,2,POS_FACEUP_ATTACK)
local adjacent=Debug.AddCard(89631139,1,1,LOCATION_MZONE,1,POS_FACEUP_ATTACK)
local distant=Debug.AddCard(89631139,1,1,LOCATION_MZONE,4,POS_FACEUP_ATTACK)
local trap=Debug.AddCard(25024929,1,1,LOCATION_SZONE,0,POS_FACEDOWN)
Debug.ReloadFieldEnd()
assert(c50284408.banfilter(adjacent,2),"Adjacent column monster must be legal")
assert(not c50284408.banfilter(distant,2),"Nonadjacent monster must not be legal")
assert(c50284408.banfilter(trap,2),"Spell/Trap two columns away must be legal")
""",
 'skyborne-fusion-materials':r"""
local c=Debug.AddCard(51307986,0,0,LOCATION_EXTRA,0,POS_FACEDOWN)
local dark8=Debug.AddCard(40737112,0,0,LOCATION_HAND,0,POS_FACEDOWN)
local light8=Debug.AddCard(89631139,0,0,LOCATION_HAND,1,POS_FACEDOWN)
Debug.ReloadFieldEnd()
assert(c51307986.lv8matfilter(dark8,c,SUMMON_TYPE_FUSION,0),"Level8 DARK fusion material rejected")
assert(not c51307986.lv8matfilter(light8,c,SUMMON_TYPE_FUSION,0),"LIGHT material incorrectly accepted")
assert(not c51307986.negcostfilter(light8),"Hand monster is not an equipped card")
""",
 'verdict-targets':r"""
local verdict=Debug.AddCard(48340269,0,0,LOCATION_SZONE,0,POS_FACEUP)
local angel=Debug.AddCard(50284408,0,0,LOCATION_MZONE,0,POS_FACEUP_ATTACK)
local target=Debug.AddCard(62962630,1,1,LOCATION_MZONE,0,POS_FACEUP_ATTACK)
Debug.ReloadFieldEnd()
assert(c48340269.countfilter(angel),"Angelechy monster count failed")
local e=verdict:GetActivateEffect()
assert(c48340269.efftg(e,0,nil,0,0,nil,0,0,0),"Verdict should activate against face-up effect monster")
assert(Card.IsNegatable(target))
""",
 'kumenyo-attach-card-types':r"""
local xyz=Debug.AddCard(52123534,0,0,LOCATION_MZONE,0,POS_FACEUP_ATTACK)
local spell=Debug.AddCard(59062857,0,0,LOCATION_HAND,0,POS_FACEDOWN)
local trap=Debug.AddCard(25024929,0,0,LOCATION_GRAVE,0,POS_FACEUP)
local monster=Debug.AddCard(89631139,0,0,LOCATION_REMOVED,0,POS_FACEUP)
Debug.ReloadFieldEnd()
assert(c52123534.attachfilter(spell,xyz,0,REASON_EFFECT),"Kumenyo must accept a Spell Card")
assert(c52123534.attachfilter(trap,xyz,0,REASON_EFFECT),"Kumenyo must accept a Trap Card")
assert(c52123534.attachfilter(monster,xyz,0,REASON_EFFECT),"Kumenyo must accept a monster")
assert(not c52123534.attachfilter(xyz,xyz,0,REASON_EFFECT),"Kumenyo cannot attach itself")
""",
 'opposition-and-score-filters':r"""
local score=Debug.AddCard(13068112,0,0,LOCATION_HAND,0,POS_FACEDOWN)
local opposition=Debug.AddCard(86457955,0,0,LOCATION_DECK,0,POS_FACEDOWN)
local monster=Debug.AddCard(50284408,0,0,LOCATION_EXTRA,0,POS_FACEDOWN)
local ordinary=Debug.AddCard(89631139,0,0,LOCATION_HAND,1,POS_FACEDOWN)
Debug.ReloadFieldEnd()
assert(c13068112.thfilter(opposition),"Angelechy Spell search should include Opposition")
assert(not c13068112.thfilter(score),"Strategy Score cannot search itself")
assert(not c13068112.thfilter(ordinary),"Unrelated monster cannot be searched")
assert(c86457955.spplfilter(monster),"Opposition cannot identify its Extra Deck archetype")
"""
}
for name,body in PROBES.items():
 errors.clear();scripts['betb_probe.lua']=(BASE+body).encode();buffers.pop('betb_probe.lua',None);duel=core.create_duel(1)
 result=core.preload_script(duel,b'betb_probe.lua');core.end_duel(duel)
 assert result and not errors,(name,result,errors)
 print('PASS',name)


# Exercise the actual blocking zone-selection message, with and without Endgame.
core.start_duel.argtypes=[C.c_void_p,C.c_uint32];core.process.argtypes=[C.c_void_p];core.process.restype=C.c_uint32
core.get_message.argtypes=[C.c_void_p,C.c_void_p];core.get_message.restype=C.c_int32
for with_endgame in (False,True):
 body=r"""
 Debug.ReloadFieldBegin(DUEL_TEST_MODE,5)
 Debug.SetPlayerInfo(0,8000,0,0);Debug.SetPlayerInfo(1,8000,0,0)
 local monster=Debug.AddCard(89631139,1,1,LOCATION_HAND,0,POS_FACEDOWN)
 Debug.AddCard(89631139,0,0,LOCATION_DECK,0,POS_FACEDOWN)
 Debug.AddCard(89631139,1,1,LOCATION_DECK,0,POS_FACEDOWN)
 ENDGAME
 Debug.ReloadFieldEnd()
 local e=Effect.GlobalEffect();e:SetType(EFFECT_TYPE_FIELD|EFFECT_TYPE_CONTINUOUS);e:SetCode(EVENT_ADJUST)
 e:SetOperation(function(e) e:Reset();Duel.SpecialSummon(monster,0,1,1,false,false,POS_FACEUP_ATTACK) end)
 Duel.RegisterEffect(e,1)
 """
 endgame=r"""
 Debug.AddCard(12845564,0,0,LOCATION_SZONE,5,POS_FACEUP)
 Debug.AddCard(50284408,0,0,LOCATION_SZONE,0,POS_FACEUP)
 Debug.AddCard(86673403,0,0,LOCATION_SZONE,1,POS_FACEUP)
 """ if with_endgame else ''
 errors.clear();scripts['betb_zone.lua']=body.replace('ENDGAME',endgame).encode();buffers.pop('betb_zone.lua',None)
 duel=core.create_duel(1);assert core.preload_script(duel,b'betb_zone.lua')
 core.start_duel(duel,1);captured=None
 for _ in range(30):
  core.process(duel);data=C.create_string_buffer(65536);size=core.get_message(duel,data);raw=data.raw[:size]
  if len(raw)>=7 and raw[-7]==18:
   captured=raw[-7:];break
  if errors:break
 core.end_duel(duel)
 expected=0 if with_endgame else 1
 assert not errors and captured is not None and captured[1]==expected,(with_endgame,errors,captured.hex() if captured else None)
 print('PASS summon-zone-selection', 'opponent chooses' if with_endgame else 'summoner chooses')
