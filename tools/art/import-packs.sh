#!/usr/bin/env bash
# Import Unity asset packages into game/ headlessly.
#
#   tools/art/import-packs.sh <folder-with-unitypackage-files>
#   tools/art/import-packs.sh ~/Downloads/synty
#
# Unity's -importPackage runs the same importer the editor uses, with no dialog. Packages are
# imported one at a time and in sorted order so a rerun does the same thing twice, which matters
# when two packs ship the same shader or the same demo scene and the later one wins.
#
# Close the Unity editor before running this: Unity refuses a second instance on one project.
set -euo pipefail

UNITY="/c/Program Files/Unity/Hub/Editor/6000.0.83f1/Editor/Unity.exe"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="$REPO/game"
SRC="${1:-}"

if [[ -z "$SRC" || ! -d "$SRC" ]]; then
  echo "usage: $0 <folder containing .unitypackage files>" >&2
  exit 2
fi
if [[ ! -x "$UNITY" ]]; then
  echo "Unity not found at $UNITY" >&2
  exit 2
fi

mapfile -t PACKS < <(find "$SRC" -maxdepth 2 -iname '*.unitypackage' | sort)
if [[ ${#PACKS[@]} -eq 0 ]]; then
  echo "no .unitypackage files under $SRC" >&2
  exit 1
fi

echo "Found ${#PACKS[@]} package(s):"
printf '  %s\n' "${PACKS[@]}"

LOGDIR="$REPO/.artifacts/art-import"
mkdir -p "$LOGDIR"

for pack in "${PACKS[@]}"; do
  name="$(basename "$pack" .unitypackage)"
  echo
  echo "==> importing $name"
  # -importPackage implies a project open; -quit closes when the import finishes.
  "$UNITY" -batchmode -nographics -projectPath "$PROJECT" \
           -importPackage "$(cygpath -w "$pack" 2>/dev/null || echo "$pack")" \
           -quit -logFile "$LOGDIR/$name.log" || {
    echo "!! import failed, see $LOGDIR/$name.log" >&2
    grep -iE "error|exception" "$LOGDIR/$name.log" | head -5 >&2 || true
    exit 1
  }
  echo "    ok"
done

echo
echo "All packages imported. Logs in $LOGDIR"
echo "Next: run the EditMode tests, then a screenshot smoke test, before committing anything."
