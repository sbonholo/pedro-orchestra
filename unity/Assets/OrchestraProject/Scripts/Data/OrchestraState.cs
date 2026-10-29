using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    public enum PlayStyle
    {
        HandsOn = 0,   // you run every practice yourself
        Director = 1   // musicians also follow their daily training plan when a day passes
    }

    [Serializable]
    public class PieceProgress
    {
        public string pieceId;
        public float preparation;     // 0 – 1, raised by rehearsals
        public int timesRehearsed;
        public float bestConcertRating;
    }

    /// <summary>The whole saved game. Plain data, saved to JSON by SaveSystem.</summary>
    [Serializable]
    public class OrchestraState
    {
        public const int CurrentVersion = 1;
        public const int MaxRosterSize = 40;

        public int saveVersion = CurrentVersion;
        public string orchestraName = "New Chamber Orchestra";
        public int day = 1;
        public float reputation = 5f;          // 0 – 100
        public int playStyle = (int)PlayStyle.Director;
        public int concertsPerformed;
        public float bestConcertRating;
        public int lastPoolRefreshDay = -100;
        public int lastConcertDay = -1;
        public bool heardOrchestra;            // first Listen done (drives the Next step card)
        public int randomSeed;
        public List<MusicianData> roster = new List<MusicianData>();
        public List<MusicianData> auditionPool = new List<MusicianData>();
        public List<MusicianData> invitePool = new List<MusicianData>();
        public List<PieceProgress> pieces = new List<PieceProgress>();
        public List<string> journal = new List<string>();

        public PieceProgress Progress(string pieceId)
        {
            foreach (var p in pieces) if (p.pieceId == pieceId) return p;
            var np = new PieceProgress { pieceId = pieceId };
            pieces.Add(np);
            return np;
        }

        public MusicianData FindMusician(string id)
        {
            foreach (var m in roster) if (m.id == id) return m;
            return null;
        }

        public void Log(string line)
        {
            journal.Add("Day " + day + ": " + line);
            if (journal.Count > 60) journal.RemoveAt(0);
        }

        public void Validate()
        {
            if (roster == null) roster = new List<MusicianData>();
            if (auditionPool == null) auditionPool = new List<MusicianData>();
            if (invitePool == null) invitePool = new List<MusicianData>();
            if (pieces == null) pieces = new List<PieceProgress>();
            if (journal == null) journal = new List<string>();
            foreach (var m in roster) m.Validate();
            foreach (var m in auditionPool) m.Validate();
            foreach (var m in invitePool) m.Validate();
            foreach (var p in pieces) p.preparation = Math.Max(0f, Math.Min(1f, p.preparation));
            if (day < 1) day = 1;
            reputation = Math.Max(0f, Math.Min(100f, reputation));
        }

        public static OrchestraState NewGame(int seed)
        {
            var rng = new Random(seed);
            var s = new OrchestraState { randomSeed = seed };
            s.roster = MusicianFactory.CreateStartingOrchestra(rng);
            s.Log("The orchestra is founded with four string players.");
            return s;
        }
    }
}
