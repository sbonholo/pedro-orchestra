# ORCHESTRA PROJECT: Stage 1 + Stage 2 build

A 3D orchestra management and simulation game for Unity 6 (URP). Targets: Windows PC now; iPhone and iPad from the same project later.

---

## 1. What is real and what is a placeholder

| Area | Status in this build |
|---|---|
| Game systems: skills, training, progression, recruitment, auditions, invitations, concerts, ratings, saving | **Real and working.** The logic was tested outside Unity (see §8). |
| Music: 3 pieces in two arrangements | **Real.** "Separate Parts" produces 4 different lines (melody, harmony, inner voice, bass). Each musician plays their own part through their own speaker on stage. |
| Skills change the sound | **Real.** Rhythm affects timing, Intonation affects pitch, Accuracy causes wrong notes, Tone sets bow noise and brightness, Expression sets vibrato, Musicality shapes phrases, Ensemble controls drift, Confidence adds nerves in concerts, Speed slows the tempo. |
| Rhythm mini-game | **Real.** Every tap is measured in milliseconds. You score nothing if you don't tap. |
| Instrument sound | **REAL RECORDINGS** from VSCO 2 Community Edition (CC0, free for commercial use; see CREDITS.md): solo violin (15 notes), viola section (13), cello section (13), solo double bass (13), all with vibrato. Skill effects are applied on top. The synthesizer remains only as a fallback. |
| Musicians | **Procedural figures** (built from shapes in code): tailcoats with satin lapels and bow ties or concert dresses, faces (eyes, brows, nose, lips, ears), six hairstyles, hands with fingers, polished shoes. Bowing (down/up per note), left-hand shifts and vibrato follow the real notes. Stage 3 replaces them with rigged human models. |
| Instruments, chairs, stands, hall | **PLACEHOLDER.** Real-world proportions built from primitives, with procedural wood, velvet and sheet-music textures. Lighting, shadows, reverb and post-processing are real. |

---

## 2. Install (one time, about 30 minutes)

1. Install **Unity Hub**: https://unity.com/download
2. In Unity Hub, go to **Installs → Install Editor** and pick the newest **Unity 6 LTS** version (marked "LTS").
   Tick these modules:
   - **Microsoft Visual Studio Community** (the code editor)
   - **Windows Build Support (IL2CPP)**
   - **iOS Build Support** (for later; Apple builds need a Mac, see §6)
3. Sign in with a free Unity account. The **Personal** licence is free while revenue is under Unity's threshold.

## 3. Create the project and run the game

1. Unity Hub → **New project** → template **Universal 3D** (it must be the URP template) → name it `OrchestraProject` → **Create**.
2. Close Unity. Copy the folder `Assets/OrchestraProject` from this package into your new project's `Assets` folder.
   Result: `YourProject/Assets/OrchestraProject/Scripts/...`
3. Reopen the project and wait for the progress bar to finish compiling.
4. A window asks **"Create the game scene now?"**. Click **Create Game Scene**. (If it doesn't appear: top menu **Orchestra Project → Create Game Scene**.) This creates `Assets/OrchestraProject/Scenes/Orchestra.unity` and adds it to the build.
5. Press **▶ Play**.

If anything goes red, open **Window → General → Console**, copy the first red error and send it to me.

### First-run troubleshooting
| You see | Do this |
|---|---|
| No "Orchestra Project" menu at the top | Scripts haven't compiled. Open the Console and send me the first red error. |
| Everything pink/magenta | The project isn't URP. Run **Orchestra Project → Check Setup**. It tells you what to fix. |
| Play shows an empty scene, not the concert hall | The wrong scene is open. Run **Orchestra Project → Check Setup** → **Open**, then Play again. |
| No sound | Click once inside the Game view. Check that the Game view's **Mute Audio** toggle is off. |
| "Input System" warning dialog on first open | Click **Yes** (Unity restarts). The game supports both input systems. |
| Odd game state while testing | **Orchestra Project → Delete Save (start fresh)**. |

## 4. How to play

- **Next step card (bottom left):** always suggests the best move (Listen, Rehearse, Perform Concert, See applicant, Next Day). Tapping its button is enough to play.
- **Top bar:** **Orchestra**, **Concerts**, **Recruit**, then **Next Day**, **+7 Days**, **?** (tutorial) and **Menu**.
- **Orchestra tab:** tap a player chip (or a musician on stage), tap a skill, then **Quick Practice**. The **Rhythm game** (count-in, then tap with the click: Space or the pad) gives up to 2× a quick practice. **Daily plan** sets what that player trains each new day.
- **Concerts tab:** pick a piece and **Same Melody** or **Separate Parts**. **Perform Concert** (1–5 stars, raises reputation); **Listen** (no effect), **Rehearse** (with playback) or **Quick rehearsal** build the piece's preparation.
- **Recruit tab:** hear applicants, compare them with your player, hire. **Invitations** go to stronger players; acceptance depends on reputation.
- **Energy and days:** each musician has up to 1,000 energy; practice (20), rehearsal (15) and concerts (25) use it. **Next Day** restores 600. In **Director** play style (Settings) musicians also train their daily plan whenever a day passes.
- **Camera button:** cycles Orchestra → Stage → Audience → Close-up → Free (keys 1–5 also work). Free camera: drag, WASD / QE, Shift, scroll; on touch drag, pinch, two-finger pan.
- **Hide UI** to watch with only the stage on screen. During a concert the side panel steps aside; tap any tab to bring it back.
- Progress is saved automatically after every action.

## 5. Build for Windows

**File → Build Profiles → Windows → Switch Platform → Build.** Pick an empty folder. Run `OrchestraProject.exe`.

## 6. iPhone and iPad (later stage): what Apple requires

- **A Mac with Xcode.** Unity on Windows cannot finish an iOS build. Options:
  - your own Mac (any Apple-silicon Mac is enough)
  - a rented cloud Mac (e.g. MacinCloud)
  - Unity Build Automation (cloud builds, paid by build minutes)
- **Apple Developer Program**: US$99 per year. Needed for TestFlight and the App Store. A free Apple ID can only side-load to your own device, and that install expires after 7 days.
- Steps:
  1. Switch the platform to iOS.
  2. Set a Bundle Identifier, e.g. `com.yourname.orchestra`.
  3. Build. Unity generates an Xcode project.
  4. In Xcode: sign with your team, run on the device, then Archive → TestFlight → App Store review.
- The game already handles touch, the iPhone notch (safe area), bigger buttons on phones, and lighter graphics on mobile (fewer lights and seats, no spotlight shadows).
- App Store needs: privacy details (this game collects no data), screenshots for iPhone and iPad, an age rating, and rights to all music and recordings used.

## 7. Replacing placeholders (Stage 3 plan)

| Placeholder | Free / low-cost path (check each licence) |
|---|---|
| Instrument sound | **VSCO 2 Community Edition** (CC0, free to use commercially). Multi-sampled strings get mapped per note and per dynamic, and the skill effects (timing, pitch, vibrato) are applied on top. |
| Musicians | Rigged human models (e.g. Reallusion Character Creator, paid; or MakeHuman, whose exported models can be used freely) plus Unity **Animation Rigging** Two-Bone IK, driven by the same note data the mannequins already use. |
| Instruments, hall | Purchased or CC-BY models (Sketchfab / Asset Store) with correct credits, or commissioned models. The scale already matches real instruments. |
| Repertoire | Public-domain scores (Bach, Vivaldi, Saint-Saëns, Danse Macabre) are fine to arrange. **Elgar's Cello Concerto and Shostakovich are still in copyright in many countries**, so they need a licence. Every recording needs its own licence. |

## 7a. Adding or replacing recordings
`python3 Tools~/build_samples.py <folder of single-note WAVs> Assets/OrchestraProject/Resources/OrchestraSamples/<Instrument> <prefix> [--vibrato]` measures each note's real pitch, loops it and writes the files the game loads.

### Recording your own (same method as the cello)
- Record each **open string** on its own: one sustained, even bow for about 3 s, at mezzo-forte, with 2 s of silence between strings, in a quiet room with the phone 1 m away.
- Even better: add a stopped note about every 4–5 semitones, ideally with and without vibrato.
- Send me the file. I extract, tune-check, loop and install the notes. The game then plays them instead of the synthesizer, with no code change.
- Your recording quality: phone AAC at 65 kbps, noise about −35 dB below the notes. That is good enough now. A WAV recording from a better microphone will sound noticeably cleaner.

## 8. Project structure

```
Assets/OrchestraProject/
  Editor/OrchestraSetup.cs        Menu: create scene, open/delete save
  Scripts/Core/                   GameBootstrap (entry), GameManager (game flow), InputHelper, MaterialLibrary
  Scripts/Data/                   SkillType, InstrumentDefinition (+catalog), MusicianData, MusicianFactory, OrchestraState (save data)
  Scripts/Systems/                TrainingSystem, RecruitmentSystem, SaveSystem
  Scripts/Music/                  MusicTypes (+note parser), MusicLibrary (pieces), Arranger (same melody / separate parts)
  Scripts/Performance/            PerformanceRenderer (skills → played notes), PerformanceEvaluator (ratings), PerformanceController (playback)
  Scripts/Audio/                  SampleVoice + SampleLibrary (recorded instruments), StringSynth (fallback synthesizer + metronome)
  Resources/OrchestraSamples/     Recorded samples per instrument (samples.txt + 16-bit PCM .bytes)
  Scripts/Practice/               RhythmExercise (+scorer), RhythmMiniGame
  Scripts/World/                  ConcertHallBuilder, BatchBuilder, PlaceholderFactory, MusicianView (animation/IK), StageManager (+seating)
  Scripts/CameraSystem/           CameraController
  Scripts/UI/                     UIFactory (theme, helpers), UIManager, Screens/* (menu, HUD, tabs, rhythm, results, settings)
```

- **Add a piece:** add one entry in `MusicLibrary.cs`, with the melody as `"C5:1 D5:0.5 ..."` and chords as `"C:4 G7:2 ..."`. Both arrangements are generated automatically.
- **Add an instrument:** add one entry in `InstrumentCatalog`. Winds, brass and timpani are already listed as "planned" and also need a synth voice and a model.
- **Add a rhythm exercise:** add one entry in `RhythmExercise.All`.

### Tests already run (outside Unity, .NET 8)
- All scripts compile against Unity engine reference assemblies, with both the new Input System and the old Input Manager.
- 3 pieces × 2 arrangements × 4 instruments: correct length, every note within the instrument's range.
- Synthesizer output: no invalid values, no clipping.
- Rhythm scoring: perfect taps score 100%, no taps score 0%, and early, late and extra taps are penalised correctly.
- Ratings: the starting quartet gets about 2★ on an unrehearsed concert; world-class players get 5★.
- Pacing simulation with daily practice and rehearsal (energy 1,000 per musician, +600 per day): Advanced around day 35, Professional around day 80, World-Class around day 155.

### Not yet verified (needs your first Play)
- The visual placement of hands, bow and instruments is computed from real measurements but has not been seen on screen yet. Tune the values in `MusicianView.Build`.
- Light intensities may need adjusting on your display (`ConcertHallBuilder.BuildLighting`).
