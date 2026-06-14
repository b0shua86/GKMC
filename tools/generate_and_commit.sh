#!/usr/bin/env bash
# Generate every tour model (textured/refine) ONE AT A TIME, committing + pushing
# each .glb as it lands so long runs survive container reclamation and can resume.
# Requires MESHY_API_KEY in the environment.
#
#   MESHY_API_KEY=msy_... tools/generate_and_commit.sh [branch]
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
BRANCH="${1:-claude/eloquent-ritchie-jss2lz}"

if [ -z "${MESHY_API_KEY:-}" ]; then
  echo "MESHY_API_KEY not set." >&2; exit 2
fi

KEYS=$(python3 -c "import json;print(' '.join(m['key'] for m in json.load(open('tools/meshy_models.json'))['models']))")
echo "Models to make: $KEYS"

for k in $KEYS; do
  dest="Assets/StreamingAssets/Models/$k.glb"
  if [ -f "$dest" ]; then echo "[skip] $k (exists)"; continue; fi
  echo "=== [gen] $k ==="
  if ! python3 tools/meshy_generate.py --keys "$k" --refine; then
    echo "[FAIL] $k"; continue
  fi
  if [ -f "$dest" ]; then
    git add "$dest"
    git commit -q -F - <<MSG
Add Meshy model: $k

https://claude.ai/code/session_01G9yEvyRyeBCAbQaZMuaMMd
MSG
    for attempt in 1 2 3 4; do
      if git push origin "$BRANCH"; then break; fi
      echo "push retry $attempt"; sleep $((attempt*2))
    done
    echo "[ok] committed+pushed $k"
  else
    echo "[miss] $k produced no file"
  fi
done
echo "ALL DONE"
