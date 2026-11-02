using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>A concert hall on the world tour. Real halls and their real seat counts.</summary>
    public class Venue
    {
        public string Id, Name, City;
        public int MinLevel;     // career level needed to be invited (Economy.Levels)
        public int Seats;        // every listener pays Economy.TicketPrice
        public float Strict;     // added to the judges' pass mark
        public int Prize;        // paid once, the first time the judges approve a concert there
    }

    /// <summary>
    /// The goal of the game: tour the world's great concert halls. Each career level opens a new hall;
    /// the first approved concert in a hall wins its prize and +5 reputation. Winning in the Golden Hall
    /// of the Vienna Musikverein (World Symphony Orchestra level) wins the Golden Baton: the game is won.
    /// Also: one surprise event on about a third of the days. Same rules and numbers as VENUES / dailyEvent() in the web preview.
    /// </summary>
    public static class Tour
    {
        public const string Home = "home", Final = "vienna";
        public const float WinReputation = 5f, EventChance = 0.35f;

        public static readonly Venue[] Venues =
        {
            new Venue { Id = Home, Name = "Riverside Concert Hall", City = "your home town", MinLevel = 0, Seats = Economy.HallSeats, Strict = 0f, Prize = 0 },
            new Venue { Id = "prague", Name = "Rudolfinum, Dvořák Hall", City = "Prague", MinLevel = 1, Seats = 1100, Strict = 0.1f, Prize = 500 },
            new Venue { Id = "amsterdam", Name = "Concertgebouw", City = "Amsterdam", MinLevel = 2, Seats = 1974, Strict = 0.2f, Prize = 1000 },
            new Venue { Id = "tokyo", Name = "Suntory Hall", City = "Tokyo", MinLevel = 3, Seats = 2006, Strict = 0.3f, Prize = 2000 },
            new Venue { Id = "london", Name = "Royal Albert Hall", City = "London", MinLevel = 4, Seats = 5272, Strict = 0.4f, Prize = 3000 },
            new Venue { Id = "newyork", Name = "Carnegie Hall", City = "New York", MinLevel = 5, Seats = 2804, Strict = 0.5f, Prize = 5000 },
            new Venue { Id = Final, Name = "Musikverein, Golden Hall", City = "Vienna", MinLevel = 6, Seats = 1744, Strict = 0.6f, Prize = 10000 },
        };

        public static Venue Get(string id)
        {
            foreach (var v in Venues) if (v.Id == id) return v;
            return Venues[0];
        }

        public static bool IsOpen(OrchestraState s, Venue v) { return s.level >= v.MinLevel; }
        /// <summary>The hall chosen for the next concert (falls back to home if it is not open).</summary>
        public static Venue Current(OrchestraState s) { var v = Get(s.venue); return IsOpen(s, v) ? v : Venues[0]; }
        public static int Audience(Venue v, float reputation) { return (int)Math.Round(v.Seats * Economy.AudienceFill(reputation)); }

        /// <summary>After an approved concert: the hall's prize the first time. Returns the message, or null.</summary>
        public static string Win(OrchestraState s, Venue v)
        {
            if (s.venuesWon.Contains(v.Id)) return null;
            s.venuesWon.Add(v.Id);
            if (v.Id == Home) return null;
            s.coins += v.Prize;
            s.reputation = Math.Min(100f, s.reputation + WinReputation);
            if (v.Id == Final) { s.wonGame = true; return "THE GOLDEN BATON! Your orchestra conquered Vienna: the best orchestra in the world. Prize " + v.Prize.ToString("N0") + " coins."; }
            return "Triumph in " + v.City + "! Prize " + v.Prize.ToString("N0") + " coins and reputation +" + WinReputation.ToString("0") + ".";
        }

        /// <summary>A surprise at the start of about a third of the days. Returns the message, or null.</summary>
        public static string DailyEvent(OrchestraState s, Random rng)
        {
            if (rng.NextDouble() >= EventChance || s.roster.Count == 0) return null;
            var m = s.roster[rng.Next(s.roster.Count)];
            switch (rng.Next(6))
            {
                case 0: { int c = 100 + 50 * s.level; s.coins += c; return "A sponsor donates " + c + " coins."; }
                case 1: s.reputation = Math.Min(100f, s.reputation + 3f); return "A newspaper critic praises your orchestra: reputation +3.";
                case 2: m.energy = (float)Math.Floor(m.energy / 2f); return m.firstName + " caught a cold: energy halved.";
                case 3:
                {
                    var sk = (SkillType)rng.Next(Skills.Count);
                    m.Set(sk, Math.Min(100f, m.Get(sk) + 2f));
                    return "A guest teacher coached " + m.firstName + ": " + Skills.DisplayName(sk) + " +2.";
                }
                case 4: { int c = Math.Min(s.coins, 50); s.coins -= c; return "New strings and bow rehairs cost " + c + " coins."; }
                default: { int c = 5 * Economy.Audience(s.reputation) / 10; s.coins += c; return "Your last concert was on the radio: " + c + " coins in royalties."; }
            }
        }
    }
}
