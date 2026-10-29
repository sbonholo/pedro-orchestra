using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>
    /// Turns a melody + chord symbols into real parts for each instrument.
    /// SameMelody: every instrument plays the tune in its own octave (octave doubling).
    /// SeparateParts: violin melody, viola harmony (3rds/6ths below), cello moving inner voice,
    /// double bass on chord roots. Each part is a genuinely different line.
    /// Solo: the soloist plays the melody in their own octave; the others play the separate-parts
    /// accompaniment (violin takes the harmony line) at lower volume. Unaccompanied pieces: soloist only.
    /// </summary>
    public static class Arranger
    {
        public const float AccompanimentGain = 0.55f;

        /// <summary>Who plays in Solo mode: the best player of the solo instrument, plus one player per other part.</summary>
        public static List<MusicianData> SoloPlayers(List<MusicianData> roster, InstrumentType soloist, bool unaccompanied)
        {
            MusicianData lead = null;
            foreach (var m in roster) if (m.instrument == soloist && (lead == null || m.Overall > lead.Overall)) lead = m;
            var list = new List<MusicianData>();
            if (lead == null) return list;
            list.Add(lead);
            if (!unaccompanied) foreach (var m in roster) if (m.instrument != soloist) list.Add(m);
            return list;
        }

        public static Arrangement Arrange(PieceDefinition piece, ArrangementMode mode, IEnumerable<InstrumentType> instruments, InstrumentType soloist = InstrumentType.Violin)
        {
            var arr = new Arrangement { Piece = piece, Mode = mode };
            foreach (var t in instruments)
            {
                if (arr.Parts.ContainsKey(t)) continue;
                var def = InstrumentCatalog.Get(t);
                Part part;
                if (mode == ArrangementMode.SameMelody) part = Doubling(piece, def);
                else if (mode == ArrangementMode.Solo && t == soloist) { part = Doubling(piece, def); part.Role = "Solo"; }
                else if (mode == ArrangementMode.Solo && piece.Unaccompanied) continue;
                else if (mode == ArrangementMode.Solo && t == InstrumentType.Violin) { part = Harmony(piece, def); part.Role = "Harmony (accompaniment)"; part.Gain = AccompanimentGain; }
                else
                {
                    switch (t)
                    {
                        case InstrumentType.Violin: part = Doubling(piece, def); part.Role = "Melody"; break;
                        case InstrumentType.Viola: part = Harmony(piece, def); break;
                        case InstrumentType.Cello: part = InnerVoice(piece, def); break;
                        case InstrumentType.DoubleBass: part = BassLine(piece, def); break;
                        default: part = Doubling(piece, def); break;
                    }
                    if (mode == ArrangementMode.Solo) { part.Role += " (accompaniment)"; part.Gain = AccompanimentGain; }
                }
                arr.Parts[t] = part;
            }
            return arr;
        }

        // ---------- Same melody ----------

        static Part Doubling(PieceDefinition piece, InstrumentDefinition def)
        {
            var part = new Part { Instrument = def.Type, Role = "Melody (octave " + (def.SameMelodyOctaveShift >= 0 ? "+" : "") + def.SameMelodyOctaveShift + ")" };
            foreach (var n in piece.Melody)
                part.Notes.Add(n.IsRest ? n : new Note(FitRange(n.midi + 12 * def.SameMelodyOctaveShift, def), n.beats));
            return part;
        }

        static int FitRange(int midi, InstrumentDefinition def)
        {
            while (midi < def.LowestMidi) midi += 12;
            while (midi > def.HighestMidi) midi -= 12;
            return midi;
        }

        // ---------- Separate parts ----------

        /// <summary>Viola: a chord tone a 3rd–6th below the melody, chosen for smooth voice leading.</summary>
        static Part Harmony(PieceDefinition piece, InstrumentDefinition def)
        {
            var part = new Part { Instrument = def.Type, Role = "Harmony" };
            float t = 0f;
            int prev = def.CenterMidi;
            foreach (var n in piece.Melody)
            {
                if (n.IsRest) { part.Notes.Add(n); t += n.beats; continue; }
                var chord = ChordTones(ChordAt(piece, t));
                int best = -1; float bestCost = float.MaxValue;
                for (int m = def.LowestMidi; m <= Math.Min(def.HighestMidi, n.midi - 1); m++)
                {
                    if (!chord.Contains(m % 12)) continue;
                    int below = n.midi - m;
                    float cost = Math.Abs(m - prev) * 1.0f;
                    if (below < 3 || below > 9) cost += 8f + Math.Abs(below - 6);
                    else if (below == 5 || below == 7) cost += 5f;  // prefer sweet 3rds/6ths over bare 4ths/5ths
                    if (m < 55) cost += (55 - m) * 0.5f;  // keep the viola in its singing register
                    if (cost < bestCost) { bestCost = cost; best = m; }
                }
                if (best < 0) best = FitRange(n.midi - 12, def);
                part.Notes.Add(new Note(best, n.beats));
                prev = best;
                t += n.beats;
            }
            return part;
        }

        /// <summary>Cello: one note per half-bar, moving between 3rds and 5ths of the chord in the tenor register.</summary>
        static Part InnerVoice(PieceDefinition piece, InstrumentDefinition def)
        {
            var part = new Part { Instrument = def.Type, Role = "Lower harmony" };
            int prev = 52; // E3
            int index = 0;
            foreach (var seg in SplitChords(piece, 2f))
            {
                var tones = ChordTonesOrdered(seg.Symbol);
                // Alternate between 3rd and 5th (or 7th) for a gently moving line.
                int wantedPc = tones[index % 2 == 0 ? 1 : Math.Min(2, tones.Count - 1)];
                int best = Nearest(wantedPc, prev, 45, 60);
                part.Notes.Add(new Note(best, seg.Beats));
                prev = best;
                index++;
            }
            return part;
        }

        /// <summary>Double bass: chord roots, root–fifth on long chords.</summary>
        static Part BassLine(PieceDefinition piece, InstrumentDefinition def)
        {
            var part = new Part { Instrument = def.Type, Role = "Bass line" };
            int prev = 36;
            foreach (var seg in piece.Chords)
            {
                var tones = ChordTonesOrdered(seg.Symbol);
                int root = Nearest(tones[0], prev, 31, 43);
                if (seg.Beats >= 4f)
                {
                    float half = seg.Beats / 2f;
                    part.Notes.Add(new Note(root, half));
                    int fifth = Nearest(tones[2 % tones.Count], root, 28, 43);
                    part.Notes.Add(new Note(fifth, half));
                    prev = fifth;
                }
                else
                {
                    part.Notes.Add(new Note(root, seg.Beats));
                    prev = root;
                }
            }
            return part;
        }

        static int Nearest(int pitchClass, int near, int lo, int hi)
        {
            int best = -1;
            for (int m = lo; m <= hi; m++)
                if (m % 12 == pitchClass && (best < 0 || Math.Abs(m - near) < Math.Abs(best - near))) best = m;
            return best < 0 ? lo : best;
        }

        static List<ChordSegment> SplitChords(PieceDefinition piece, float maxBeats)
        {
            var list = new List<ChordSegment>();
            foreach (var c in piece.Chords)
            {
                float left = c.Beats;
                while (left > maxBeats + 0.001f) { list.Add(new ChordSegment(c.Symbol, maxBeats)); left -= maxBeats; }
                if (left > 0.001f) list.Add(new ChordSegment(c.Symbol, left));
            }
            return list;
        }

        static string ChordAt(PieceDefinition piece, float beat)
        {
            float t = 0f;
            foreach (var c in piece.Chords)
            {
                if (beat < t + c.Beats - 0.0001f) return c.Symbol;
                t += c.Beats;
            }
            return piece.Chords.Count > 0 ? piece.Chords[piece.Chords.Count - 1].Symbol : "C";
        }

        // ---------- Chord parsing ----------

        static HashSet<int> ChordTones(string symbol) { return new HashSet<int>(ChordTonesOrdered(symbol)); }

        /// <summary>Root, 3rd, 5th (and 7th) as pitch classes. Supports C, Cm, C7, Cmaj7, Cm7, Cdim, F#, Bb...</summary>
        public static List<int> ChordTonesOrdered(string symbol)
        {
            int root = NoteParser.NameToMidi(RootName(symbol) + "4") % 12;
            string quality = symbol.Substring(RootName(symbol).Length);
            int[] intervals;
            switch (quality)
            {
                case "m": intervals = new[] { 0, 3, 7 }; break;
                case "7": intervals = new[] { 0, 4, 7, 10 }; break;
                case "maj7": intervals = new[] { 0, 4, 7, 11 }; break;
                case "m7": intervals = new[] { 0, 3, 7, 10 }; break;
                case "dim": intervals = new[] { 0, 3, 6 }; break;
                case "": intervals = new[] { 0, 4, 7 }; break;
                default: throw new FormatException("Unsupported chord symbol '" + symbol + "'");
            }
            var list = new List<int>();
            foreach (var i in intervals) list.Add((root + i) % 12);
            return list;
        }

        static string RootName(string symbol)
        {
            if (symbol.Length > 1 && (symbol[1] == '#' || symbol[1] == 'b')) return symbol.Substring(0, 2);
            return symbol.Substring(0, 1);
        }
    }
}
