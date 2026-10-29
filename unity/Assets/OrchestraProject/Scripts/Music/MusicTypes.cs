using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    public enum Difficulty { Beginner = 1, Intermediate = 2, Advanced = 3, Virtuoso = 4 }

    public enum ArrangementMode
    {
        SameMelody,     // everyone plays the tune, each in their own octave
        SeparateParts   // melody, harmony, lower voice and bass line
    }

    /// <summary>A written note. midi &lt; 0 is a rest. Duration is in beats.</summary>
    [Serializable]
    public struct Note
    {
        public int midi;
        public float beats;
        public Note(int midi, float beats) { this.midi = midi; this.beats = beats; }
        public bool IsRest { get { return midi < 0; } }
    }

    public struct ChordSegment
    {
        public string Symbol;
        public float Beats;
        public ChordSegment(string symbol, float beats) { Symbol = symbol; Beats = beats; }
    }

    public class PieceDefinition
    {
        public string Id;
        public string Title;
        public string Composer;
        public Difficulty Difficulty;
        public string Character;        // e.g. "Gentle lullaby"
        public int Bpm;
        public int BeatsPerBar;
        public float PickupBeats;       // anacrusis before bar 1
        public int PhraseBars = 2;      // used for musical phrase shaping
        public List<Note> Melody;       // written around C5 for the violin
        public List<ChordSegment> Chords;
        public string Rights;           // copyright / licence note

        public float TotalBeats
        {
            get { float t = 0; foreach (var n in Melody) t += n.beats; return t; }
        }
    }

    public class Part
    {
        public InstrumentType Instrument;
        public string Role;
        public List<Note> Notes = new List<Note>();
    }

    public class Arrangement
    {
        public PieceDefinition Piece;
        public ArrangementMode Mode;
        public Dictionary<InstrumentType, Part> Parts = new Dictionary<InstrumentType, Part>();
    }

    /// <summary>
    /// Compact, readable note entry: "C5:1 D5:0.5 R:1 F#4:2". Pitch name + octave, then ':' + beats.
    /// "R" is a rest. Sharps '#', flats 'b'.
    /// </summary>
    public static class NoteParser
    {
        public static List<Note> Parse(string text)
        {
            var list = new List<Note>();
            foreach (var token in text.Split(new[] { ' ', '\n', '\r', '\t', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = token.Split(':');
                if (parts.Length != 2) throw new FormatException("Bad note token '" + token + "'. Expected e.g. C5:1");
                float beats = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                list.Add(new Note(parts[0] == "R" ? -1 : NameToMidi(parts[0]), beats));
            }
            return list;
        }

        public static int NameToMidi(string name)
        {
            int[] baseOf = { 9, 11, 0, 2, 4, 5, 7 }; // A B C D E F G
            char letter = char.ToUpperInvariant(name[0]);
            if (letter < 'A' || letter > 'G') throw new FormatException("Bad pitch '" + name + "'");
            int pc = baseOf[letter - 'A'];
            int i = 1;
            while (i < name.Length && (name[i] == '#' || name[i] == 'b'))
            {
                pc += name[i] == '#' ? 1 : -1;
                i++;
            }
            int octave = int.Parse(name.Substring(i), System.Globalization.CultureInfo.InvariantCulture);
            return (octave + 1) * 12 + pc;
        }

        public static string MidiToName(int midi)
        {
            if (midi < 0) return "rest";
            string[] names = { "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B" };
            return names[midi % 12] + (midi / 12 - 1);
        }

        public static List<ChordSegment> ParseChords(string text)
        {
            var list = new List<ChordSegment>();
            foreach (var token in text.Split(new[] { ' ', '\n', '\r', '\t', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = token.Split(':');
                if (parts.Length != 2) throw new FormatException("Bad chord token '" + token + "'. Expected e.g. G7:2");
                list.Add(new ChordSegment(parts[0], float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)));
            }
            return list;
        }
    }
}
