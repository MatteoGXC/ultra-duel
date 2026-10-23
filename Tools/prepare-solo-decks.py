"""Build separate Solo libraries against the game's bundled rule snapshots.

Original Master Duel/OCG lists are retained. TCG builds remove forbidden cards
and excess copies, filling Main Deck vacancies with supported TCG staples.
Genesys builds retain their Main Deck strategy; incompatible Main Decks are
excluded, Link/Pendulum Extra/Side cards are removed, and the UI filters by cap.
"""
import collections
import hashlib
import json
import re
from pathlib import Path
import sqlite3

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "Data"
LIBRARY = DATA / "Windbot/Decks"
db = sqlite3.connect(DATA / "locales/en-US/cards.cdb")
CARDS = {r[0]: r for r in db.execute(
    "select d.id,d.ot,d.alias,d.type,t.name from datas d join texts t on t.id=d.id")}
LIMITS = {}
active = False
for line in (DATA / "lflist.conf").read_text(encoding="utf-8-sig").splitlines():
    if line.startswith("!"):
        if active:
            break
        assert line.startswith("!TCG Advanced"), "Expected current TCG list first"
        active = True
        continue
    parts = line.split()
    if active and parts and parts[0].isdigit():
        LIMITS[int(parts[0])] = int(parts[1])
POINTS = {}
for line in (DATA / "lflist_genesys.conf").read_text(encoding="utf-8-sig").splitlines():
    p = line.split()
    if p and p[0].isdigit() and p[1] == "$genesys":
        POINTS[int(p[0])] = int(p[2])
EXTRA = 0x4802040
EXCLUDED = 0x5000000  # Link and Pendulum


def read(path):
    sections = [[], [], []]
    section = 0
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if line == "#extra":
            section = 1
        elif line == "!side":
            section = 2
        elif line.isdigit():
            sections[section].append(int(line))
    return sections


def key(code):
    return CARDS[code][2] or code


def eligible(code, section, genesys):
    c = CARDS.get(code)
    return (c is not None and c[1] & 2 and not c[3] & 0x4000
            and (section != 0 or not c[3] & EXTRA)
            and (section != 1 or c[3] & EXTRA)
            and (not genesys or not c[3] & EXCLUDED))


def validate(deck, genesys):
    assert 40 <= len(deck[0]) <= 60 and len(deck[1]) <= 15 and len(deck[2]) <= 15
    counts = collections.Counter(key(i) for s in deck for i in s)
    for section, codes in enumerate(deck):
        for code in codes:
            assert eligible(code, section, genesys), code
            assert counts[key(code)] <= (3 if genesys else LIMITS.get(key(code), 3)), code
    return sum(POINTS.get(key(i), 0) for s in deck for i in s)


def build(source, genesys):
    if genesys and any(CARDS.get(i, (0, 0, 0, 0))[3] & EXCLUDED for i in source[0]):
        return None  # Do not replace a Pendulum/Link strategy with an unrelated deck.
    result = [[], [], []]
    counts = collections.Counter()
    for section, codes in enumerate(source):
        for code in codes:
            if not eligible(code, section, genesys):
                continue
            limit = 3 if genesys else LIMITS.get(key(code), 3)
            if counts[key(code)] < limit and len(result[section]) < (60 if section == 0 else 15):
                result[section].append(code)
                counts[key(code)] += 1
    if genesys and len(result[0]) < 40:
        return None
    if not genesys:
        target = max(40, min(60, len(source[0])))
        # Executors already implement these cards in DefaultExecutor.
        staples = [14558127, 97268402, 10045474, 43898403, 53129443, 12580477, 44095762, 83764718]
        for code in staples:
            while (len(result[0]) < target and eligible(code, 0, False)
                   and counts[key(code)] < LIMITS.get(key(code), 3)):
                result[0].append(code)
                counts[key(code)] += 1
        assert len(result[0]) == target, "No legal replacement capacity"
    validate(result, genesys)
    return result


def write(path, deck, mode):
    path.parent.mkdir(parents=True, exist_ok=True)
    lines = ["#created by Matteo Duel", "#Solo " + mode + " build"]
    for header, codes in zip(["#main", "#extra", "!side"], deck):
        lines += [header] + [str(i) for i in codes]
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


report = {"ruleFiles": {name: hashlib.sha256((DATA / name).read_bytes()).hexdigest()
                        for name in ["lflist.conf", "lflist_genesys.conf"]}, "decks": []}
executors = {}
for source in (ROOT / "Assets/Scripts/Windbot/Game/AI/Decks").glob("*.cs"):
    for name, filename in re.findall(r'\[Deck\("([^"]+)",\s*"([^"]+)"', source.read_text(encoding="utf-8-sig")):
        executors[name] = filename
(DATA / "solo-executors.conf").write_text(
    "\n".join(name + "|" + filename for name, filename in sorted(executors.items())) + "\n", encoding="utf-8")
for path in sorted(LIBRARY.glob("AI_*.ydk")):
    source = read(path)
    for mode in ["TCG", "Genesys"]:
        result = build(source, mode == "Genesys")
        if result is None:
            continue
        target = LIBRARY / mode / path.name
        write(target, result, mode)
        points = validate(read(target), mode == "Genesys")
        report["decks"].append({"file": str(target.relative_to(DATA)).replace("\\", "/"),
                                "mode": mode, "points": points,
                                "removed": list((collections.Counter(sum(source, []))
                                                 - collections.Counter(sum(result, []))).elements())})
assert LIMITS[23434538] == 0
assert all(23434538 not in sum(read(DATA / row["file"]), [])
           for row in report["decks"] if row["mode"] == "TCG")
(DATA / "solo-decks-audit.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
for mode in ["TCG", "Genesys"]:
    rows = [r for r in report["decks"] if r["mode"] == mode]
    print(mode, "legal variants:", len(rows), "within 100 points:", sum(r["points"] <= 100 for r in rows))
print("PASS: every generated deck validated; Maxx C absent from all TCG variants")
