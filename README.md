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
- **Placeholder:** 3D musicians, instruments and hall are built from simple shapes.
- **Blocked:** solo cello (Karoryfer x Bigcat, CC0) and solo viola (University of Iowa MIS) need `karoryfer.com` and `theremin.music.uiowa.edu` allowed in the cloud session's network settings.
