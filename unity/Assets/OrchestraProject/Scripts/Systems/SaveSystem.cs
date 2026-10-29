using System;
using System.IO;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// Saves the game as JSON in Application.persistentDataPath
    /// (Windows: %USERPROFILE%\AppData\LocalLow\&lt;Company&gt;\&lt;Product&gt;; iOS: the app's Documents folder).
    /// Writes to a temp file first and keeps a .bak copy, so a crash mid-save never loses progress.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "orchestra_save.json";

        public static string PathMain { get { return Path.Combine(Application.persistentDataPath, FileName); } }
        static string PathBackup { get { return PathMain + ".bak"; } }
        static string PathTemp { get { return PathMain + ".tmp"; } }

        public static bool HasSave { get { return File.Exists(PathMain) || File.Exists(PathBackup); } }

        public static bool Save(OrchestraState state)
        {
            try
            {
                string json = JsonUtility.ToJson(state, true);
                File.WriteAllText(PathTemp, json);
                if (File.Exists(PathMain)) File.Copy(PathMain, PathBackup, true);
                File.Copy(PathTemp, PathMain, true);
                File.Delete(PathTemp);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Save failed: " + e);
                return false;
            }
        }

        public static OrchestraState Load()
        {
            var s = TryLoad(PathMain);
            if (s == null)
            {
                s = TryLoad(PathBackup);
                if (s != null) Debug.LogWarning("Main save was unreadable; restored from backup.");
            }
            return s;
        }

        static OrchestraState TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var s = JsonUtility.FromJson<OrchestraState>(File.ReadAllText(path));
                if (s == null || s.roster == null || s.roster.Count == 0) return null;
                s.Validate();
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not read save " + path + ": " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(PathMain)) File.Delete(PathMain);
                if (File.Exists(PathBackup)) File.Delete(PathBackup);
            }
            catch (Exception e) { Debug.LogError("Delete save failed: " + e); }
        }
    }
}
