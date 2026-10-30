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

- **Main menu:** New Game / Continue / How to Play / Orchestra Management / Music Library / Practice / Auditions / Concerts / Settings.
- **Top bar:** Roster, Musician, Music & Concerts, Practice, Auditions, **Next Day**, **+7 Days**, How to Play (?), Menu.
- **Next step card** (bottom left, under the roster): an optional hint that suggests a good move (Listen, Rehearse, Perform Concert, See applicant, Next Day) with one button.
- **Select a musician:** click or tap them on stage, or pick them in the Roster.
- **Musician tab:** all 11 skills. Tap a skill to choose a focus, then **Quick Practice**, or set it as the musician's **daily plan**.
- **Practice tab:** the rhythm mini-game (count-in, then tap with the click: Space or the pad; up to 2× a quick practice) and one quick-practice button per skill.
- **Music & Concerts:** 31 pieces, listed from Beginner to Virtuoso; pieces shorter than a minute repeat (up to 4 times). Thirteen original Virtuoso showpieces (“Storm Caprice”, “The Devil’s Staircase”, “Perpetual Motion Finale”, “Firebird Toccata”, “Lightning Étude”, “Hurricane Variations”, “Volcano Gigue”, “Midnight Tarantella”, “Glacier Perpetuum”, “Phoenix Rising”, “Dragon’s Dance”, “Comet Trail”, “Thunderbolt Finale”); Pachelbel Canon in D, Beethoven Für Elise, Grieg In the Hall of the Mountain King, Jingle Bells, Amazing Grace, Twinkle, Mary, Happy Birthday, Ode to Joy, Minuet in G, Brahms Lullaby, Dvořák “Going Home”, Greensleeves, Vivaldi “Spring”, the original “Midnight Zombie March”, Mozart “Eine kleine Nachtmusik”, Rimsky-Korsakov “Flight of the Bumblebee” (Virtuoso), Bach Cello Suite No. 1 Prelude). Pick **Same Melody**, **Separate Parts** or **Solo** (choose the solo instrument; the best player of it plays the melody alone). **Perform Concert** (1–5 stars, applause, raises reputation); **Listen** (no effect), **Rehearse** or **Quick rehearsal** build preparation.
- **Conductor and audience:** choose **Automatic** (the conductor beats time 2/4, 3/4, 4/4 with the music) or **You conduct** (tap the beat with Space or the pad; the baton follows your taps; steady beating adds up to +6 points to the rating, no beating costs 6). The hall fills as reputation grows (10 % at 0, full at 75) and the audience applauds after each concert.
- **Coins, judges and the Shop:** five judges score every concert (from strict to generous: intonation, note accuracy, rhythm/ensemble, overall impression, artistry). If three of them give 5.5/10 or more you earn **60 coins**, otherwise **10** (you start with 500). Every listener also pays **5 coins** for a ticket: the hall has 1,166 seats and fills with reputation (10 % at 0, full at 75), so tickets earn about 585 coins per concert at the start and 5,830 when the hall is full. The **Shop** tab sells masterclasses (+3 to any skill of the selected musician; cost 10 + half the skill) and instruments: workshop (3,000), master-made (12,000), ten Stradivarius violins from 35,000 to 50,000 (the “Messiah” is the finest), Stradivarius violas and cellos, and the “Dragonetti” bass. Better instruments add up to +20 Tone, +8 Expression and +4 Intonation.
- **Career levels:** Community Orchestra → Youth Symphony → City Chamber Orchestra → Regional Symphony → National Orchestra → Grand Philharmonic → World Symphony Orchestra. Each concert the judges approve is your audition for the next level. Every level raises the judges' pass mark by 0.5 (5.5 → 8.5), the approved-concert reward by 20 coins (60 → 180) and the strength of applicants.
- **Goals tab:** 15 career goals (National Orchestra, World Symphony Orchestra, first concert, judges' approval, 3 and 5 stars, hiring, instruments, a solo, conducting at 90 %, 5 different pieces, Advanced and Professional level, reputation 50, owning a Stradivarius) pay 20–500 coins once each; an “Achievement unlocked” banner appears.
- **Concert atmosphere:** house lights dim and the stage brightens during concerts; afterwards the musicians bow and the conductor turns to the audience.
- **Auditions:** hear applicants play, compare their skills against your section leader, hire them. Each applicant and invitation has a **1 % chance to be a rare, world-class talent** (level 86–94), marked RARE TALENT. **Invitations** go to stronger players; whether they accept depends on your reputation.
- **Energy and days:** each musician has up to 2,000 energy; practice (20), rehearsal (15) and concerts (25) use it. **Next Day** restores 1,200. In **Director** play style (Settings) musicians also train their daily plan whenever a day passes.
- **Cameras:** Stage (over the conductor's shoulder), Audience, Orchestra, Close-up, Free (keys 1–5). Free camera: drag, WASD / QE, Shift, scroll; on touch drag, pinch, two-finger pan.
- **Hide UI** to watch with only the stage on screen. During a concert the side panels step aside; tap any tab to bring them back.
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
| Repertoire | Public-domain works (Bach, Vivaldi, Saint-Saëns incl. Danse Macabre, **Elgar**: died 1934, so his Cello Concerto (1919) is public domain in the US, UK and EU; a few countries with life + 80/100 terms, e.g. Mexico, differ) are fine to arrange, but transcribe from an old public-domain score, not a modern edition. **Shostakovich** is still in copyright and needs a licence. Every recording needs its own licence. |

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

- **Add a piece:** add one entry in `MusicLibrary.cs`, with the melody as `"C5:1 D5:0.5 ..."` and chords as `"C:4 G7:2 ..."`, plus `Featured` (default soloist) and `Unaccompanied` for solo works. Same Melody, Separate Parts and Solo are generated automatically. Add the same entry to `PIECES` in `web/index.html`.
- **Add an instrument:** add one entry in `InstrumentCatalog`. Winds, brass and timpani are already listed as "planned" and also need a synth voice and a model.
- **Add a rhythm exercise:** add one entry in `RhythmExercise.All`.

### Tests already run (outside Unity, .NET 8)
- All scripts compile against Unity engine reference assemblies, with both the new Input System and the old Input Manager.
- 3 pieces × 2 arrangements × 4 instruments: correct length, every note within the instrument's range.
- Synthesizer output: no invalid values, no clipping.
- Rhythm scoring: perfect taps score 100%, no taps score 0%, and early, late and extra taps are penalised correctly.
- Ratings: the starting quartet gets about 2★ on an unrehearsed concert; world-class players get 5★.
- Pacing simulation with daily practice and rehearsal (energy 2,000 per musician, +1,200 per day): Advanced around day 35, Professional around day 80, World-Class around day 155.

### Not yet verified (needs your first Play)
- The visual placement of hands, bow and instruments is computed from real measurements but has not been seen on screen yet. Tune the values in `MusicianView.Build`.
- Light intensities may need adjusting on your display (`ConcertHallBuilder.BuildLighting`).
