using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Choose a piece and arrangement; listen, rehearse, or perform a concert.</summary>
    public class MusicTab
    {
        string pieceId = "twinkle";
        ArrangementMode mode = ArrangementMode.SeparateParts;

        public void Build(Transform root, GameManager gm, UIManager ui)
        {
            var s = gm.State;
            UIFactory.Header(root, "Music library");
            foreach (var p in MusicLibrary.All)
            {
                var piece = p;
                var prog = s.Progress(p.Id);
                var b = UIFactory.Button(root, "", () => { pieceId = piece.Id; ui.Hud.Refresh(); }, 20, 86);
                var t = b.GetComponentInChildren<Text>();
                t.alignment = TextAnchor.MiddleLeft;
                t.text = "<b>" + p.Title + "</b>\n<size=16>" + p.Composer + "  ·  " + p.Difficulty + "  ·  prepared " + (prog.preparation * 100f).ToString("0") + "%" +
                         (prog.bestConcertRating > 0 ? "  ·  best concert " + prog.bestConcertRating.ToString("0") : "") + "</size>";
                UIFactory.SetSelected(b, p.Id == pieceId);
            }
            var more = UIFactory.Label(root, "More pieces (Bach, Vivaldi, Saint-Saëns, Elgar…) arrive with the library expansion stage.", 16, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(more, 44);

            var sel = MusicLibrary.Get(pieceId);
            UIFactory.Header(root, "Arrangement");
            var row = UIFactory.Row(root, 60);
            var same = UIFactory.Button(row, "Same Melody", () => { mode = ArrangementMode.SameMelody; ui.Hud.Refresh(); }, 20, 60);
            var sep = UIFactory.Button(row, "Separate Parts", () => { mode = ArrangementMode.SeparateParts; ui.Hud.Refresh(); }, 20, 60);
            UIFactory.SetSelected(same, mode == ArrangementMode.SameMelody);
            UIFactory.SetSelected(sep, mode == ArrangementMode.SeparateParts);

            var instruments = new System.Collections.Generic.List<InstrumentType>();
            foreach (var m in s.roster) if (!instruments.Contains(m.instrument)) instruments.Add(m.instrument);
            var arr = Arranger.Arrange(sel, mode, instruments);
            var sb = new System.Text.StringBuilder();
            foreach (var kv in arr.Parts) sb.Append(InstrumentCatalog.Get(kv.Key).DisplayName).Append(": ").Append(kv.Value.Role).Append("\n");
            var roles = UIFactory.Label(root, sb.ToString().TrimEnd(), 18, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(roles, 26 * arr.Parts.Count + 6);
            var info = UIFactory.Label(root, sel.Character + "  ·  " + sel.Bpm + " bpm  ·  " + sel.BeatsPerBar + "/4\n<size=15>" + sel.Rights + "</size>", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(info, 52);

            UIFactory.Header(root, "Play");
            bool busy = gm.Performance.IsBusy;
            var listen = UIFactory.Button(root, "Listen  (no training, no energy)", () => gm.Listen(pieceId, mode), 20, 62);
            var rehearse = UIFactory.Button(root, "Rehearse with playback  (−" + TrainingSystem.EnergyPerRehearsal + " energy)", () => gm.RehearseWithPlayback(pieceId, mode), 20, 62);
            var quick = UIFactory.Button(root, "Quick rehearsal  (instant, −" + TrainingSystem.EnergyPerRehearsal + " energy)", () => gm.QuickRehearsal(pieceId), 20, 62);
            listen.interactable = rehearse.interactable = quick.interactable = !busy;

            UIFactory.Header(root, "Concert");
            string reason;
            bool can = gm.CanPerformConcert(out reason);
            var concert = UIFactory.Button(root, "Perform Concert: " + sel.Title, () => gm.PerformConcert(pieceId, mode), 21, 70, UITheme.AccentDim);
            concert.interactable = can && !busy;
            var cl = UIFactory.Label(root, can ? "The audience is waiting. Rating depends on skills, rehearsal preparation, difficulty, accuracy and ensemble. Good concerts raise reputation, which attracts better musicians."
                : reason, 17, TextAnchor.UpperLeft, can ? UITheme.TextDim : UITheme.Bad);
            UIFactory.Size(cl, 70);
            var rep = UIFactory.Label(root, "Reputation " + s.reputation.ToString("0") + "  ·  concerts " + s.concertsPerformed + "  ·  best rating " + s.bestConcertRating.ToString("0"), 17, TextAnchor.UpperLeft, UITheme.Text);
            UIFactory.Size(rep, 30);
        }
    }
}
