"""Exercise the compiled server over TCP, including actual card and points data."""
import ctypes
import concurrent.futures
import json
import os
from pathlib import Path
import socket
import struct
import subprocess
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    'genesys-exact-100': (8, 100, 'Starter - Genesys', None, True),
    'genesys-over-cap': (8, 99, 'Starter - Genesys', None, False),
    'genesys-zero-cap': (8, 0, 'Starter - Genesys', None, False),
    'genesys-zero-point-deck': (8, 0, 'AI_MokeyMokey', None, True),
    'genesys-link-rejected': (8, 10000, 'Starter - Genesys', 'link', False),
    'tcg-starter': (7, 100, 'Starter - TCG', None, True),
    'tcg-forbidden-rejected': (7, 100, 'Starter - TCG', 'forbidden', False),
    'tcg-solo-dragunity': (7, 100, 'TCG/AI_Dragunity', None, True),
    'tcg-solo-maxxc-rejected': (7, 100, 'TCG/AI_Dragunity', 'maxxc', False),
    'genesys-solo-cyber-100': (8, 100, 'Genesys/AI_CyberDragon', None, True),
    'genesys-solo-cyber-zero-rejected': (8, 0, 'Genesys/AI_CyberDragon', None, False),
    'genesys-solo-mokey-zero': (8, 0, 'Genesys/AI_MokeyMokey', None, True),
}
for row in json.loads((ROOT / 'Data/solo-decks-audit.json').read_text(encoding='utf-8'))['decks']:
    deck = row['file'].removeprefix('Windbot/Decks/').removesuffix('.ydk')
    label = 'solo-library-' + deck.replace('/', '-')
    CASES[label] = (8 if row['mode'] == 'Genesys' else 7,
                    row['points'] if row['mode'] == 'Genesys' else 100, deck, None, True)
    if row['mode'] == 'Genesys' and row['points'] > 0:
        CASES[label + '-below-cap'] = (8, row['points'] - 1, deck, None, False)

def child(case):
    os.chdir(ROOT)
    native = ROOT / 'Assets/Plugins/Windows'
    handles = [os.add_dll_directory(str(native))]
    library = ctypes.CDLL(str(native / 'ygoserver.dll'))
    library.start_server.argtypes = [ctypes.c_char_p]
    library.set_local_genesys_point_cap.argtypes = [ctypes.c_uint]
    assert library.get_server_format_capabilities() & 3 == 3
    rule, cap, deck, mutation, expected = CASES[case]
    library.set_local_genesys_point_cap(cap)
    with socket.socket() as probe:
        probe.bind(('127.0.0.1', 0))
        port = probe.getsockname()[1]
    args = f'{port} {"-1" if rule == 8 else "0"} 1 0 {rule} F F 8000 5 1 0 0'.encode()
    thread = threading.Thread(target=lambda: library.start_server(args), daemon=True)
    thread.start()
    client = socket.socket()
    for _ in range(100):
        try:
            client.connect(('127.0.0.1', port))
            break
        except OSError:
            time.sleep(.05)
    else:
        raise AssertionError('Server did not listen')
    client.settimeout(.7)

    def send(opcode, data=b''):
        client.sendall(struct.pack('<HB', len(data) + 1, opcode) + data)

    def receive():
        data = bytearray()
        while True:
            try:
                part = client.recv(65536)
                if not part: break
                data.extend(part)
            except socket.timeout:
                break
        frames = []
        while len(data) >= 2:
            length = struct.unpack_from('<H', data)[0]
            if len(data) < length + 2: raise AssertionError('Truncated protocol frame')
            frames.append(bytes(data[2:2 + length]))
            del data[:length + 2]
        return frames

    send(0x10, 'Rules Test'.encode('utf-16-le').ljust(40, b'\0'))
    send(0x12, struct.pack('<HBBI', 0x1362, 204, 204, 0) + bytes(40))
    joined = receive()
    assert any(f[0] == 0x12 for f in joined), ('Join failed', joined)
    path = ROOT / ('Data/Windbot/Decks' if deck.startswith('AI_') or '/' in deck else 'Deck') / (deck + '.ydk')
    main, side = [], []
    destination = main
    for line in path.read_text(encoding='utf-8-sig').splitlines():
        if line == '!side': destination = side
        elif line.strip().isdigit(): destination.append(int(line))
    if mutation == 'link': main.append(41999284)  # Linkuriboh, extra-deck Link Monster.
    if mutation == 'forbidden': main[0] = 55144522  # Pot of Greed.
    if mutation == 'maxxc': main[0] = 23434538
    send(0x02, struct.pack('<II', len(main), len(side)) + struct.pack('<' + 'I' * (len(main) + len(side)), *(main + side)))
    send(0x22)
    result = receive()
    rejected = any(f[0] == 2 for f in result)
    ready = any(f[0] == 0x21 and (f[1] & 15) == 9 for f in result)
    assert (ready and not rejected) if expected else rejected, (case, result)
    print('PASS', case, flush=True)
    client.close()
    library.stop_server()
    thread.join(5)

if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == '--all-solo':
        names = [name for name in CASES if name.startswith('solo-library-')]
        def run(name):
            result = subprocess.run([sys.executable, __file__, name], capture_output=True,
                                    text=True, timeout=25)
            if result.returncode:
                raise AssertionError(name + '\n' + result.stdout + result.stderr)
            print('PASS', name, flush=True)
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
            list(pool.map(run, names))
        print(f'{len(names)} native Solo library admission/cap checks passed.')
    elif len(sys.argv) > 1:
        child(sys.argv[1])
    else:
        names = [name for name in CASES if not name.startswith('solo-library-')]
        for name in names:
            subprocess.run([sys.executable, __file__, name], check=True, timeout=20)
        print(f'{len(names)} native TCP server checks passed.')
