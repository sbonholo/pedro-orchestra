# Orchestra Project

A 3D orchestra management and simulation game: train musicians, rehearse, perform concerts and build a world-class orchestra.

**Play the browser preview:** https://claude.ai/artifact/QJJELMkK8ogZVNQgCRbEJQ (private link)

| Folder | What it is |
|---|---|
| `unity/` | The real game: a Unity 6 (URP) project. Setup steps: `unity/README.md` |
| `web/` | Browser preview of the same game (Three.js + Web Audio). The same logic, ported |
| `tools/` | `build_samples.py` (WAV notes → game sample set), `bake_web_audio.py` (sample set → web MP3s) |
| `tests/` | .NET tests for game logic, synth/sample pitch, and a Unity compile check without Unity |
| `CREDITS.md` | Audio sources and licences |

## Run the tests
```
cd tests/core && dotnet run -c Release                 # game logic, arrangements, ratings, rhythm scoring
cd tests/pitch && dotnet run -c Release                # every instrument plays in tune
cd tests/unity-compile && ./fetch_refs.sh && dotnet build unity_inputsystem.csproj
```

## Status
- **Done (Stages 1–2):** hall, 4 musicians, 11 skills, training and progression, 3 pieces × 2 arrangements, real recorded strings, rhythm mini-game, auditions and invitations, concerts with ratings, saving, cameras, PC and touch UI.
- **Simplified play:** 3 tabs (Orchestra, Concerts, Recruit), a one-button "Next step" card, one camera button.
- **Placeholder:** musicians are detailed procedural figures (faces, tailcoats/dresses, hands); instruments and hall are still built from simple shapes. Rigged human models come in Stage 3.
- **Blocked:** solo cello (Karoryfer x Bigcat) and solo viola (University of Iowa MIS) need these hosts allowed in the cloud environment's network settings, then a **new** session: `karoryfer.com`, `www.karoryfer.com`, `shop.karoryfer.com` (download page), `theremin.music.uiowa.edu`, `sfzinstruments.github.io` (licence check). The cello's licence also needs checking: one catalogue lists CC0, others say attribution is required.
- **Next:** Stage 3 plan in `STAGE3.md`.
