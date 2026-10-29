using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OrchestraProject;

static class Program
{
    static int fails = 0;
    static void Check(bool c, string msg) { Console.WriteLine((c ? "PASS " : "FAIL ") + msg); if (!c) fails++; }

    static Dictionary<InstrumentType, SampleSet> sets = new Dictionary<InstrumentType, SampleSet>();
    static float[] Voice(RenderedPart rp) =>
        sets.TryGetValue(rp.Instrument.Type, out var set) ? SampleVoice.RenderPart(rp, set, 48000, rp.LengthSec, 3) : StringSynth.RenderPart(rp, 48000, rp.LengthSec, 3);

    static void Main(string[] args)
    {
        foreach (var t in new[] { InstrumentType.Violin, InstrumentType.Viola, InstrumentType.Cello, InstrumentType.DoubleBass })
        {
            string dir = "../../unity/Assets/OrchestraProject/Resources/OrchestraSamples/" + t + "/";
            sets[t] = SampleSet.Parse(t, File.ReadAllText(dir + "samples.txt"), f => File.ReadAllBytes(dir + f + ".bytes"));
        }
        var s = OrchestraState.NewGame(1234);
        Check(s.roster.Count == 4, "4 starting musicians");
        foreach (var m in s.roster) Console.WriteLine($"  {m.FullName,-22} {m.Instrument.DisplayName,-12} overall {m.Overall:0.0} {m.LevelName} talent {m.talent:0.00}");
        Check(s.roster.All(m => m.Overall > 25 && m.Overall < 45), "starting level = experienced amateur (25-45)");

        // Arrangements
        foreach (var p in MusicLibrary.All)
        {
            Check(Math.Abs(p.TotalBeats - p.Chords.Sum(c => c.Beats)) < 0.001, $"{p.Id}: melody beats {p.TotalBeats} == chord beats");
            foreach (ArrangementMode mode in Enum.GetValues(typeof(ArrangementMode)))
            {
                var arr = Arranger.Arrange(p, mode, s.roster.Select(m => m.instrument));
                foreach (var kv in arr.Parts)
                {
                    var def = InstrumentCatalog.Get(kv.Key);
                    float beats = kv.Value.Notes.Sum(n => n.beats);
                    bool inRange = kv.Value.Notes.All(n => n.IsRest || (n.midi >= def.LowestMidi && n.midi <= def.HighestMidi));
                    Check(Math.Abs(beats - p.TotalBeats) < 0.001 && inRange, $"{p.Id}/{mode}/{kv.Key}: {beats} beats, in range");
                }
                if (mode == ArrangementMode.SeparateParts && p.Id == "twinkle")
                    foreach (var kv in arr.Parts)
                        Console.WriteLine($"  {kv.Key,-11} {kv.Value.Role,-14} " + string.Join(" ", kv.Value.Notes.Take(10).Select(n => NoteParser.MidiToName(n.midi))));
            }
        }

        // Render + evaluate + synth timing
        var piece = MusicLibrary.Get("twinkle");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var parts = new List<RenderedPart>();
        var arr2 = Arranger.Arrange(piece, ArrangementMode.SeparateParts, s.roster.Select(m => m.instrument));
        float tempo = PerformanceRenderer.EnsembleTempoScale(s.roster, arr2);
        var mix = new float[0];
        foreach (var m in s.roster)
        {
            var rp = PerformanceRenderer.Render(m, arr2.Parts[m.instrument], piece, 0f, PerformanceContext.Concert, piece.Bpm * tempo, 7);
            parts.Add(rp);
            var buf = Voice(rp);
            if (mix.Length < buf.Length) Array.Resize(ref mix, buf.Length);
            for (int i = 0; i < buf.Length; i++) mix[i] += buf[i] * 0.6f;
            Check(buf.All(v => !float.IsNaN(v) && Math.Abs(v) <= 1f), $"synth {m.instrument}: no NaN, within ±1, peak {buf.Max(Math.Abs):0.00}");
        }
        Console.WriteLine($"  synth 4 parts: {sw.ElapsedMilliseconds} ms, tempo scale {tempo:0.00}");
        var res = PerformanceEvaluator.Evaluate(parts, piece, ArrangementMode.SeparateParts, PerformanceContext.Concert, tempo);
        Console.WriteLine($"  beginners concert: overall {res.Overall:0} ({res.Stars}*) R{res.Rhythm:0} I{res.Intonation:0} A{res.Accuracy:0} E{res.Ensemble:0} Art{res.Artistry:0}\n  {res.Review}");
        WriteWav("twinkle_beginners_separate_parts.wav", mix, 48000);
        WriteWav("cello_part_beginner.wav", Voice(parts.First(p => p.Instrument.Type == InstrumentType.Cello)), 48000);

        // World-class version for comparison
        var pros = s.roster.Select(m => { var c = MusicianFactory.Create(m.instrument, 92, new Random(5)); return c; }).ToList();
        var parts2 = new List<RenderedPart>(); var mix2 = new float[0];
        foreach (var m in pros)
        {
            var rp = PerformanceRenderer.Render(m, arr2.Parts[m.instrument], piece, 1f, PerformanceContext.Concert, piece.Bpm, 7);
            parts2.Add(rp);
            var buf = Voice(rp);
            if (mix2.Length < buf.Length) Array.Resize(ref mix2, buf.Length);
            for (int i = 0; i < buf.Length; i++) mix2[i] += buf[i] * 0.6f;
        }
        var res2 = PerformanceEvaluator.Evaluate(parts2, piece, ArrangementMode.SeparateParts, PerformanceContext.Concert, 1f);
        Console.WriteLine($"  world-class concert: overall {res2.Overall:0} ({res2.Stars}*) R{res2.Rhythm:0} I{res2.Intonation:0} A{res2.Accuracy:0} E{res2.Ensemble:0}");
        Check(res2.Overall > res.Overall + 25, "world-class scores far above beginners");
        WriteWav("twinkle_worldclass_separate_parts.wav", mix2, 48000);
        WriteWav("cello_part_worldclass.wav", Voice(parts2.First(p => p.Instrument.Type == InstrumentType.Cello)), 48000);

        // Progression pacing (Director mode, rehearse daily, concert every 7 days)
        var rng = new Random(9);
        var st = OrchestraState.NewGame(77);
        var milestones = new Dictionary<string, int>();
        for (int d = 0; d < 400; d++)
        {
            foreach (var m in st.roster) TrainingSystem.Practice(m, TrainingSystem.ChooseFocus(m), 1f, rng);
            TrainingSystem.Rehearse(st.roster, st.Progress("twinkle"), rng);
            TrainingSystem.AdvanceDay(st, rng);
            float avg = st.roster.Average(m => m.Overall);
            string tier = SkillLevels.TierName(avg);
            if (!milestones.ContainsKey(tier)) milestones[tier] = st.day;
        }
        Console.WriteLine("  pacing (avg roster tier first reached on day): " + string.Join(", ", milestones.Select(kv => kv.Key + "=" + kv.Value)));

        // Rhythm scorer
        var targets = new List<double> { 1.0, 1.75, 2.5, 3.25 };
        var sc = new RhythmScorer(targets);
        Check(sc.RegisterTap(1.01).Grade == TapGrade.Perfect, "tap +10ms = perfect");
        Check(sc.RegisterTap(1.70).Grade == TapGrade.Good, "tap -50ms = good");
        Check(sc.RegisterTap(2.62).Grade == TapGrade.Late, "tap +120ms = late");
        Check(sc.RegisterTap(5.0).Grade == TapGrade.Extra, "far tap = extra");
        Console.WriteLine($"  score {sc.Score:0.00} hits {sc.Hits} extra {sc.ExtraTaps} — {sc.Verdict}");
        var perfect = new RhythmScorer(targets); foreach (var t in targets) perfect.RegisterTap(t);
        Check(Math.Abs(perfect.Score - 1f) < 1e-6, "perfect taps = 1.0");
        var none = new RhythmScorer(targets);
        Check(none.Score == 0f, "no taps = 0 (no free points)");

        // Recruitment
        RecruitmentSystem.Refresh(st, rng);
        Check(st.auditionPool.Count == 3 && st.invitePool.Count == 2, "pools filled");

        // Solo mode: soloist plays the melody; others accompany softly; unaccompanied pieces have one player
        {
            var ss = OrchestraState.NewGame(5);
            var bach = MusicLibrary.Get("bachprelude");
            var sp = Arranger.SoloPlayers(ss.roster, InstrumentType.Cello, bach.Unaccompanied);
            Check(sp.Count == 1 && sp[0].instrument == InstrumentType.Cello, "Bach prelude: cello alone");
            var a = Arranger.Arrange(bach, ArrangementMode.Solo, sp.Select(m => m.instrument), InstrumentType.Cello);
            Check(a.Parts.Count == 1 && a.Parts[InstrumentType.Cello].Notes[0].midi == 43, "Bach prelude starts on the cello's low G (G2)");
            var gh = MusicLibrary.Get("goinghome");
            var sp2 = Arranger.SoloPlayers(ss.roster, InstrumentType.DoubleBass, gh.Unaccompanied);
            var a2 = Arranger.Arrange(gh, ArrangementMode.Solo, sp2.Select(m => m.instrument), InstrumentType.DoubleBass);
            Check(sp2.Count == 4 && a2.Parts[InstrumentType.DoubleBass].Gain == 1f && a2.Parts[InstrumentType.Violin].Gain < 1f, "bass solo with soft quartet accompaniment");
        }
        // You conduct: perfect beating +6, no beating -6
        {
            var r1 = new PerformanceResult { Overall = 50f }; PerformanceEvaluator.ApplyConducting(r1, 1f);
            var r0 = new PerformanceResult { Overall = 50f }; PerformanceEvaluator.ApplyConducting(r0, 0f);
            Check(Math.Abs(r1.Overall - 56f) < 0.01f && Math.Abs(r0.Overall - 44f) < 0.01f && r1.MusicianNotes.Count == 1, "conducting bonus: +6 perfect, -6 none");
        }
        // Economy: coins, judges, masterclasses, instruments
        {
            var es = OrchestraState.NewGame(8); var v = es.roster.First(m => m.instrument == InstrumentType.Violin);
            var strads = Economy.Catalog.Where(i => i.Id.StartsWith("strad_") && i.Type == InstrumentType.Violin).ToList();
            Check(strads.Count == 10 && strads.Min(i => i.Price) == 3000 && strads.Max(i => i.Price) == 5000 && strads.OrderBy(i => i.Price).Last().Id == "strad_messiah", "10 Stradivarius violins, 3,000 to 5,000 coins, Messiah the best");
            Check(es.coins == Economy.StartingCoins, "new game starts with " + Economy.StartingCoins + " coins");
            var good = new PerformanceResult { Overall = 80, Rhythm = 80, Intonation = 80, Ensemble = 80, Artistry = 80 };
            var bad = new PerformanceResult { Overall = 35, Rhythm = 35, Intonation = 35, Ensemble = 35, Artistry = 35 };
            Check(Economy.ConcertCoins(Economy.Judge(good, new Random(1))) == 30 && Economy.ConcertCoins(Economy.Judge(bad, new Random(1))) == 5, "judges: good concert 30 coins, bad concert 5");
            float before = v.Get(SkillType.Intonation); int cost = Economy.MasterclassCost(before);
            Economy.Masterclass(es, v, SkillType.Intonation);
            Check(Math.Abs(v.Get(SkillType.Intonation) - before - 3f) < 0.01f && es.coins == Economy.StartingCoins - cost, "masterclass: +3 for " + cost + " coins");
            es.coins = 5000; float tone0 = Economy.Played(v, SkillType.ToneQuality);
            Economy.Buy(es, v, Economy.Get("strad_messiah"));
            Check(es.coins == 0 && v.instrumentModel == "strad_messiah" && Economy.Played(v, SkillType.ToneQuality) > tone0 + 19f, "Messiah Stradivarius bought: +20 tone");
        }
        // Achievements pay once
        {
            var gs = OrchestraState.NewGame(4); int c0 = gs.coins;
            gs.concertsPerformed = 1; var got = Achievements.Check(gs);
            var again = Achievements.Check(gs);
            Check(got.Count == 1 && got[0].Id == "first_concert" && gs.coins == c0 + 20 && again.Count == 0, "achievement Opening Night pays 20 coins once");
            Check(Achievements.All.Count == 13, "13 career goals");
        }
        // Next step card: listen -> rehearse until 60% -> concert -> rest
        {
            var ns = OrchestraState.NewGame(99); var tw = MusicLibrary.Get("twinkle"); var rr = new Random(3);
            Check(NextStepAdvisor.Get(ns, tw).Action == NextStepAction.Listen, "next step starts with Listen");
            ns.heardOrchestra = true;
            Check(NextStepAdvisor.Get(ns, tw).Action == NextStepAction.Rehearse, "then Rehearse");
            int guard = 0;
            while (NextStepAdvisor.Get(ns, tw).Action == NextStepAction.Rehearse && guard++ < 20) TrainingSystem.Rehearse(ns.roster, ns.Progress("twinkle"), rr);
            var st2 = NextStepAdvisor.Get(ns, tw).Action;
            Check(st2 == NextStepAction.Concert || st2 == NextStepAction.NextDay, "after rehearsals: concert or rest (" + st2 + ")");
            ns.lastConcertDay = ns.day;
            var st3 = NextStepAdvisor.Get(ns, tw).Action;
            Check(st3 == NextStepAction.NextDay || st3 == NextStepAction.Recruit || st3 == NextStepAction.Rehearse, "after a concert: no second concert today (" + st3 + ")");
        }
        Console.WriteLine(fails == 0 ? "\nALL PASS" : $"\n{fails} FAILED");
        Environment.Exit(fails == 0 ? 0 : 1);
    }

    static void WriteWav(string path, float[] data, int sr)
    {
        float peak = data.Max(Math.Abs); float g = peak > 0.95f ? 0.95f / peak : 1f;
        using var bw = new BinaryWriter(File.Create(path));
        bw.Write("RIFF"u8.ToArray()); bw.Write(36 + data.Length * 2); bw.Write("WAVEfmt "u8.ToArray());
        bw.Write(16); bw.Write((short)1); bw.Write((short)1); bw.Write(sr); bw.Write(sr * 2); bw.Write((short)2); bw.Write((short)16);
        bw.Write("data"u8.ToArray()); bw.Write(data.Length * 2);
        foreach (var v in data) bw.Write((short)(Math.Clamp(v * g, -1f, 1f) * 32767));
    }
}
