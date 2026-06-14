# VIBE OS 🪟✨

**The operating system that builds whatever app you search for.** A self‑contained,
Windows XP–flavored desktop that runs entirely in your browser. Type an app name in
the search bar, press **Enter**, and VIBE OS builds it and opens it in a window.

No install, no build step, no server, no API keys — just open `index.html`.

---

## Run it

```bash
# any of these:
open vibe-os/index.html                 # macOS
xdg-open vibe-os/index.html             # Linux
start vibe-os/index.html                # Windows

# or serve it (recommended so the in‑OS Browser's iframes behave):
cd vibe-os && python3 -m http.server 8080
# then visit http://localhost:8080
```

---

## How it works

1. A short **boot screen** (click or press a key to skip) drops you onto a Bliss‑style desktop.
2. Use the big **search bar** on the desktop — or the one inside the **Start menu** — to
   describe an app.
3. VIBE OS **resolves** your query:
   - If it matches a **built‑in app**, that real app opens.
   - If it doesn't, the **generative builder** scaffolds a working app on the spot
     (with a little "building…" animation), categorized from your words.

Everything you create — notes, tasks, drawings, high scores, budgets — is saved to your
browser's `localStorage`. Nothing leaves your machine.

### Built‑in apps (real, fully working)
| Search… | You get |
|---|---|
| `calculator` | A working calculator (keyboard supported) |
| `notepad` | Auto‑saving text editor, download as `.txt` |
| `paint` | Canvas painter — brushes, colors, eraser, save PNG |
| `snake` | Playable Snake with high‑score |
| `minesweeper` | Classic 9×9 Minesweeper |
| `piano` / `synth` | Web‑Audio keyboard (mouse or A‑W‑S‑E‑D… keys) |
| `clock` | Analog + digital clock |
| `calendar` | Month calendar |
| `tasks` / `to‑do` | Checklist that persists |
| `weather` | Simulated 5‑day forecast for any city |
| `terminal` | A little command line (`help`, `build <app>`, …) |
| `browser` | "Vibe Explorer" with a VibeSearch home page |
| `about` | Welcome / help |

### Generated apps (anything else)
Type something that isn't built‑in and VIBE OS builds a functional app from keywords:

- **game / shooter / clicker / arcade** → a reaction click‑game with timer & high score
- **chat / bot / assistant** → a chat interface
- **budget / expense / money / finance** → an income/expense tracker with running balance
- **count / tally / streak / habit / reps** → a big tap‑counter
- **track / log / list / inventory / collection / gym / grocery / recipe …** → a list manager
- **anything else** → a dashboard scaffold (features checklist, scratchpad, item list)

Try: `pizza order tracker`, `gym workout logger`, `space shooter`, `habit streaks`,
`coffee bean inventory`, `dungeon crawler`, `tip calculator`.

---

## Desktop features

- **Draggable / resizable / minimize / maximize / close** windows with XP "Luna" chrome
- **Taskbar** with running‑app buttons, system tray, and a live clock
- **Start menu** (green Start button) with pinned apps, recent builds, and search
- **Desktop icons** (double‑click) + a **right‑click context menu**
- Keyboard: **Ctrl+Space** focuses the search bar, **Esc** closes menus

---

## Files

```
vibe-os/
  index.html   desktop markup, boot screen, taskbar, start menu
  styles.css   Windows XP "Luna" theme
  apps.js      app engine: built‑in apps + generative builder + resolver
  os.js        window manager, taskbar, start menu, boot
```

No dependencies. Pure HTML/CSS/vanilla JS.

---

*A playful project. "Windows XP" is a trademark of Microsoft; this is an original,
XP‑inspired tribute UI and is not affiliated with or endorsed by Microsoft.*
