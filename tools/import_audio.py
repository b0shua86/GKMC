#!/usr/bin/env python3
"""Import your local copy of *good kid, m.A.A.d city* into the GKMC tour.

It reads the audio files in a folder you point it at, figures out each track's
number from its filename, and writes them into the game's audio folder as the
names the tour reads (01.<ext> ... 12.<ext>). Files already in a Unity-friendly
format (.ogg/.mp3/.wav) are copied as-is; anything else (.m4a/.flac/.aac/...) is
transcoded to .ogg if `ffmpeg` is on your PATH.

This only renames/organises audio you already have on disk. It downloads nothing.

Examples:
    # 1) Dry run — shows the planned mapping, changes nothing:
    python3 tools/import_audio.py "/path/to/miller/gkmc/gkmc audio"

    # 2) Looks right? Do it:
    python3 tools/import_audio.py "/path/to/miller/gkmc/gkmc audio" --apply

    # Windows:
    python tools\\import_audio.py "C:\\Users\\you\\miller\\gkmc\\gkmc audio" --apply
"""

import argparse
import os
import re
import shutil
import subprocess
import sys

# Standard 12-track order, with distinctive keywords for matching by title.
TRACKS = [
    (1,  "Sherane a.k.a Master Splinter's Daughter", ["sherane", "master splinter"]),
    (2,  "Bitch, Don't Kill My Vibe",                ["kill my vibe", "dont kill my vibe"]),
    (3,  "Backseat Freestyle",                       ["backseat"]),
    (4,  "The Art of Peer Pressure",                 ["peer pressure"]),
    (5,  "Money Trees",                              ["money tree"]),
    (6,  "Poetic Justice",                           ["poetic"]),
    (7,  "good kid",                                 ["good kid"]),
    (8,  "m.A.A.d city",                             ["maad city", "maad", "m a a d"]),
    (9,  "Swimming Pools (Drank)",                   ["swimming pool", "swimming", "drank"]),
    (10, "Sing About Me, I'm Dying of Thirst",       ["dying of thirst", "sing about me", "thirst"]),
    (11, "Real",                                     ["real"]),
    (12, "Compton",                                  ["compton"]),
]

AUDIO_EXTS = (".mp3", ".ogg", ".wav", ".m4a", ".flac", ".aac", ".aiff", ".aif", ".opus", ".wma", ".alac")
NATIVE_EXTS = (".ogg", ".mp3", ".wav")  # formats Unity can decode at runtime


def clean(stem):
    """Lowercase, drop punctuation, and remove the artist/album name so the
    leftover is just the track title (keeps 'good kid' vs 'm.A.A.d city' apart)."""
    s = " " + re.sub(r"\s+", " ", re.sub(r"[^a-z0-9]+", " ", stem.lower())).strip() + " "
    for junk in (" good kid m a a d city ", " good kid maad city ", " gkmc ",
                 " kendrick lamar ", " kendrick "):
        s = s.replace(junk, " ")
    return re.sub(r"\s+", " ", s).strip()


def explicit_number(cleaned):
    nums = sorted({int(t) for t in re.findall(r"\b(\d{1,2})\b", cleaned) if 1 <= int(t) <= 12})
    return nums[0] if len(nums) == 1 else None


def keyword_number(cleaned):
    for num, _title, keys in TRACKS:
        for k in keys:
            if k == "real":
                if re.search(r"\breal\b", cleaned):
                    return num
            elif k in cleaned:
                return num
    return None


def match(filename):
    cleaned = clean(os.path.splitext(filename)[0])
    n = explicit_number(cleaned)
    k = keyword_number(cleaned)
    if n and k and n == k:
        return n, "number+title"
    if k:
        return k, "title"
    if n:
        return n, "number"
    return None, "unmatched"


def default_out():
    here = os.path.dirname(os.path.abspath(__file__))
    cand = os.path.normpath(os.path.join(here, "..", "Assets", "StreamingAssets", "Audio"))
    return cand if os.path.isdir(cand) else None


def transcode(src, dst):
    cmd = ["ffmpeg", "-y", "-loglevel", "error", "-i", src, "-vn", dst]
    subprocess.run(cmd, check=True)


def main():
    ap = argparse.ArgumentParser(description="Import local GKMC album audio into the tour.")
    ap.add_argument("source", help="folder holding your album audio files")
    ap.add_argument("--to", default=default_out(),
                    help="output Audio folder (default: repo Assets/StreamingAssets/Audio)")
    ap.add_argument("--apply", action="store_true", help="actually copy/convert (default: dry run)")
    ap.add_argument("--force-ogg", action="store_true",
                    help="transcode every track to .ogg even if it's already mp3/wav")
    args = ap.parse_args()

    if not os.path.isdir(args.source):
        sys.exit(f"Source folder not found: {args.source}")
    if not args.to:
        sys.exit("Could not locate the output Audio folder — pass --to <path>.")

    files = sorted(f for f in os.listdir(args.source)
                   if os.path.isfile(os.path.join(args.source, f))
                   and os.path.splitext(f)[1].lower() in AUDIO_EXTS)
    if not files:
        sys.exit(f"No audio files in {args.source}")

    have_ffmpeg = shutil.which("ffmpeg") is not None
    plan = {}        # track number -> (src filename, target filename, how, needs_ffmpeg)
    unmatched = []
    conflicts = []

    for f in files:
        num, how = match(f)
        if num is None:
            unmatched.append(f)
            continue
        src_ext = os.path.splitext(f)[1].lower()
        if args.force_ogg or src_ext not in NATIVE_EXTS:
            tgt_ext, needs_ffmpeg = ".ogg", True
        else:
            tgt_ext, needs_ffmpeg = src_ext, False
        target = f"{num:02d}{tgt_ext}"
        if num in plan:
            conflicts.append((num, f, plan[num][0]))
            continue
        plan[num] = (f, target, how, needs_ffmpeg)

    print(f"\nSource : {args.source}")
    print(f"Output : {args.to}")
    print(f"ffmpeg : {'found' if have_ffmpeg else 'NOT found (needed only for non-mp3/ogg/wav)'}\n")
    print(f"{'#':>2}  {'title':<38}  {'your file -> target':<48}  match")
    print("-" * 104)
    for num, title, _ in TRACKS:
        if num in plan:
            src, target, how, needs = plan[num]
            arrow = f"{src} -> {target}"
            flag = "  [needs ffmpeg]" if needs and not have_ffmpeg else ""
            print(f"{num:>2}  {title:<38}  {arrow:<48}  {how}{flag}")
        else:
            print(f"{num:>2}  {title:<38}  {'(no file matched)':<48}  MISSING")

    if unmatched:
        print("\nNot matched to any track (ignored):")
        for f in unmatched:
            print(f"  - {f}")
    if conflicts:
        print("\nMultiple files matched the same track (kept the first, skipped these):")
        for num, dup, kept in conflicts:
            print(f"  - track {num:02d}: skipped '{dup}' (kept '{kept}')")

    if not args.apply:
        print("\nDry run — nothing written. Re-run with --apply once the mapping looks right.")
        return

    os.makedirs(args.to, exist_ok=True)
    written = 0
    for num in sorted(plan):
        src, target, _how, needs = plan[num]
        src_path = os.path.join(args.source, src)
        dst_path = os.path.join(args.to, target)
        try:
            if needs:
                if not have_ffmpeg:
                    print(f"  ! track {num:02d}: needs ffmpeg to convert '{src}' — skipped")
                    continue
                transcode(src_path, dst_path)
            else:
                shutil.copy2(src_path, dst_path)
            written += 1
            print(f"  + {target}  <-  {src}")
        except Exception as e:
            print(f"  ! track {num:02d}: failed on '{src}': {e}")

    print(f"\nDone: wrote {written}/12 tracks into {args.to}")
    print("Open the project, press Play — real tracks override the procedural score.")
    print("(These files are git-ignored, so they stay out of the repo.)")


if __name__ == "__main__":
    main()
