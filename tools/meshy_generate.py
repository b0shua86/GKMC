#!/usr/bin/env python3
"""
Generate (or fetch) the 3D models for the good kid, m.A.A.d city Unity tour using the Meshy AI API.

Each model is written to Assets/StreamingAssets/Models/<key>.glb, where the Unity ModelLibrary
loads it at runtime (requires the com.unity.cloud.gltfast package + the GKMC_GLTFAST define).

Auth: set MESHY_API_KEY in the environment (recommended) or pass --key.
  export MESHY_API_KEY="msy_xxx"
  python3 tools/meshy_generate.py --essential            # generate the essential props
  python3 tools/meshy_generate.py --all --refine         # everything, with textured refine pass
  python3 tools/meshy_generate.py --keys car,lowrider    # specific keys

To use an existing/community model instead of generating, add "glb_url" or "task_id" to that
entry in tools/meshy_models.json and it will be downloaded instead of generated.

Docs: https://docs.meshy.ai/en/api/text-to-3d
"""

import argparse
import json
import os
import sys
import time
import urllib.request
import urllib.error

API_BASE = "https://api.meshy.ai/openapi/v2/text-to-3d"
HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_CATALOG = os.path.join(HERE, "meshy_models.json")
DEFAULT_OUT = os.path.normpath(os.path.join(HERE, "..", "Assets", "StreamingAssets", "Models"))
POLL_SECONDS = 6
POLL_TIMEOUT = 25 * 60  # 25 minutes per task


def log(msg):
    print(msg, flush=True)


def api_post(payload, key):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(API_BASE, data=data, method="POST",
                                 headers={"Authorization": "Bearer " + key,
                                          "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode("utf-8"))


def api_get_task(task_id, key):
    req = urllib.request.Request(API_BASE + "/" + task_id,
                                 headers={"Authorization": "Bearer " + key})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode("utf-8"))


def download(url, path):
    req = urllib.request.Request(url, headers={"User-Agent": "gkmc-tour/1.0"})
    with urllib.request.urlopen(req, timeout=300) as r:
        body = r.read()
    with open(path, "wb") as f:
        f.write(body)
    return len(body)


def poll(task_id, key, label):
    start = time.time()
    while True:
        task = api_get_task(task_id, key)
        status = task.get("status")
        progress = task.get("progress", 0)
        if status == "SUCCEEDED":
            return task
        if status in ("FAILED", "CANCELED"):
            raise RuntimeError(f"{label} {status}: {task.get('task_error', task)}")
        if time.time() - start > POLL_TIMEOUT:
            raise TimeoutError(f"{label} timed out")
        log(f"    {label}: {status} {progress}%")
        time.sleep(POLL_SECONDS)


def generate(entry, key, args):
    kkey = entry["key"]
    model_type = "lowpoly" if args.lowpoly else "standard"

    # Preview pass (geometry).
    log(f"  preview: {kkey}")
    res = api_post({
        "mode": "preview",
        "prompt": entry["prompt"],
        "model_type": model_type,
        "ai_model": "latest",
        "target_formats": ["glb"],
        "should_remesh": True,
    }, key)
    preview_id = res["result"]
    task = poll(preview_id, key, f"{kkey} preview")

    # Optional refine pass (textures).
    if args.refine:
        log(f"  refine:  {kkey}")
        res = api_post({
            "mode": "refine",
            "preview_task_id": preview_id,
            "enable_pbr": True,
            "hd_texture": bool(args.hd),
            "ai_model": "latest",
            "target_formats": ["glb"],
        }, key)
        task = poll(res["result"], key, f"{kkey} refine")

    return task["model_urls"]["glb"]


def resolve_glb_url(entry, key, args):
    """Direct URL > existing task id > generate."""
    if entry.get("glb_url"):
        log(f"  using provided glb_url for {entry['key']}")
        return entry["glb_url"]
    if entry.get("task_id"):
        log(f"  fetching existing task {entry['task_id']} for {entry['key']}")
        return api_get_task(entry["task_id"], key)["model_urls"]["glb"]
    return generate(entry, key, args)


def main():
    ap = argparse.ArgumentParser(description="Generate/fetch Meshy models for the GKMC tour.")
    ap.add_argument("--key", default=os.environ.get("MESHY_API_KEY", ""))
    ap.add_argument("--catalog", default=DEFAULT_CATALOG)
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--keys", default="", help="comma-separated subset of keys")
    ap.add_argument("--essential", action="store_true", help="only models flagged essential")
    ap.add_argument("--all", action="store_true", help="every model in the catalogue")
    ap.add_argument("--refine", action="store_true", help="run the textured refine pass (more credits)")
    ap.add_argument("--hd", action="store_true", help="HD textures on refine")
    ap.add_argument("--lowpoly", action="store_true", help="generate low-poly geometry")
    ap.add_argument("--force", action="store_true", help="re-download even if the file exists")
    args = ap.parse_args()

    if not args.key:
        log("ERROR: no API key. Set MESHY_API_KEY or pass --key.")
        return 2

    with open(args.catalog, "r", encoding="utf-8") as f:
        catalog = json.load(f)
    models = catalog["models"]

    if args.keys:
        wanted = {k.strip() for k in args.keys.split(",") if k.strip()}
        models = [m for m in models if m["key"] in wanted]
    elif args.essential:
        models = [m for m in models if m.get("essential")]
    elif not args.all:
        log("Nothing selected. Use --essential, --all, or --keys a,b,c.")
        return 1

    os.makedirs(args.out, exist_ok=True)
    log(f"Output: {args.out}")
    log(f"Models: {', '.join(m['key'] for m in models)}\n")

    ok, fail = [], []
    for m in models:
        kkey = m["key"]
        dest = os.path.join(args.out, kkey + ".glb")
        if os.path.exists(dest) and not args.force:
            log(f"[skip] {kkey} (exists)")
            ok.append(kkey)
            continue
        log(f"[gen]  {kkey}")
        try:
            url = resolve_glb_url(m, args.key, args)
            size = download(url, dest)
            log(f"  saved {dest} ({size//1024} KB)\n")
            ok.append(kkey)
        except (urllib.error.HTTPError, urllib.error.URLError, RuntimeError, TimeoutError, KeyError) as e:
            log(f"  FAILED {kkey}: {e}\n")
            fail.append(kkey)

    log("=" * 48)
    log(f"done. ok={len(ok)} fail={len(fail)}")
    if fail:
        log("failed: " + ", ".join(fail))
    return 0 if not fail else 1


if __name__ == "__main__":
    sys.exit(main())
