using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrchestraProject
{
    /// <summary>
    /// Collects many boxes/cylinders and merges them into one mesh per material.
    /// A concert hall with ~1000 seats becomes a handful of draw calls — important on iPhone.
    /// </summary>
    public class BatchBuilder
    {
        static Mesh cube, cylinder, sphere;
        readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();

        public static Mesh CubeMesh { get { Init(); return cube; } }
        public static Mesh CylinderMesh { get { Init(); return cylinder; } }
        public static Mesh SphereMesh { get { Init(); return sphere; } }

        static void Init()
        {
            if (cube != null) return;
            cube = Grab(PrimitiveType.Cube);
            cylinder = Grab(PrimitiveType.Cylinder);
            sphere = Grab(PrimitiveType.Sphere);
        }

        static Mesh Grab(PrimitiveType t)
        {
            var go = GameObject.CreatePrimitive(t);
            var m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return m;
        }

        public void Box(Material mat, Vector3 center, Vector3 size, Quaternion? rot = null)
        {
            Add(mat, CubeMesh, Matrix4x4.TRS(center, rot ?? Quaternion.identity, size));
        }

        /// <summary>Cylinder standing on Y; size.y is the full height.</summary>
        public void Cylinder(Material mat, Vector3 center, float diameter, float height, Quaternion? rot = null)
        {
            Add(mat, CylinderMesh, Matrix4x4.TRS(center, rot ?? Quaternion.identity, new Vector3(diameter, height * 0.5f, diameter)));
        }

        public void Sphere(Material mat, Vector3 center, Vector3 size)
        {
            Add(mat, SphereMesh, Matrix4x4.TRS(center, Quaternion.identity, size));
        }

        public void Add(Material mat, Mesh mesh, Matrix4x4 m)
        {
            List<CombineInstance> list;
            if (!groups.TryGetValue(mat, out list)) groups[mat] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mesh, transform = m });
        }

        public GameObject Build(string name, Transform parent, bool castShadows = true, bool receiveShadows = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            foreach (var kv in groups)
            {
                var mesh = new Mesh { name = name + "_" + kv.Key.name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                var go = new GameObject(kv.Key.name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = kv.Key;
                r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = receiveShadows;
            }
            groups.Clear();
            return root;
        }
    }
}
