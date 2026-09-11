#!/usr/bin/env bash
# Download every free CC0 pack listed in free-packs.txt into art/free/.
#
#   bash tools/art/fetch-free-packs.sh            # FBX, textures and the licence files
#   bash tools/art/fetch-free-packs.sh --all      # everything, including .blend sources
#
# Resumable: anything already on disk is skipped, so rerunning after a dropped connection is cheap.
# art/free/ is gitignored on purpose. These are a re-downloadable input, not project source, and
# committing a few hundred megabytes of third-party meshes would be wrong even under CC0.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LIST="$REPO/tools/art/free-packs.txt"
DEST="$REPO/art/free"

ONLY="fbx,txt,png,jpg,jpeg,mtl"
if [[ "${1:-}" == "--all" ]]; then ONLY=""; fi

mkdir -p "$DEST"
total=0

while read -r name id _src; do
  [[ -z "${name:-}" || "${name:0:1}" == "#" ]] && continue
  echo
  echo "############ $name"
  if python "$REPO/tools/art/fetch_drive_folder.py" "$id" "$DEST/$name" \
       ${ONLY:+--only "$ONLY"}; then
    total=$((total + 1))
  else
    echo "!! $name produced nothing; check the id in free-packs.txt" >&2
  fi
done < "$LIST"

echo
echo "=========================================="
echo "$total pack(s) fetched into $DEST"
du -sh "$DEST" 2>/dev/null || true
echo
echo "Licences (CC0 expected in every one):"
find "$DEST" -iname 'License*.txt' -exec sh -c 'printf "  %s: " "$1"; grep -m1 -i "CC0\|Creative Commons\|public domain" "$1" || echo "NO CC0 LINE FOUND"' _ {} \;
