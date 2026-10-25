"""Map historical archive entries only; every new build uses the canonical layout."""
LEGACY_EXECUTABLES = ('Matteo-Duel.exe', 'MDPro3-TCG-Genesys.exe')
LEGACY_DATA = ('Matteo-Duel_Data', 'MDPro3-TCG-Genesys_Data')
def canonical_path(name):
    if name in LEGACY_EXECUTABLES:return 'Ultra-Duel.exe'
    for old in LEGACY_DATA:
        if name.startswith(old+'/'):return 'Ultra-Duel_Data'+name[len(old):]
    return name
