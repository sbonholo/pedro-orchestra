using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>Places a MusicianView on stage for every roster member and keeps them in seating order.</summary>
    public class StageManager : MonoBehaviour
    {
        readonly Dictionary<string, MusicianView> views = new Dictionary<string, MusicianView>();
        MusicianView auditionView;
        string selectedId;

        public IEnumerable<MusicianView> Views { get { return views.Values; } }

        public MusicianView Get(string id)
        {
            MusicianView v;
            if (id != null && views.TryGetValue(id, out v)) return v;
            if (auditionView != null && auditionView.Data.id == id) return auditionView;
            return null;
        }

        public void Sync(List<MusicianData> roster)
        {
            var keep = new HashSet<string>();
            foreach (var m in roster) keep.Add(m.id);
            var remove = new List<string>();
            foreach (var kv in views) if (!keep.Contains(kv.Key)) remove.Add(kv.Key);
            foreach (var id in remove) { Destroy(views[id].gameObject); views.Remove(id); }

            foreach (var m in roster)
                if (!views.ContainsKey(m.id) && m.Instrument.Implemented)
                    views[m.id] = Spawn(m, "Musician - " + m.FullName);

            var seats = SeatingLayout.Compute(roster);
            for (int i = 0; i < roster.Count; i++)
            {
                MusicianView v;
                if (!views.TryGetValue(roster[i].id, out v)) continue;
                v.transform.position = seats[i].Position;
                v.transform.rotation = Quaternion.Euler(0, seats[i].Yaw, 0);
            }
            SetSelected(selectedId);
        }

        MusicianView Spawn(MusicianData m, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var v = go.AddComponent<MusicianView>();
            v.Build(m);
            return v;
        }

        public void SetSelected(string id)
        {
            selectedId = id;
            foreach (var kv in views) kv.Value.SetSelected(kv.Key == id);
        }

        /// <summary>Audition candidate playing alone at the front of the stage, facing the hall.</summary>
        public MusicianView ShowAuditionCandidate(MusicianData candidate)
        {
            HideAuditionCandidate();
            auditionView = Spawn(candidate, "Audition - " + candidate.FullName);
            auditionView.transform.position = new Vector3(0f, ConcertHallBuilder.StageHeight, -0.6f);
            auditionView.transform.rotation = Quaternion.Euler(0, 180f, 0);
            foreach (var v in views.Values) v.gameObject.SetActive(false);
            return auditionView;
        }

        public void HideAuditionCandidate()
        {
            if (auditionView != null) Destroy(auditionView.gameObject);
            auditionView = null;
            foreach (var v in views.Values) v.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Arcs of seats around the conductor, filled in instrument order (violins on the left,
    /// basses on the right) — the standard modern orchestra layout. Grows with the roster.
    /// </summary>
    public static class SeatingLayout
    {
        public struct Seat { public Vector3 Position; public float Yaw; }

        public static Seat[] Compute(List<MusicianData> roster)
        {
            var order = new List<int>();
            for (int i = 0; i < roster.Count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                int c = roster[a].Instrument.SeatingOrder.CompareTo(roster[b].Instrument.SeatingOrder);
                return c != 0 ? c : roster[b].Overall.CompareTo(roster[a].Overall); // section leader first
            });

            var seats = new Seat[roster.Count];
            Vector3 c0 = ConcertHallBuilder.ConductorPoint;
            int placed = 0, row = 0;
            while (placed < order.Count)
            {
                int capacity = 4 + row * 4;
                int inRow = Mathf.Min(capacity, order.Count - placed);
                float radius = 2.3f + row * 1.35f;
                float arc = Mathf.Min(68f, 17f * inRow + 10f);
                for (int k = 0; k < inRow; k++)
                {
                    float angle = inRow == 1 ? 0f : Mathf.Lerp(-arc, arc, k / (float)(inRow - 1));
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 pos = c0 + new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
                    pos.y = ConcertHallBuilder.StageHeight;
                    Vector3 look = c0 - pos; look.y = 0f;
                    seats[order[placed + k]] = new Seat { Position = pos, Yaw = Quaternion.LookRotation(look).eulerAngles.y };
                }
                placed += inRow;
                row++;
            }
            return seats;
        }
    }
}
