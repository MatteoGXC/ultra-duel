"""Load card initial effects through the actual engine (not a Lua syntax-only check)."""
import argparse
import ctypes as C
import json
import os
import sys
from pathlib import Path
import sqlite3
import zipfile

ROOT = Path(__file__).resolve().parents[1]

class CardData(C.Structure):
    _fields_ = [('code',C.c_uint32),('alias',C.c_uint32),('setcode',C.c_uint16*16)] + [
        (n,C.c_int32 if n in ('attack','defense') else C.c_uint32)
        for n in ('type','level','attribute','race','attack','defense','lscale','rscale','link_marker')]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--database',type=Path,default=ROOT/'Data/locales/en-US/cards.cdb')
    parser.add_argument('--scripts',type=Path,default=ROOT/'Data/script.zip')
    parser.add_argument('--ids',type=Path)
    parser.add_argument('--output',type=Path)
    parser.add_argument('--all',action='store_true',help='Include OCG and legacy non-token entries too.')
    args=parser.parse_args()
    db=sqlite3.connect(args.database)
    cards={r[0]:r for r in db.execute('select d.id,d.ot,d.alias,d.setcode,d.type,d.atk,d.def,d.level,d.race,d.attribute,t.name from datas d join texts t using(id)')}
    archive=zipfile.ZipFile(args.scripts)
    scripts={Path(n).name:archive.read(n) for n in archive.namelist() if n.endswith('.lua')}
    buffers={}
    directory=Path(os.environ.get('ULTRA_NATIVE_DIR',ROOT/'Assets/Plugins/Windows')).resolve()
    handle=os.add_dll_directory(str(directory)) if os.name=='nt' else None
    core=C.CDLL(str(directory/('ocgcore.dll' if os.name=='nt' else 'libocgcore.dylib' if sys.platform=='darwin' else 'libocgcore.so')),mode=C.RTLD_GLOBAL)
    ScriptReader=C.CFUNCTYPE(C.c_void_p,C.c_char_p,C.POINTER(C.c_int32))
    CardReader=C.CFUNCTYPE(C.c_uint32,C.c_uint32,C.POINTER(CardData))
    MessageHandler=C.CFUNCTYPE(C.c_uint32,C.c_void_p,C.c_uint32)
    errors=[]
    current=[0]
    @ScriptReader
    def read_script(name,length):
        name=Path(name.decode().replace('\\','/')).name
        body=scripts.get(name)
        if body is None: length[0]=0; return 0
        if name not in buffers:buffers[name]=C.create_string_buffer(body)
        length[0]=len(body)
        return C.addressof(buffers[name])
    @CardReader
    def read_card(code,target):
        if code not in cards:return 0
        r=cards[code];d=target.contents;d.code=code;d.alias=r[2]
        value=r[3]&((1<<64)-1)
        for n in range(16): d.setcode[n]=(value>>(n*16))&0xffff if n<4 else 0
        d.type=r[4];d.attack=r[5];d.defense=r[6];d.level=r[7]&0xff
        d.lscale=(r[7]>>24)&0xff;d.rscale=(r[7]>>16)&0xff;d.race=r[8];d.attribute=r[9]
        d.link_marker=(r[6] if r[4]&0x4000000 else 0)
        return 0
    core.get_log_message.argtypes=[C.c_void_p,C.c_void_p]
    @MessageHandler
    def message(duel,kind):
        buffer=C.create_string_buffer(4096);core.get_log_message(duel,buffer)
        errors.append(dict(id=current[0],name=cards.get(current[0],[None]*11)[10],message=buffer.value.decode(errors='replace'),kind=kind))
        return 0
    core.set_script_reader.argtypes=[ScriptReader];core.set_card_reader.argtypes=[CardReader];core.set_message_handler.argtypes=[MessageHandler]
    core.set_script_reader(read_script);core.set_card_reader(read_card);core.set_message_handler(message)
    core.create_duel.argtypes=[C.c_uint32];core.create_duel.restype=C.c_void_p
    core.end_duel.argtypes=[C.c_void_p]
    core.new_card.argtypes=[C.c_void_p,C.c_uint32,C.c_uint8,C.c_uint8,C.c_uint8,C.c_uint8,C.c_uint8]
    ids=json.loads(args.ids.read_text()) if args.ids else [i for i,r in cards.items() if (args.all or r[1]&2) and not r[4]&0x4000]
    for index,code in enumerate(ids):
        current[0]=code
        duel=core.create_duel(1)
        core.new_card(duel,code,0,0,1,0,8)
        core.end_duel(duel)
        if (index+1)%500==0:print('INITIALIZED',index+1,'errors',len(errors),flush=True)
    result=dict(checked=len(ids),errors=errors)
    if args.output:args.output.write_text(json.dumps(result,indent=2),encoding='utf-8')
    print('INITIAL_EFFECT_AUDIT',len(ids),'cards',len(errors),'messages',flush=True)
    if errors:print(json.dumps(errors[:8],ensure_ascii=True))
    if errors:raise SystemExit(1)

if __name__=='__main__':main()
