import json, glob, sys
sys.path.insert(0, ".")
from cfb_agent.teams import Registry
r = Registry.load(2026, refresh=True)
print("teams:", len(r.teams), "| lookup keys:", len(r.lookup))
traps = ["Miami","Miami (FL)","Miami Hurricanes","Miami (OH)","Miami RedHawks","Miami OH",
         "UL Monroe","Louisiana-Monroe","La.-Monroe","Louisiana","Louisiana Ragin' Cajuns",
         "App State","Appalachian State","Appalachian State Mountaineers",
         "USC","USC Trojans","Southern California",
         "Ole Miss","Mississippi","Mississippi State","Mississippi State Bulldogs",
         "San Jose State","San Jose State Spartans","UMass Minutemen","Massachusetts",
         "Albany","UAlbany Great Danes","Citadel Bulldogs","Troy","Charlotte",
         "Sam Houston State Bearkats","Southern Mississippi Golden Eagles","Youngstown St Penguins",
         "LIU Sharks","Houston Baptist Huskies","Nicholls State Colonels","Southeastern Louisiana Lions",
         "UT Rio Grande Valley Vaqueros","Texas A&M","Texas A&M Aggies","Hawaii"]
bad = 0
for n in traps:
    tid = r.try_resolve(n)
    if not tid: bad += 1
    tag = "OK  " if tid else "MISS"
    print(f"  {tag} {n!r:38s} -> {tid} {r.name(tid) if tid else ''} [{r.classification(tid) if tid else '-'}]")
print("unmapped traps:", bad)

raw = sorted(glob.glob("data/cache/raw/oddsapi_ncaaf_spreads_*.json"))[-1]
od = json.load(open(raw, encoding="utf-8"))
names = sorted({g["home_team"] for g in od} | {g["away_team"] for g in od})
mapped, missing = r.resolve_all(names)
print(f"\nOdds API names: {len(names)} | mapped: {len(mapped)} | UNMAPPED: {len(missing)}")
for m in missing: print("   MISSING", repr(m))
ids = {}
for n, t in mapped.items(): ids.setdefault(t, []).append(n)
dupes = {r.name(t): v for t, v in ids.items() if len(v) > 1}
print("names collapsing to one id (must be genuine synonyms):", dupes)
