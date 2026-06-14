# good kid, m.A.A.d city — a walkable visual experience (Unity)

A first-person, tourable interpretation of Kendrick Lamar's *good kid, m.A.A.d city*.
You walk the album from track 1 to 12; each song is its own **world** themed to its
meaning, the album plays as you move between worlds, **police/news helicopters** patrol
overhead, **lowriders bounce** in classic hydraulic styles, and **easter eggs from
Kendrick's whole career** (Section.80 → GNX → Super Bowl LIX) are scattered to find.

The entire experience is **generated procedurally from C#** — there are no fragile scene
or prefab files to break. Open the folder in Unity and press **Play**.

---

## Quick start

1. Install **Unity 6 LTS (6000.0)** with **Build Support** for your platform (via Unity Hub ▸ Installs). The project is pinned to `6000.0.32f1` — if Hub prompts, open it with whatever `6000.0.x` you have installed.
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

As you cross into each world the **fog, ambient light, sky colour and sun** morph to match
the track, and the audio cross-fades.

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

## Audio (YouTube)

The tour runs with or without sound. Per track it tries, in order:
1. a local file `Assets/StreamingAssets/Audio/NN.ogg` (most reliable — see that folder's README),
2. an optional stream-resolver URL you configure,
3. otherwise it shows the track's **YouTube link** on screen and plays on silently.

YouTube has no direct audio URL for a watch page, so either download the audio to `NN.ogg`
(e.g. `yt-dlp -x --audio-format vorbis`) or point `gkmc_tracks.json` at a resolver. Full details
in `Assets/StreamingAssets/Audio/README.txt`. Copy `gkmc_tracks.sample.json` →
`gkmc_tracks.json` to set links/ids/files without recompiling.

---

## 3D models with Meshy AI (optional, recommended)

Every prop renders as a procedural primitive by default and **upgrades to a real model** when one
is present. Generate or fetch them, then drop a `<key>.glb` into `Assets/StreamingAssets/Models/`.

**Generate / fetch**
- In Unity: **GKMC ▸ Open Meshy Generator** (key stored in EditorPrefs, never in the repo).
- CLI: `export MESHY_API_KEY=...` then `python3 tools/meshy_generate.py --essential` (add `--refine` for textures, `--all` for everything).
- **Community models:** grab a model's direct **GLB URL** from [meshy.ai/discover](https://www.meshy.ai/discover) (or your library) and add `"glb_url": "https://..."` (or `"task_id": "..."`) to that key in `tools/meshy_models.json` — the generator downloads instead of generating.

**Load GLB at runtime** (textured): install `com.unity.cloud.gltfast` (Package Manager ▸ *Add by name*),
then **GKMC ▸ Enable glTFast (GKMC_GLTFAST)**. No glTFast? Put natively-imported FBX/OBJ/prefabs at
`Assets/Resources/GKMC_Models/<key>` instead — no package needed.

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
  Core/      GKMCExperience (boot+atmosphere), AlbumData, GKMCUtil
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
- **No audio** — expected until you add `Audio/NN.ogg` files or a resolver (visuals are unaffected).
- **Models don't appear** — primitives are the fallback; for GLB confirm glTFast + the `GKMC_GLTFAST` define, and that files sit in `Assets/StreamingAssets/Models/`.
- **Text missing** — uses Unity's built-in `LegacyRuntime.ttf`; present in Unity 6.

---

*Fan / educational art project. Respect Kendrick Lamar's and rights-holders' copyrights; supply
only audio and likenesses you're entitled to use.*
