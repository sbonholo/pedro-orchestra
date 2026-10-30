using UnityEngine;

namespace OrchestraProject
{
    /// <summary>Spend concert coins: masterclasses (+3 to a skill) and better instruments, up to the Stradivarius.</summary>
    public class ShopTab
    {
        static readonly SkillType[] Order =
        {
            SkillType.Intonation, SkillType.Rhythm, SkillType.Accuracy, SkillType.ToneQuality, SkillType.Expression, SkillType.Musicality,
            SkillType.Ensemble, SkillType.Technique, SkillType.Speed, SkillType.SightReading, SkillType.Confidence
        };

        static string Plain(SkillType s)
        {
            switch (s)
            {
                case SkillType.Intonation: return "plays in tune";
                case SkillType.Rhythm: return "plays notes on time";
                case SkillType.Accuracy: return "fewer wrong notes";
                case SkillType.ToneQuality: return "more beautiful sound";
                case SkillType.Expression: return "vibrato and feeling";
                case SkillType.Musicality: return "shapes the phrases";
                case SkillType.Ensemble: return "stays together";
                case SkillType.Technique: return "cleaner playing";
                case SkillType.Speed: return "plays fast pieces";
                case SkillType.SightReading: return "new pieces";
                default: return "calm on stage";
            }
        }

        public void Build(Transform root, GameManager gm, UIManager ui)
        {
            var s = gm.State;
            var coins = UIFactory.Label(root, "<b>" + s.coins.ToString("N0") + " coins</b>   <size=16>Concerts earn " + Economy.ApprovedCoins(s.level) + " coins when the judges approve, " + Economy.CoinsRejected + " when they don't.</size>", 22, TextAnchor.UpperLeft, UITheme.Accent);
            UIFactory.Size(coins, 60);
            var m = gm.Selected;
            if (m == null) { UIFactory.Label(root, "Select a musician (Roster or tap one on stage) to upgrade them.", 19); return; }

            UIFactory.Header(root, "Masterclasses for " + m.FullName + "  (+" + Economy.MasterclassGain.ToString("0") + " per lesson)");
            foreach (var sk in Order)
            {
                var skill = sk;
                float v = m.Get(skill);
                int cost = Economy.MasterclassCost(v);
                var b = UIFactory.Button(root, Skills.DisplayName(skill) + " " + v.ToString("0") + "  <size=15>(" + Plain(skill) + ")</size>   —   " + (v >= 100f ? "maximum" : cost + " coins"),
                    () => { gm.Notify(Economy.Masterclass(s, m, skill)); gm.Save(); ui.Hud.Refresh(); }, 18, 50);
                b.interactable = v < 100f && s.coins >= cost;
            }

            UIFactory.Header(root, "Instruments for " + m.FullName);
            var current = Economy.Get(m.instrumentModel);
            var cur = UIFactory.Label(root, "Playing: <b>" + (current != null ? current.Name : "own student " + m.Instrument.DisplayName.ToLowerInvariant()) + "</b>. Better instruments add up to +20 Tone, +8 Expression and +4 Intonation.", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(cur, 50);
            foreach (var im in Economy.Catalog)
            {
                if (im.Type != m.instrument) continue;
                var model = im;
                int copies = Economy.Copies(s, im.Id);
                bool owned = Economy.Spares(s, im.Id, m) > 0, playing = m.instrumentModel == im.Id;
                string label = "<b>" + im.Name + "</b>  <size=15>" + im.Maker + " · quality " + (im.Quality * 100f).ToString("0") + (copies > 1 ? " · you own " + copies : "") + "</size>   —   "
                               + (playing ? "playing" : owned ? "owned: give to " + m.firstName : (copies > 0 ? "buy another: " : "") + im.Price.ToString("N0") + " coins");
                var b = UIFactory.Button(root, label, () => { gm.Notify(Economy.Buy(s, m, model)); gm.Save(); ui.Hud.Refresh(); }, 18, 56, playing ? UITheme.AccentDim : (Color?)null);
                b.interactable = !playing && (owned || s.coins >= im.Price);
            }
        }
    }
}
