namespace OrchestraProject
{
    public enum NextStepAction { Listen, Rehearse, Concert, Recruit, NextDay }

    public struct NextStep
    {
        public NextStepAction Action;
        public string Text, Label;
    }

    /// <summary>
    /// The "Next step" card: one suggested action, so the game can be played with a single button.
    /// Same rules as nextStep() in the web preview.
    /// </summary>
    public static class NextStepAdvisor
    {
        public const float ConcertReadyPreparation = 0.6f;

        public static NextStep Get(OrchestraState s, PieceDefinition piece)
        {
            var pr = s.Progress(piece.Id);
            int pct = (int)System.Math.Round(pr.preparation * 100f);
            bool canWork = false, canConcert = s.lastConcertDay != s.day;
            foreach (var m in s.roster)
            {
                if (m.energy >= TrainingSystem.MinEnergyToWork) canWork = true;
                if (m.energy < TrainingSystem.EnergyPerConcert) canConcert = false;
            }

            if (!s.heardOrchestra)
                return Step(NextStepAction.Listen, "Hear how your " + (s.roster.Count == 4 ? "quartet" : "orchestra") + " sounds today. Listen for wrong notes and players drifting apart.", "Listen: " + piece.Title);
            if (canConcert && pr.preparation >= ConcertReadyPreparation)
                return Step(NextStepAction.Concert, "“" + piece.Title + "” is " + pct + "% prepared. Time to play it for an audience.", "Perform Concert");
            if (canWork && pr.preparation < ConcertReadyPreparation)
                return Step(NextStepAction.Rehearse, "Rehearse “" + piece.Title + "” together. It is " + pct + "% prepared; aim for 60% before the concert.", "Rehearse");

            MusicianData best = null; float bestGain = 0f;
            foreach (var c in s.auditionPool)
            {
                MusicianData lead = null;
                foreach (var m in s.roster) if (m.instrument == c.instrument && (lead == null || m.Overall > lead.Overall)) lead = m;
                float gain = lead != null ? c.Overall - lead.Overall : 99f;
                if (gain >= 3f && gain > bestGain) { best = c; bestGain = gain; }
            }
            if (best != null && s.lastConcertDay == s.day)
                return Step(NextStepAction.Recruit, best.FullName + ", a " + best.Instrument.PlayerTitle.ToLowerInvariant() + " "
                    + (bestGain >= 99f ? "for a new section" : bestGain.ToString("0") + " points stronger than yours") + ", has applied.", "See applicant");

            return Step(NextStepAction.NextDay, s.lastConcertDay == s.day
                ? "Concert done. Rest, and the musicians train their daily plans overnight."
                : "Your musicians need rest. They train their daily plans overnight.", "Next Day");
        }

        static NextStep Step(NextStepAction a, string text, string label) { return new NextStep { Action = a, Text = text, Label = label }; }
    }
}
