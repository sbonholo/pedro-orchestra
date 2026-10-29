using System.Collections.Generic;

namespace OrchestraProject
{
    public enum InstrumentType
    {
        Violin, Viola, Cello, DoubleBass,
        Flute, Oboe, Clarinet, Bassoon,
        Horn, Trumpet, Trombone, Tuba,
        Timpani
    }

    public enum InstrumentFamily { Strings, Woodwinds, Brass, Percussion }

    public enum PlayingPosture
    {
        SeatedShoulder,      // violin, viola
        SeatedBetweenKnees,  // cello
        StandingUpright      // double bass
    }

    /// <summary>
    /// Everything the game needs to know about one instrument: range, sound colour,
    /// placeholder model size and how it is held. Adding a new instrument = adding one
    /// entry to <see cref="InstrumentCatalog"/> (plus a synth voice and a model for non-strings).
    /// </summary>
    public class InstrumentDefinition
    {
        public InstrumentType Type;
        public string DisplayName;
        public string PlayerTitle;
        public InstrumentFamily Family;
        public PlayingPosture Posture;
        public bool Implemented;   // has a sound voice and a 3D placeholder model
        public int SeatingOrder;   // left-to-right order on stage

        // Range (MIDI note numbers, sounding pitch). 60 = middle C.
        public int LowestMidi;
        public int HighestMidi;
        public int CenterMidi;               // comfortable middle of the range
        public int SameMelodyOctaveShift;    // octaves to move the C5-centred melody in "Same Melody" mode

        // Sound colour for the placeholder string synthesizer.
        public float BrightnessLowHz;        // low-pass cutoff for a poor tone
        public float BrightnessHighHz;       // low-pass cutoff for a beautiful tone
        public float[] FormantHz;            // body resonances
        public float[] FormantGainDb;
        public float LowCutHz;
        public float Loudness;

        // Placeholder model dimensions in metres.
        public float BodyLength, BodyWidth, BodyDepth, NeckLength, EndpinLength, BowLength;
        public float BowMin, BowMax;         // usable bow range (distance from frog to string)
    }

    public static class InstrumentCatalog
    {
        static readonly Dictionary<InstrumentType, InstrumentDefinition> defs = new Dictionary<InstrumentType, InstrumentDefinition>();

        static InstrumentCatalog()
        {
            Add(new InstrumentDefinition
            {
                Type = InstrumentType.Violin, DisplayName = "Violin", PlayerTitle = "Violinist",
                Family = InstrumentFamily.Strings, Posture = PlayingPosture.SeatedShoulder, Implemented = true, SeatingOrder = 0,
                LowestMidi = 55, HighestMidi = 100, CenterMidi = 74, SameMelodyOctaveShift = 0,
                BrightnessLowHz = 3200, BrightnessHighHz = 7500, FormantHz = new[] { 290f, 1100f, 3000f }, FormantGainDb = new[] { 6f, 3f, 6f },
                LowCutHz = 180, Loudness = 0.9f,
                BodyLength = 0.356f, BodyWidth = 0.205f, BodyDepth = 0.06f, NeckLength = 0.235f, EndpinLength = 0f, BowLength = 0.75f, BowMin = 0.10f, BowMax = 0.62f
            });
            Add(new InstrumentDefinition
            {
                Type = InstrumentType.Viola, DisplayName = "Viola", PlayerTitle = "Violist",
                Family = InstrumentFamily.Strings, Posture = PlayingPosture.SeatedShoulder, Implemented = true, SeatingOrder = 1,
                LowestMidi = 48, HighestMidi = 88, CenterMidi = 64, SameMelodyOctaveShift = -1,
                BrightnessLowHz = 2600, BrightnessHighHz = 6000, FormantHz = new[] { 230f, 900f, 2400f }, FormantGainDb = new[] { 6f, 3f, 5f },
                LowCutHz = 120, Loudness = 1.0f,
                BodyLength = 0.41f, BodyWidth = 0.24f, BodyDepth = 0.065f, NeckLength = 0.26f, EndpinLength = 0f, BowLength = 0.74f, BowMin = 0.10f, BowMax = 0.60f
            });
            Add(new InstrumentDefinition
            {
                Type = InstrumentType.Cello, DisplayName = "Cello", PlayerTitle = "Cellist",
                Family = InstrumentFamily.Strings, Posture = PlayingPosture.SeatedBetweenKnees, Implemented = true, SeatingOrder = 2,
                LowestMidi = 36, HighestMidi = 81, CenterMidi = 52, SameMelodyOctaveShift = -2,
                BrightnessLowHz = 1800, BrightnessHighHz = 4500, FormantHz = new[] { 110f, 500f, 1500f }, FormantGainDb = new[] { 6f, 4f, 4f },
                LowCutHz = 55, Loudness = 1.0f,
                BodyLength = 0.76f, BodyWidth = 0.44f, BodyDepth = 0.22f, NeckLength = 0.48f, EndpinLength = 0.30f, BowLength = 0.73f, BowMin = 0.10f, BowMax = 0.58f
            });
            Add(new InstrumentDefinition
            {
                Type = InstrumentType.DoubleBass, DisplayName = "Double Bass", PlayerTitle = "Double Bassist",
                Family = InstrumentFamily.Strings, Posture = PlayingPosture.StandingUpright, Implemented = true, SeatingOrder = 3,
                LowestMidi = 28, HighestMidi = 67, CenterMidi = 40, SameMelodyOctaveShift = -3,
                BrightnessLowHz = 1000, BrightnessHighHz = 2800, FormantHz = new[] { 70f, 250f, 900f }, FormantGainDb = new[] { 5f, 4f, 3f },
                LowCutHz = 32, Loudness = 1.15f,
                BodyLength = 1.11f, BodyWidth = 0.66f, BodyDepth = 0.30f, NeckLength = 0.70f, EndpinLength = 0.25f, BowLength = 0.70f, BowMin = 0.08f, BowMax = 0.45f
            });

            // Planned instruments: data exists so the roster/UI can grow, but they have no
            // synth voice or model yet (Implemented = false), so they cannot be recruited.
            AddPlanned(InstrumentType.Flute, "Flute", "Flautist", InstrumentFamily.Woodwinds, 60, 96, 10);
            AddPlanned(InstrumentType.Oboe, "Oboe", "Oboist", InstrumentFamily.Woodwinds, 58, 91, 11);
            AddPlanned(InstrumentType.Clarinet, "Clarinet", "Clarinettist", InstrumentFamily.Woodwinds, 50, 91, 12);
            AddPlanned(InstrumentType.Bassoon, "Bassoon", "Bassoonist", InstrumentFamily.Woodwinds, 34, 75, 13);
            AddPlanned(InstrumentType.Horn, "French Horn", "Horn Player", InstrumentFamily.Brass, 34, 77, 20);
            AddPlanned(InstrumentType.Trumpet, "Trumpet", "Trumpeter", InstrumentFamily.Brass, 54, 84, 21);
            AddPlanned(InstrumentType.Trombone, "Trombone", "Trombonist", InstrumentFamily.Brass, 40, 72, 22);
            AddPlanned(InstrumentType.Tuba, "Tuba", "Tubist", InstrumentFamily.Brass, 26, 65, 23);
            AddPlanned(InstrumentType.Timpani, "Timpani", "Timpanist", InstrumentFamily.Percussion, 38, 57, 30);
        }

        static void Add(InstrumentDefinition d) { defs[d.Type] = d; }

        static void AddPlanned(InstrumentType t, string name, string title, InstrumentFamily fam, int lo, int hi, int order)
        {
            Add(new InstrumentDefinition
            {
                Type = t, DisplayName = name, PlayerTitle = title, Family = fam, Implemented = false, SeatingOrder = order,
                LowestMidi = lo, HighestMidi = hi, CenterMidi = (lo + hi) / 2, Loudness = 1f
            });
        }

        public static InstrumentDefinition Get(InstrumentType t) { return defs[t]; }

        public static IEnumerable<InstrumentDefinition> All { get { return defs.Values; } }

        public static List<InstrumentType> Implemented()
        {
            var list = new List<InstrumentType>();
            foreach (var d in defs.Values) if (d.Implemented) list.Add(d.Type);
            list.Sort((a, b) => Get(a).SeatingOrder.CompareTo(Get(b).SeatingOrder));
            return list;
        }
    }
}
