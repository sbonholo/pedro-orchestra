# Stage 3 plan: realistic performance

Rule: nothing gets bought or added before the owner approves it. Every asset must be CC0 or explicitly allow commercial use, and must be recorded in `CREDITS.md`.

## 1. Asset shortlist (licence-clean first)

| Need | Option | Licence | Cost | Notes |
|---|---|---|---|---|
| Musicians (bodies) | **Mixamo** characters and animations (Adobe) | Royalty-free use in games, including commercial; raw assets may not be redistributed | Free (Adobe ID) | Realistic humans. Already rigged as Humanoid. Check Adobe's current terms before use. |
| Musicians (alternative) | **MakeHuman / MPFB2** (Blender) | Exported models CC0 | Free | Custom faces, ages and body types, so the roster can look varied. Needs a Blender export step that I script. |
| Musicians (stylised fallback) | Quaternius / Kenney humanoids | CC0 | Free | Low-poly. Fast on phones but less realistic. |
| Playing motion | Procedural, built in code (already started in `MusicianView`) + Unity **Animation Rigging** (Two-Bone IK) | Unity package | Free | Bow arm and left hand follow the real notes. Mixamo has no string-playing clips, so this has to be procedural either way. |
| Instruments | Sketchfab models filtered to CC0 / CC-BY; or Unity Asset Store string sets | CC0 / CC-BY (credit) / Asset Store EULA | Free to about US$40 | The game already uses real proportions, so models drop into the same anchor points. |
| Hall | Poly Haven textures and HDRIs (wood, plaster, velvet); ambientCG materials | CC0 | Free | Swap the procedural textures for PBR sets (albedo + normal + roughness). Biggest visual gain for the least work. |
| Seats and props | Poly Haven / Kenney furniture | CC0 | Free | |
| Sound | VSCO 2 CE (current) + Karoryfer x Bigcat solo cello + U. Iowa solo viola (item 1) | CC0 / to verify | Free | Next: two dynamics layers (p/f) and spiccato/pizzicato for faster pieces. |

## 2. Order of work (each step is playable on its own)

1. **PBR hall** (Poly Haven): floor, walls, seats. About 1 session. No gameplay risk.
2. **One realistic musician** (Mixamo or MakeHuman) seated with a cello, driven by the existing bowing/IK code. Owner checks the look in Unity before the rest.
3. All four musicians + instrument models.
4. **Playing animation pass**: bow speed follows note length, string crossing per pitch, shifting, vibrato wobble, breathing and body sway from Musicality/Expression.
5. **Sound pass**: dynamics layers, round-robin samples, proper hall impulse reverb.
6. Phone performance check (iPhone/iPad): LODs and texture size limits.

## 3. Decisions for the owner

- Realistic (Mixamo/MakeHuman) or stylised (Quaternius/Kenney) musicians? Recommendation: **realistic**, since the game sells on the performance.
- Budget for instrument models: free CC-BY only, or up to about US$40 on the Asset Store?
- The cloud session cannot download from Mixamo (it needs a login). You download the FBX files (steps will be given) and add them to the repo, or send them to me.
