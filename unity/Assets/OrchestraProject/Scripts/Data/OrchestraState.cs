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
        public const int CurrentVersion = 4;   // 2: energy scale 0–1000, 3: coins, 4: energy 0–2000
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
        public bool heardOrchestra;
        public bool playerConducts;
        public int coins = Economy.StartingCoins;
        public List<string> ownedInstruments = new List<string>();
        // Career statistics for achievements
        public List<string> achievements = new List<string>();
        public List<string> concertPieces = new List<string>();
        public int judgeApprovals, bestStars, musiciansHired;
        public float bestConducting;
        public bool soloApproved;
        public int level;                      // career level (Economy.Levels): promoted when the judges approve a concert            // "You conduct": the player taps the beat during performances            // first Listen done (drives the Next step card)
        public string venue = Tour.Home;                        // hall chosen for the next concert (Tour.Venues)
        public List<string> venuesWon = new List<string>();     // halls where the judges approved a concert
        public bool wonGame;                                    // the Golden Baton (approved concert in Vienna)
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
            if (saveVersion < 2)
            {
                foreach (var m in roster) m.energy = TrainingSystem.MaxEnergy;   // old saves used 0–100
                saveVersion = 2;
            }
            if (saveVersion < 3) { coins = Economy.StartingCoins; saveVersion = 3; }
            if (saveVersion < 4) { foreach (var m in roster) m.energy = TrainingSystem.MaxEnergy; coins += 450; saveVersion = 4; }   // more energy and coins
            if (ownedInstruments == null) ownedInstruments = new List<string>();
            if (achievements == null) achievements = new List<string>();
            if (concertPieces == null) concertPieces = new List<string>();
            if (venuesWon == null) venuesWon = new List<string>();
            if (string.IsNullOrEmpty(venue)) venue = Tour.Home;
            if (coins < 0) coins = 0;
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
