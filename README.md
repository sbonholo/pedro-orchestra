# Orchestra Project

A 3D orchestra management and simulation game: train musicians, rehearse, perform concerts and build a world-class orchestra.

**Play it:** https://claude.ai/artifact/QJJELMkK8ogZVNQgCRbEJQ (private link). Phones and desktop browsers.

| Folder | What it is |
|---|---|
| `web/` | **The main version.** The playable game in the browser (Three.js + Web Audio); `web/index.html` is a standalone page with no CDN dependencies |
| `unity/` | Future/optional native build: a Unity 6 (URP) project with the same rules. Setup steps: `unity/README.md` |
| `tools/` | `build_samples.py` (WAV notes → game sample set), `bake_web_audio.py` (sample set → web MP3s), `web-vendor/` (rebuilds `web/vendor` and `web/fonts`) |
| `tests/` | Browser smoke test for `web/`, .NET tests for game logic and sample pitch, and a Unity compile check without Unity |
| `CREDITS.md` | Audio, library and font sources and licences |

## Run the web game locally
Any static file server on `web/`, e.g. `cd web && python3 -m http.server 8000`, then open http://localhost:8000.
Everything it loads (Three.js, fonts, recordings) is in `web/`. Recordings load lazily when you enter the game, with a progress indicator.

After using a new `THREE.Something` in `web/index.html`, rebuild the trimmed library:
```
cd tools/web-vendor && npm ci && npm run build
```

## Run the tests
```
node tests/web/smoke.mjs                                # web game in headless Chromium, desktop + iPhone 13 (needs Playwright)
cd tests/core && dotnet run -c Release                 # game logic, arrangements, ratings, rhythm scoring
cd tests/pitch && dotnet run -c Release                # every instrument plays in tune
cd tests/unity-compile && ./fetch_refs.sh && dotnet build unity_inputsystem.csproj
```

## Status
- **Done (Stages 1–2):** hall, 4 musicians, 11 skills, training and progression, 3 pieces × 2 arrangements, real recorded strings, rhythm mini-game, auditions and invitations, concerts with ratings, saving, cameras, PC and touch UI.
- **Placeholder:** 3D musicians, instruments and hall are built from simple shapes.
- **Blocked:** solo cello (Karoryfer x Bigcat, CC0) and solo viola (University of Iowa MIS) need `karoryfer.com` and `theremin.music.uiowa.edu` allowed in the cloud session's network settings.
