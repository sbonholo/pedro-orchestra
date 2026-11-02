using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Choose a piece and arrangement; listen, rehearse, or perform a concert.</summary>
    public class MusicTab
    {
        string pieceId = "twinkle";
        ArrangementMode mode = ArrangementMode.SeparateParts;
        public string PieceId { get { return pieceId; } }
        public ArrangementMode Mode { get { return mode; } }

        public void Build(Transform root, GameManager gm, UIManager ui)
        {
            var s = gm.State;
            UIFactory.Header(root, "Music library");
            // Easiest first, with a heading per level: Beginner, Intermediate, Advanced, Virtuoso.
            var sorted = new System.Collections.Generic.List<PieceDefinition>(MusicLibrary.All);
            for (int a = 1; a < sorted.Count; a++)   // stable insertion sort by difficulty
                for (int b = a; b > 0 && sorted[b - 1].Difficulty > sorted[b].Difficulty; b--) { var tmp = sorted[b]; sorted[b] = sorted[b - 1]; sorted[b - 1] = tmp; }
            Difficulty? shown = null;
            foreach (var p in sorted)
            {
                var piece = p;
                if (shown != p.Difficulty) { shown = p.Difficulty; var hd = UIFactory.Label(root, "<b>" + p.Difficulty + "</b>", 22, TextAnchor.LowerLeft, UITheme.Accent); UIFactory.Size(hd, 40); }
                var prog = s.Progress(p.Id);
                var b = UIFactory.Button(root, "", () => { pieceId = piece.Id; gm.Soloist = piece.Featured; ui.Hud.Refresh(); }, 20, 86);
                var t = b.GetComponentInChildren<Text>();
                t.alignment = TextAnchor.MiddleLeft;
                t.text = "<b>" + p.Title + "</b>\n<size=16>" + p.Composer + "  ·  " + p.Difficulty + "  ·  " + (p.Unaccompanied ? "solo " : "featured ") + InstrumentCatalog.Get(p.Featured).DisplayName.ToLowerInvariant() + "  ·  prepared " + (prog.preparation * 100f).ToString("0") + "%" +
                         (prog.bestConcertRating > 0 ? "  ·  best concert " + prog.bestConcertRating.ToString("0") : "") + "</size>";
                UIFactory.SetSelected(b, p.Id == pieceId);
            }
            var sel = MusicLibrary.Get(pieceId);
            UIFactory.Header(root, "Arrangement");
            var row = UIFactory.Row(root, 60);
            var same = UIFactory.Button(row, "Same Melody", () => { mode = ArrangementMode.SameMelody; ui.Hud.Refresh(); }, 20, 60);
            var sep = UIFactory.Button(row, "Separate Parts", () => { mode = ArrangementMode.SeparateParts; ui.Hud.Refresh(); }, 20, 60);
            UIFactory.SetSelected(same, mode == ArrangementMode.SameMelody);
            var solo = UIFactory.Button(row, "Solo", () => { mode = ArrangementMode.Solo; ui.Hud.Refresh(); }, 20, 60);
            UIFactory.SetSelected(sep, mode == ArrangementMode.SeparateParts);
            UIFactory.SetSelected(solo, mode == ArrangementMode.Solo);
            if (mode == ArrangementMode.Solo)
            {
                // Soloist: any instrument the orchestra has. The best player of that instrument plays the solo.
                var soloRow = UIFactory.Row(root, 56);
                foreach (InstrumentType it in new[] { InstrumentType.Violin, InstrumentType.Viola, InstrumentType.Cello, InstrumentType.DoubleBass })
                {
                    var inst = it;
                    if (!s.roster.Exists(m => m.instrument == inst)) continue;
                    var sb2 = UIFactory.Button(soloRow, InstrumentCatalog.Get(inst).DisplayName, () => { gm.Soloist = inst; ui.Hud.Refresh(); }, 18, 56);
                    UIFactory.SetSelected(sb2, gm.Soloist == inst);
                }
                var who = Arranger.SoloPlayers(s.roster, gm.Soloist, sel.Unaccompanied);
                var wl = UIFactory.Label(root, who.Count == 0 ? "Nobody plays this instrument yet. Hire one in Auditions."
                    : "Soloist: <b>" + who[0].FullName + "</b>" + " plays alone", 17, TextAnchor.UpperLeft, UITheme.TextDim);
                UIFactory.Size(wl, 30);
            }

            // Conductor: the automatic conductor, or you tap the beat (steady beating adds up to +6 points, none costs 6).
            UIFactory.Header(root, "Conductor");
            var crow = UIFactory.Row(root, 56);
            var cAuto = UIFactory.Button(crow, "Automatic", () => { s.playerConducts = false; gm.Save(); ui.Hud.Refresh(); }, 19, 56);
            var cYou = UIFactory.Button(crow, "You conduct (tap the beat)", () => { s.playerConducts = true; gm.Save(); ui.Hud.Refresh(); }, 19, 56);
            UIFactory.SetSelected(cAuto, !s.playerConducts);
            UIFactory.SetSelected(cYou, s.playerConducts);

            // Main action first, so it is visible without scrolling.
            UIFactory.Header(root, "Concert");
            var lvl = UIFactory.Label(root, "Your orchestra: <b>" + Economy.LevelName(s.level) + "</b> (level " + (s.level + 1) + " of " + Economy.Levels.Length + ")" +
                (s.level < Economy.MaxLevel ? ". Win the judges' approval (3 of 5 give " + Economy.PassMark(s.level).ToString("0.0") + "+) to be promoted to the <b>" + Economy.LevelName(s.level + 1) + "</b>." : ": the top of the world."), 17, TextAnchor.UpperLeft, UITheme.Accent);
            UIFactory.Size(lvl, 50);

            // World tour: each career level opens a famous hall; win the Golden Hall in Vienna to win the game.
            var cur = Tour.Current(s);
            var tl = UIFactory.Label(root, s.wonGame ? "<b>You hold the Golden Baton.</b> Keep touring for coins and glory."
                : "<b>World tour:</b> " + (s.venuesWon.FindAll(v => v != Tour.Home).Count) + " of " + (Tour.Venues.Length - 1) + " halls won. Win the judges' approval in the Golden Hall, Vienna, to win the Golden Baton.", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(tl, 46);
            int dtb = Tour.DaysToBattle(s.day);
            var bl = UIFactory.Label(root, "<b>Battle of the Orchestras</b> " + (dtb == 0 ? "is TODAY" : "in " + dtb + " day" + (dtb == 1 ? "" : "s")) + ": beat " + Tour.RivalName(s.day + dtb) + " (about " + Tour.RivalScore(s.level, s.day + dtb) + " points) for " + Tour.BattlePrize(s.level).ToString("N0") + " coins. 5-star concerts earn an encore.",
                17, TextAnchor.UpperLeft, dtb == 0 ? UITheme.Good : UITheme.TextDim);
            UIFactory.Size(bl, 46);
            foreach (var v in Tour.Venues)
            {
                var venue = v; bool open = Tour.IsOpen(s, v), won = s.venuesWon.Contains(v.Id);
                string label = (won ? "★ " : "") + "<b>" + v.Name + "</b>, " + v.City + "  <size=15>" + (open
                    ? v.Seats.ToString("N0") + " seats" + (v.Prize > 0 ? " · prize " + v.Prize.ToString("N0") + (won ? " (won)" : "") : "") + " · judges +" + v.Strict.ToString("0.0")
                    : "opens at " + Economy.LevelName(v.MinLevel)) + "</size>";
                var vb = UIFactory.Button(root, label, () => { s.venue = venue.Id; gm.Save(); ui.Hud.Refresh(); }, 17, 46);
                vb.interactable = open;
                UIFactory.SetSelected(vb, cur == v);
            }
            bool busy = gm.Performance.IsBusy;
            string reason;
            bool can = gm.CanPerformConcert(out reason);
            var concert = UIFactory.Button(root, "Perform Concert: " + sel.Title + "  <size=16>(" + cur.City + ")</size>", () => gm.PerformConcert(pieceId, mode), 21, 70, UITheme.AccentDim);
            concert.interactable = can && !busy;
            var cl = UIFactory.Label(root, can ? "Rated on rhythm, intonation, accuracy, ensemble and artistry. Good concerts raise reputation, which attracts better musicians."
                : reason, 17, TextAnchor.UpperLeft, can ? UITheme.TextDim : UITheme.Bad);
            UIFactory.Size(cl, 50);

            UIFactory.Header(root, "Rehearse");
            var playRow = UIFactory.Row(root, 62);
            var listen = UIFactory.Button(playRow, "Listen", () => { gm.State.heardOrchestra = true; gm.Save(); gm.Listen(pieceId, mode); }, 20, 62);
            var rehearse = UIFactory.Button(playRow, "Rehearse", () => gm.RehearseWithPlayback(pieceId, mode), 20, 62);
            var quick = UIFactory.Button(playRow, "Quick rehearsal", () => gm.QuickRehearsal(pieceId), 20, 62);
            listen.interactable = rehearse.interactable = quick.interactable = !busy;
            var pl = UIFactory.Label(root, "Listen: no training, no energy. Rehearse (with playback) and Quick rehearsal (instant) build preparation for this piece and cost " + TrainingSystem.EnergyPerRehearsal + " energy.", 16, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(pl, 44);

            UIFactory.Header(root, "About this piece");
            var instruments = new System.Collections.Generic.List<InstrumentType>();
            foreach (var m in s.roster) if (!instruments.Contains(m.instrument)) instruments.Add(m.instrument);
            var arr = Arranger.Arrange(sel, mode, instruments, gm.Soloist);
            var sb = new System.Text.StringBuilder();
            foreach (var kv in arr.Parts) sb.Append(InstrumentCatalog.Get(kv.Key).DisplayName).Append(": ").Append(kv.Value.Role).Append("\n");
            var roles = UIFactory.Label(root, sb.ToString().TrimEnd(), 18, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(roles, 26 * arr.Parts.Count + 6);
            var info = UIFactory.Label(root, sel.Character + "  ·  " + sel.Bpm + " bpm  ·  " + sel.BeatsPerBar + "/4\n<size=15>" + sel.Rights + "</size>", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(info, 52);
            var rep = UIFactory.Label(root, "Reputation " + s.reputation.ToString("0") + "  ·  concerts " + s.concertsPerformed + "  ·  best rating " + s.bestConcertRating.ToString("0"), 17, TextAnchor.UpperLeft, UITheme.Text);
            UIFactory.Size(rep, 30);
            var more = UIFactory.Label(root, "Elgar's Cello Concerto and Saint-Saëns arrive with the next library expansion.", 16, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(more, 44);
        }
    }
}
