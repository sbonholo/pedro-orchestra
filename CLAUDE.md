# Orchestra Project — agent notes

- This repo (`sbonholo/b3la`) is the ONLY home of the orchestra game. Never put game files in `sbonholo/beija` (UMEVI).
- The Unity project (`unity/`) is the product. `web/` is a browser port of the same rules: change a game rule in BOTH places (constants live in `TrainingSystem.cs`, `PerformanceRenderer.cs`, `PerformanceEvaluator.cs` and the matching JS).
- Before committing C# changes, run `tests/core`, `tests/pitch` and `tests/unity-compile` (see README). Unity itself cannot run in the cloud session.
- Audio must be licence-clean for App Store release: CC0 or explicitly commercial-OK only. Record sources in `CREDITS.md`. Sonatina (CC Sampling Plus) is NOT acceptable for the shipped game.
- Owner communication: answer first, few words, no process recap.
- Republish the web preview to the same artifact URL (README) after changing `web/`.
