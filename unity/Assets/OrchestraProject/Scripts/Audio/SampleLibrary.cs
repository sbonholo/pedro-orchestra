using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// Loads recorded instrument samples from Resources/OrchestraSamples/&lt;Instrument&gt;/samples.txt.
    /// An instrument with recordings plays them; one without falls back to the synthesizer.
    /// To add recordings for another instrument, copy the Cello folder's format.
    /// </summary>
    public static class SampleLibrary
    {
        static readonly Dictionary<InstrumentType, SampleSet> loaded = new Dictionary<InstrumentType, SampleSet>();
        static readonly HashSet<InstrumentType> missing = new HashSet<InstrumentType>();

        /// <summary>Main thread only (uses Resources). Returns null if the instrument has no recordings.</summary>
        public static SampleSet Get(InstrumentType t)
        {
            SampleSet set;
            if (loaded.TryGetValue(t, out set) && set != null) return set;
            if (missing.Contains(t)) return null;

            string folder = "OrchestraSamples/" + t;
            var manifest = Resources.Load<TextAsset>(folder + "/samples");
            if (manifest == null) { missing.Add(t); return null; }
            try
            {
                set = SampleSet.Parse(t, manifest.text, file =>
                {
                    var asset = Resources.Load<TextAsset>(folder + "/" + file);
                    return asset != null ? asset.bytes : null;
                });
                loaded[t] = set;
                Debug.Log("Loaded " + set.Samples.Count + " recorded samples for " + t);
                return set;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Could not load samples for " + t + ": " + e.Message + " — using the synthesizer instead.");
                missing.Add(t);
                return null;
            }
        }
    }
}
