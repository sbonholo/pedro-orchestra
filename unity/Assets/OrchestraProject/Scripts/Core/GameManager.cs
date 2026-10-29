using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// The game's "director": owns the saved state and connects the systems (training, recruitment,
    /// performance, stage, camera, UI). UI screens call these methods; they never change state directly.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public OrchestraState State { get; private set; }
        public System.Random Rng { get; private set; }
        public StageManager Stage { get; private set; }
        public PerformanceController Performance { get; private set; }
        public CameraController Cam { get; private set; }
        public RhythmMiniGame Rhythm { get; private set; }
        public bool InGame { get { return State != null; } }

        public string SelectedId { get; private set; }
        public MusicianData Selected { get { return State != null ? State.FindMusician(SelectedId) : null; } }

        public event Action StateChanged;
        public event Action SelectionChanged;
        public event Action<string> Message;
        public event Action<PerformanceResult, TrainingResult> PerformanceFinished;

        public void Init(StageManager stage, PerformanceController perf, CameraController cam, RhythmMiniGame rhythm)
        {
            Instance = this;
            Stage = stage; Performance = perf; Cam = cam; Rhythm = rhythm;
            Performance.Init(stage);
            Cam.CloseUpTarget = () =>
            {
                var v = Stage.Get(SelectedId);
                return v != null ? v.transform : null;
            };
            Cam.MusicianTapped += v => { if (State != null && State.FindMusician(v.Data.id) != null) Select(v.Data.id); };
            Rng = new System.Random();
        }

        // ---------------- Game lifecycle ----------------

        public void NewGame()
        {
            Performance.Stop();
            int seed = Environment.TickCount;
            State = OrchestraState.NewGame(seed);
            Rng = new System.Random(seed);
            RecruitmentSystem.Refresh(State, Rng);
            AfterLoad();
            Notify("Welcome, Music Director. Your four string players are waiting on stage.");
        }

        public bool Continue()
        {
            var loaded = SaveSystem.Load();
            if (loaded == null) return false;
            Performance.Stop();
            State = loaded;
            Rng = new System.Random(State.randomSeed + State.day * 7919);
            AfterLoad();
            Notify("Welcome back. Day " + State.day + ".");
            return true;
        }

        void AfterLoad()
        {
            Stage.Sync(State.roster);
            Select(State.roster.Count > 0 ? State.roster[0].id : null);
            Cam.SetMode(CameraMode.Orchestra);
            Save();
        }

        public void Save()
        {
            if (State != null && !SaveSystem.Save(State)) Notify("Warning: the game could not be saved (see Console).");
            if (StateChanged != null) StateChanged();
        }

        public void Notify(string msg)
        {
            if (Message != null) Message(msg);
        }

        public void Select(string id)
        {
            SelectedId = id;
            Stage.SetSelected(id);
            if (Cam.Mode == CameraMode.CloseUp) Cam.SetMode(CameraMode.CloseUp);
            if (SelectionChanged != null) SelectionChanged();
        }

        // ---------------- Training ----------------

        public TrainingResult QuickPractice(SkillType focus)
        {
            var m = Selected;
            if (m == null) { Notify("Select a musician first."); return null; }
            if (Performance.IsBusy) { Notify("Wait until the current performance ends."); return null; }
            var r = TrainingSystem.Practice(m, focus, 1f, Rng);
            Notify(r.Summary());
            Save();
            return r;
        }

        public void SetTrainingPlan(MusicianData m, int focus)
        {
            m.trainingFocus = focus;
            Notify(m.firstName + "'s daily plan: " + (focus < 0 ? "Balanced (weakest skill first)" : Skills.DisplayName((SkillType)focus)) +
                   (State.playStyle == (int)PlayStyle.Director ? "" : " (applies in Director play style)"));
            Save();
        }

        public void AdvanceDays(int days)
        {
            if (Performance.IsBusy) { Notify("Wait until the current performance ends."); return; }
            string last = "";
            for (int i = 0; i < days; i++)
            {
                last = TrainingSystem.AdvanceDay(State, Rng);
                if (State.day - State.lastPoolRefreshDay >= RecruitmentSystem.RefreshEveryDays)
                {
                    RecruitmentSystem.Refresh(State, Rng);
                    last += " New musicians are applying for auditions.";
                }
            }
            Notify(days > 1 ? days + " days pass. " + last : last);
            Save();
        }

        public TrainingResult ApplyRhythmPractice(MusicianData m, float score01)
        {
            var r = TrainingSystem.Practice(m, SkillType.Rhythm, 0.3f + 1.7f * score01, Rng);
            Save();
            return r;
        }

        // ---------------- Music ----------------

        public void Listen(string pieceId, ArrangementMode mode) { StartPerformance(pieceId, mode, PerformanceContext.Preview); }
        public void RehearseWithPlayback(string pieceId, ArrangementMode mode) { StartPerformance(pieceId, mode, PerformanceContext.Rehearsal); }

        public bool CanPerformConcert(out string reason)
        {
            reason = null;
            if (State.lastConcertDay == State.day) { reason = "The hall is booked once per day. Advance to the next day."; return false; }
            int tired = State.roster.FindAll(m => m.energy < TrainingSystem.EnergyPerConcert).Count;
            if (tired > 0) { reason = tired + " musician(s) need more energy (" + TrainingSystem.EnergyPerConcert + "). Rest first."; return false; }
            return true;
        }

        public void PerformConcert(string pieceId, ArrangementMode mode)
        {
            string reason;
            if (!CanPerformConcert(out reason)) { Notify(reason); return; }
            Cam.SetMode(CameraMode.Audience);
            StartPerformance(pieceId, mode, PerformanceContext.Concert);
        }

        public void QuickRehearsal(string pieceId)
        {
            if (Performance.IsBusy) { Notify("Wait until the current performance ends."); return; }
            var r = TrainingSystem.Rehearse(State.roster, State.Progress(pieceId), Rng);
            Notify(r.Summary());
            Save();
        }

        void StartPerformance(string pieceId, ArrangementMode mode, PerformanceContext ctx)
        {
            var piece = MusicLibrary.Get(pieceId);
            if (piece == null) { Notify("Unknown piece."); return; }
            var players = State.roster.FindAll(m => m.Instrument.Implemented);
            if (players.Count == 0) { Notify("Nobody to play."); return; }
            if (ctx == PerformanceContext.Rehearsal && players.TrueForAll(m => m.energy < TrainingSystem.MinEnergyToWork))
            { Notify("Everyone is too tired to rehearse. Advance to the next day."); return; }

            float prep = State.Progress(pieceId).preparation;
            Performance.Perform(piece, mode, players, ctx, prep, result => OnPerformanceDone(result, players));
            if (StateChanged != null) StateChanged();
        }

        void OnPerformanceDone(PerformanceResult result, List<MusicianData> players)
        {
            TrainingResult gains = null;
            var progress = State.Progress(result.Piece.Id);
            switch (result.Context)
            {
                case PerformanceContext.Rehearsal:
                    gains = TrainingSystem.Rehearse(players, progress, Rng);
                    break;
                case PerformanceContext.Concert:
                    State.lastConcertDay = State.day;
                    gains = new TrainingResult { Success = true, Message = "Concert experience:" };
                    TrainingSystem.ApplyConcertExperience(players, result.Overall / 100f, gains);
                    float cap = Mathf.Clamp01(1f - State.reputation / (20f + 20f * (int)result.Piece.Difficulty));
                    float repGain = Mathf.Max(0.3f, (result.Overall - 30f) / 10f) * Mathf.Max(0.1f, cap);
                    State.reputation = Mathf.Min(100f, State.reputation + repGain);
                    State.concertsPerformed++;
                    State.bestConcertRating = Mathf.Max(State.bestConcertRating, result.Overall);
                    progress.bestConcertRating = Mathf.Max(progress.bestConcertRating, result.Overall);
                    gains.Message += " Reputation +" + repGain.ToString("0.0") + " (now " + State.reputation.ToString("0") + ").";
                    if (AudienceCrowd.Instance != null) AudienceCrowd.Instance.Applaud(result.Stars, State.reputation);
                    State.Log("Concert: " + result.Piece.Title + " — " + result.Stars + "/5 (" + result.Overall.ToString("0") + ").");
                    break;
            }
            Save();
            if (PerformanceFinished != null) PerformanceFinished(result, gains);
        }

        public void StopPerformance()
        {
            Performance.Stop();
            if (StateChanged != null) StateChanged();
        }

        // ---------------- Recruitment ----------------

        public void HearAudition(MusicianData candidate)
        {
            var piece = MusicLibrary.All[Rng.Next(MusicLibrary.All.Count)];
            Cam.SetMode(CameraMode.Audience);
            Performance.Perform(piece, ArrangementMode.SameMelody, new List<MusicianData> { candidate }, PerformanceContext.Audition, 0.6f,
                result =>
                {
                    if (PerformanceFinished != null) PerformanceFinished(result, null);
                });
            Notify(candidate.FullName + " plays " + piece.Title + " for you.");
            if (StateChanged != null) StateChanged();
        }

        public void Hire(MusicianData candidate)
        {
            Notify(RecruitmentSystem.Hire(State, candidate));
            Stage.Sync(State.roster);
            Select(candidate.id);
            Save();
        }

        public void Invite(MusicianData candidate)
        {
            bool accepted;
            Notify(RecruitmentSystem.Invite(State, candidate, Rng, out accepted));
            if (accepted) { Stage.Sync(State.roster); Select(candidate.id); }
            Save();
        }

        // ---------------- Settings ----------------

        public void SetPlayStyle(PlayStyle style)
        {
            if (State == null) return;
            State.playStyle = (int)style;
            Save();
        }

        void OnApplicationPause(bool paused) { if (paused && State != null) SaveSystem.Save(State); }
        void OnApplicationQuit() { if (State != null) SaveSystem.Save(State); }
    }
}
