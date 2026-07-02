# good kid, m.A.A.d city — a walkable visual experience (Unity)

A first-person, tourable interpretation of Kendrick Lamar's *good kid, m.A.A.d city*.
You walk the album from track 1 to 12; each song is its own **world** themed to its
meaning, the **sky, sun and clouds shift** with every track, a **mood-matched score plays**
as you move between worlds, **police/news helicopters** patrol overhead, **lowriders bounce**
in classic hydraulic styles, and **easter eggs from Kendrick's whole career**
(Section.80 → GNX → Super Bowl LIX) are scattered to find.

The entire experience is **generated procedurally from C#** — there are no fragile scene
or prefab files to break. Open the folder in Unity and press **Play**.

The look is carried by a runtime-configured pipeline: **linear color space**, **bloom /
ACES tonemapping / vignette / FXAA** (Post Processing v2), raised pixel-light and shadow
quality, and a **silhouette skyline with lit windows** wrapping the whole tour so the city
extends past the walls. Each world also signs its stretch of the walls with a neon strip
in its accent color.

---

## Quick start

1. Install **Unity 6** with **Build Support** for your platform (via Unity Hub ▸ Installs). The project is pinned to `6000.4.11f1` — if Hub prompts, open it with whatever `6000.x` you have installed.
2. Open this folder as a project (Unity Hub ▸ *Add project from disk* ▸ select this folder).
3. Press **Play**. The tour builds itself (auto-boot works even in an empty scene).
   - Prefer a saved scene? Menu **GKMC ▸ Create Tour Scene**.

### Controls
| Action | Key |
|---|---|
| Move | **W A S D** |
| Look | **Mouse** |
| Sprint | **Shift** |
| Jump | **Space** |
| Read an easter egg | look at it, press **E** |
| Free the cursor | **Esc** (click to re-lock) |

---

## The twelve worlds

As you cross into each world the **procedural sky, drifting clouds, fog, ambient light and sun**
morph to match the track, and the score cross-fades. The walls are kept low so the changing
sky is always overhead.

1. **Sherane…** — dusk Compton street, a parked car, a porch light, hooded figures (the trap)
2. **Bitch, Don't Kill My Vibe** — smoke and drifting candles, the calm before the storm
3. **Backseat Freestyle** — a gold hall of mirrors, ego pillars, a little Eiffel Tower
4. **The Art of Peer Pressure** — silhouettes pulling you toward a house that isn't yours
5. **Money Trees** — sunlit backyard where the trees grow money (+ a cruising lowrider)
6. **Poetic Justice** — rose petals, a slow-dance floor, film reels (Janet's film)
7. **good kid** — chain-link, surveillance cameras, cages, searchlights from above
8. **m.A.A.d city** — fire barrels, graffiti, sirens, broken glass, a three-wheeling lowrider
9. **Swimming Pools (Drank)** — a sunken pool of liquor and floating red cups
10. **Sing About Me, I'm Dying of Thirst** — a candlelit memorial fading into a baptismal pool
11. **Real** — a glowing, beating heart ringed by mirrors of truth
12. **Compton** — bright daylight, palm-lined boulevard, the city sign, **lowriders hopping in every style**

**Helicopters** cross the whole sky (rotor sound is synthesised at runtime) and fly **lower over
worlds 7–8** with sweeping searchlights — the recurring Kendrick motif from the *Alright* video.

### Career easter eggs (look + **E**)
Section.80 *HiiiPoWeR* · *untitled unmastered.* · DAMN. **Pulitzer Prize** · *To Pimp a Butterfly* ·
*Mortal Man* (2Pac) · *Mr. Morale* **crown of thorns** · *Black Panther* · **pgLang** · **GNX** ·
*Not Like Us* / **Super Bowl LIX**.

---

## Audio

**There is always sound.** Per track the manager resolves, in order:
1. a local file `Assets/StreamingAssets/Audio/NN.ogg` — the real song, if you supply it
   (most reliable; see that folder's README),
2. an optional stream-resolver URL you configure,
3. otherwise a **procedural score synthesised at runtime** — a four-chord progression
   (minor keys walk i–VI–III–VII, major keys I–V–vi–IV) with bass and a kick/snare/hat
   backbeat, tuned to each track's key and tempo and looped seamlessly, so the tour is
   never silent and ships with zero audio files. The HUD tells you which one you're hearing.

Have the album on disk already? `python3 tools/import_audio.py "/path/to/your/album folder" --apply`
matches your filenames to track numbers and copies them in (they stay git-ignored).

The album itself is copyrighted, so it isn't bundled. To hear the actual songs, download each
track's audio to `NN.ogg` (e.g. `yt-dlp -x --audio-format vorbis`) or point `gkmc_tracks.json`
at a resolver — full details in `Assets/StreamingAssets/Audio/README.txt`. Copy
`gkmc_tracks.sample.json` → `gkmc_tracks.json` to set links/ids/files without recompiling.

---

## 3D models (textured GLB — on by default)

The bundled `.glb` props in `Assets/StreamingAssets/Models/` **load at runtime** through
[glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.18). The package is wired
into `Packages/manifest.json` (`com.unity.cloud.gltfast` 6.18.0) and switched on by the
`GKMC_GLTFAST` define in `Assets/csc.rsp`, so opening the project and pressing **Play** shows the
real models. Each prop still builds its procedural primitive first and **swaps to the model** once
it finishes loading, so the world is never empty and any prop without a `.glb` stays procedural.

Each GLB is **imported once and instanced everywhere** (the 29 candles share one import's
meshes and textures), and materials are post-processed for the Built-in pipeline: Meshy
authors everything fully metallic, which would mirror the bright sky and read as white, so
metalness is zeroed after load to let the painted base-color textures show.

- **Prefer procedural-only?** Remove `com.unity.cloud.gltfast` from the manifest and delete
  `Assets/csc.rsp` — the code falls back automatically (no other change needed).
- **No internet on first open?** glTFast can't download; either restore it later or run
  procedural-only as above.
- **Natively-imported assets** (FBX/OBJ/prefab) at `Assets/Resources/GKMC_Models/<key>` are used
  ahead of the GLB and need no package at all.

**Generate / fetch more** (Meshy AI)
- In Unity: **GKMC ▸ Open Meshy Generator** (key stored in EditorPrefs, never in the repo).
- CLI: `export MESHY_API_KEY=...` then `python3 tools/meshy_generate.py --essential` (add `--refine` for textures, `--all` for everything).
- **Community models:** grab a model's direct **GLB URL** from [meshy.ai/discover](https://www.meshy.ai/discover) (or your library) and add `"glb_url": "https://..."` (or `"task_id": "..."`) to that key in `tools/meshy_models.json` — the generator downloads instead of generating.
- Drop any new `<key>.glb` into `Assets/StreamingAssets/Models/`.

Model keys + prompts live in `tools/meshy_models.json`; runtime sizes in
`Assets/GKMC/Scripts/Models/MeshyModels.cs`.

### API key handling
Never commit your key. Use one of:
- **EditorPrefs** via the in-Unity generator (per-machine), or
- the **`MESHY_API_KEY`** environment variable for the CLI / a managed secret.

`*.key` and `.env` are git-ignored.

---

## Project layout
```
Assets/GKMC/Scripts/
  Core/      GKMCExperience (boot+atmosphere), AlbumData, GKMCUtil,
             VisualQuality (quality settings + post-processing)
  Player/    PlayerController (FPS walker)
  Audio/     YouTubeAudioManager (resolve + cross-fade)
  World/     WorldBuilder, GKMCAnimators (Bobber/Spinner/LowriderHop/…),
             Helicopter, EasterEgg, WorldTrigger
  Models/    MeshyModels (catalogue), ModelLibrary (load + fit + fallback)
  UI/        TourUI (HUD + easter-egg reader)
  Editor/    GKMCSceneSetup (menus), MeshyGeneratorWindow
Assets/StreamingAssets/  Audio/, Models/, gkmc_tracks.sample.json
tools/       meshy_models.json (catalogue), meshy_generate.py (generator)
```

## Troubleshooting
- **Nothing happens on Play** — confirm one `GKMC_Experience` object exists (auto-boot makes one) and check the Console.
- **Audio** — a procedural score always plays; drop `Audio/NN.ogg` files (or set a resolver) to hear the real album instead.
- **Models don't appear** — GLB loading needs `com.unity.cloud.gltfast` (in `Packages/manifest.json`) plus the `GKMC_GLTFAST` define (in `Assets/csc.rsp`); both ship enabled. If the package failed to resolve (e.g. offline on first open) every prop stays a procedural primitive. Check the Console — `ModelLibrary` logs loaded-vs-procedural counts.
- **Text missing** — uses Unity's built-in `LegacyRuntime.ttf`; present in Unity 6.
- **Looks flat / no glow** — post-processing needs `com.unity.postprocessing` (in
  `Packages/manifest.json`) plus the `GKMC_POSTFX` define (in `Assets/csc.rsp`); both ship
  enabled. Without them the tour still runs, just without bloom/tonemapping. Also confirm
  the Console logged the one-time switch to **Linear** color space.

---

*Fan / educational art project. Respect Kendrick Lamar's and rights-holders' copyrights; supply
only audio and likenesses you're entitled to use.*
