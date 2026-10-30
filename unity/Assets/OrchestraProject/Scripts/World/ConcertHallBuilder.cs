using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OrchestraProject
{
    /// <summary>
    /// Builds the concert hall procedurally: raked stalls, balcony, wooden acoustic shell,
    /// organ, ceiling clouds, stage lighting, reverb and post-processing.
    /// PLACEHOLDER GEOMETRY made of simple shapes — replace with a modelled hall in Stage 3.
    ///
    /// Coordinates: X = left/right, Y = up, Z = toward the back of the stage.
    /// Audience sits at negative Z looking toward +Z. Stage floor height = <see cref="StageHeight"/>.
    /// </summary>
    public class ConcertHallBuilder : MonoBehaviour
    {
        public const float StageHeight = 1.0f;
        public const float HallHalfWidth = 14f;
        public const float HallFrontZ = -32f;
        public const float HallBackZ = 13f;
        public const float HallHeight = 16f;
        public static readonly Vector3 ConductorPoint = new Vector3(0f, StageHeight, 0.9f);

        public bool MobileQuality;
        readonly System.Collections.Generic.List<Vector3> seatFloors = new System.Collections.Generic.List<Vector3>();

        public void Build()
        {
            MobileQuality = Application.isMobilePlatform;
            BuildStage();
            BuildShellAndOrgan();
            BuildRoom();
            BuildAudience();
            BuildLighting();
            BuildAtmosphere();
        }

        void BuildStage()
        {
            var b = new BatchBuilder();
            // Platform with dark front skirt
            b.Box(MaterialLibrary.DarkWood, new Vector3(0, StageHeight * 0.5f - 0.02f, 5.5f), new Vector3(22f, StageHeight - 0.04f, 14f));
            // Rounded apron front (approximated by stepped boxes)
            for (int i = 0; i < 6; i++)
            {
                float w = 18f - i * 2.4f;
                b.Box(MaterialLibrary.DarkWood, new Vector3(0, StageHeight * 0.5f - 0.02f, -1.6f - i * 0.25f), new Vector3(w, StageHeight - 0.04f, 0.3f));
            }
            // Floor boards
            b.Box(MaterialLibrary.StageFloor, new Vector3(0, StageHeight - 0.01f, 5.5f), new Vector3(22f, 0.02f, 14f));
            for (int i = 0; i < 6; i++)
            {
                float w = 18f - i * 2.4f;
                b.Box(MaterialLibrary.StageFloor, new Vector3(0, StageHeight - 0.01f, -1.6f - i * 0.25f), new Vector3(w, 0.02f, 0.3f));
            }
            // Gold edge trim
            b.Box(MaterialLibrary.Gold, new Vector3(0, StageHeight - 0.05f, -1.48f), new Vector3(19f, 0.04f, 0.03f));
            // Conductor's podium with rail
            b.Box(MaterialLibrary.DarkWood, new Vector3(ConductorPoint.x, StageHeight + 0.1f, ConductorPoint.z), new Vector3(0.9f, 0.2f, 0.9f));
            b.Box(MaterialLibrary.Velvet, new Vector3(ConductorPoint.x, StageHeight + 0.205f, ConductorPoint.z), new Vector3(0.84f, 0.01f, 0.84f));
            b.Cylinder(MaterialLibrary.Gold, new Vector3(ConductorPoint.x, StageHeight + 0.65f, ConductorPoint.z - 0.42f), 0.03f, 0.9f);
            b.Box(MaterialLibrary.Gold, new Vector3(ConductorPoint.x, StageHeight + 1.1f, ConductorPoint.z - 0.42f), new Vector3(0.8f, 0.03f, 0.03f));
            b.Build("Stage", transform);
        }

        void BuildShellAndOrgan()
        {
            var b = new BatchBuilder();
            // Curved acoustic shell of wooden panels behind the orchestra
            Vector3 c = new Vector3(0, 0, 3.5f);
            int panels = 13;
            float radius = 9.5f;
            for (int i = 0; i < panels; i++)
            {
                float a = Mathf.Lerp(-72f, 72f, i / (float)(panels - 1)) * Mathf.Deg2Rad;
                Vector3 p = c + new Vector3(Mathf.Sin(a) * radius, StageHeight + 3.6f, Mathf.Cos(a) * radius);
                var rot = Quaternion.LookRotation(new Vector3(c.x - p.x, 0, c.z - p.z));
                var mat = i % 2 == 0 ? MaterialLibrary.WarmWood : MaterialLibrary.DarkWood;
                b.Box(mat, p, new Vector3(2.35f, 7.2f, 0.18f), rot);
                // Diffuser slats
                for (int s = 0; s < 4; s++)
                    b.Box(MaterialLibrary.DarkWood, p + rot * new Vector3(0, -2.6f + s * 1.7f, -0.12f), new Vector3(2.2f, 0.12f, 0.08f), rot);
            }
            // Organ case and pipes above the shell
            float oz = 12.4f;
            b.Box(MaterialLibrary.DarkWood, new Vector3(0, 10.5f, oz + 0.3f), new Vector3(12f, 7f, 0.6f));
            b.Box(MaterialLibrary.Gold, new Vector3(0, 7.1f, oz - 0.05f), new Vector3(12.2f, 0.15f, 0.3f));
            for (int i = 0; i < 31; i++)
            {
                float x = -5.6f + i * (11.2f / 30f);
                float t = Mathf.Abs(x) / 5.6f;
                float h = Mathf.Lerp(6.2f, 2.8f, t) + ((i % 2) * 0.4f);
                float d = Mathf.Lerp(0.3f, 0.16f, t);
                b.Cylinder(MaterialLibrary.OrganPipe, new Vector3(x, 7.3f + h * 0.5f, oz - 0.15f), d, h);
            }
            b.Build("Acoustic Shell & Organ", transform);
        }

        void BuildRoom()
        {
            var b = new BatchBuilder();
            float len = HallBackZ - HallFrontZ;
            float midZ = (HallBackZ + HallFrontZ) * 0.5f;
            // Side walls with pilasters and lamps
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * HallHalfWidth;
                b.Box(MaterialLibrary.Plaster, new Vector3(x + side * 0.2f, HallHeight * 0.5f, midZ), new Vector3(0.4f, HallHeight, len));
                b.Box(MaterialLibrary.DarkWood, new Vector3(x - side * 0.05f, 1.2f, midZ), new Vector3(0.12f, 2.4f, len));   // wainscot
                for (float z = HallFrontZ + 3f; z < HallBackZ - 1f; z += 4.5f)
                {
                    b.Box(MaterialLibrary.PlasterShade, new Vector3(x - side * 0.25f, HallHeight * 0.5f, z), new Vector3(0.5f, HallHeight, 0.8f));
                    b.Box(MaterialLibrary.Gold, new Vector3(x - side * 0.52f, 4.6f, z), new Vector3(0.08f, 0.5f, 0.25f));
                    b.Sphere(MaterialLibrary.WarmLamp, new Vector3(x - side * 0.6f, 5.0f, z), new Vector3(0.28f, 0.36f, 0.28f));
                }
            }
            // Back wall, ceiling with coffers, stage-side ceiling clouds
            b.Box(MaterialLibrary.Plaster, new Vector3(0, HallHeight * 0.5f, HallFrontZ - 0.2f), new Vector3(HallHalfWidth * 2f, HallHeight, 0.4f));
            b.Box(MaterialLibrary.Plaster, new Vector3(0, HallHeight * 0.5f, HallBackZ + 0.2f), new Vector3(HallHalfWidth * 2f, HallHeight, 0.4f));
            b.Box(MaterialLibrary.Ceiling, new Vector3(0, HallHeight + 0.2f, midZ), new Vector3(HallHalfWidth * 2f, 0.4f, len));
            for (float z = HallFrontZ + 2.5f; z < HallBackZ; z += 3.5f)
                b.Box(MaterialLibrary.DarkWood, new Vector3(0, HallHeight - 0.25f, z), new Vector3(HallHalfWidth * 2f, 0.5f, 0.35f));
            for (float x = -HallHalfWidth + 3.5f; x < HallHalfWidth; x += 3.5f)
                b.Box(MaterialLibrary.DarkWood, new Vector3(x, HallHeight - 0.25f, midZ), new Vector3(0.35f, 0.5f, len));
            for (int i = 0; i < 3; i++)
                b.Box(MaterialLibrary.WarmWood, new Vector3(0, 10.5f + i * 0.4f, 1.5f + i * 3.2f), new Vector3(12f - i * 1.5f, 0.15f, 2.6f),
                    Quaternion.Euler(-12f + i * 4f, 0, 0));
            // Audience floor and aisles
            b.Box(MaterialLibrary.Carpet, new Vector3(0, -0.05f, (HallFrontZ + 0f) * 0.5f), new Vector3(HallHalfWidth * 2f, 0.1f, -HallFrontZ));
            b.Build("Room", transform, castShadows: false);
        }

        void BuildAudience()
        {
            var b = new BatchBuilder();
            int rows = MobileQuality ? 16 : 22;
            float rowDepth = 0.95f, rise = 0.14f, startZ = -4.2f;
            float seatW = 0.56f;
            float[] blockStart = { -12.6f, -4.2f, 4.6f };
            int[] blockSeats = { 14, 15, 14 };

            for (int r = 0; r < rows; r++)
            {
                float z = startZ - r * rowDepth;
                float y = r * rise;
                // Stepped floor
                b.Box(MaterialLibrary.Carpet, new Vector3(0, y * 0.5f, z - rowDepth * 0.5f + 0.475f), new Vector3(HallHalfWidth * 2f, y + 0.01f, rowDepth));
                for (int blk = 0; blk < 3; blk++)
                    for (int s = 0; s < blockSeats[blk]; s++)
                    {
                        float x = blockStart[blk] + s * seatW;
                        AddSeat(b, new Vector3(x, y, z)); seatFloors.Add(new Vector3(x, y, z));
                    }
            }

            // Balcony across the back
            float bz = HallFrontZ + 8f, by = 6.2f;
            b.Box(MaterialLibrary.DarkWood, new Vector3(0, by - 0.25f, (bz + HallFrontZ) * 0.5f), new Vector3(HallHalfWidth * 2f, 0.5f, bz - HallFrontZ));
            b.Box(MaterialLibrary.Plaster, new Vector3(0, by + 0.5f, bz), new Vector3(HallHalfWidth * 2f, 1.1f, 0.25f));
            b.Box(MaterialLibrary.Gold, new Vector3(0, by + 1.07f, bz - 0.02f), new Vector3(HallHalfWidth * 2f, 0.06f, 0.3f));
            b.Box(MaterialLibrary.Gold, new Vector3(0, by - 0.05f, bz - 0.02f), new Vector3(HallHalfWidth * 2f, 0.08f, 0.3f));
            for (float x = -10.5f; x <= 10.5f; x += 7f)
                b.Cylinder(MaterialLibrary.Plaster, new Vector3(x, by * 0.5f, bz + 0.6f), 0.5f, by);
            for (int r = 0; r < 5; r++)
                for (int s = 0; s < 44; s++)
                {
                    var seat = new Vector3(-12.2f + s * seatW, by + r * 0.35f, bz - 1.0f - r * 1.0f);
                    AddSeat(b, seat); seatFloors.Add(seat);
                }

            b.Build("Audience Seating", transform, castShadows: !MobileQuality);
            new GameObject("Audience").AddComponent<AudienceCrowd>().Build(seatFloors);
        }

        static void AddSeat(BatchBuilder b, Vector3 floor)
        {
            b.Box(MaterialLibrary.Velvet, floor + new Vector3(0, 0.44f, 0.02f), new Vector3(0.5f, 0.1f, 0.46f));
            b.Box(MaterialLibrary.Velvet, floor + new Vector3(0, 0.78f, -0.24f), new Vector3(0.5f, 0.62f, 0.09f), Quaternion.Euler(-8f, 0, 0));
            b.Box(MaterialLibrary.DarkWood, floor + new Vector3(0.27f, 0.6f, -0.02f), new Vector3(0.05f, 0.05f, 0.42f));
            b.Box(MaterialLibrary.SteelBlack, floor + new Vector3(0, 0.2f, -0.05f), new Vector3(0.42f, 0.4f, 0.06f));
        }

        void BuildLighting()
        {
            // Key light from above the audience, warm, with soft shadows.
            var key = new GameObject("Key Light").AddComponent<Light>();
            key.transform.SetParent(transform, false);
            key.type = LightType.Directional;
            key.transform.rotation = Quaternion.Euler(52f, 12f, 0f);
            key.color = new Color(1f, 0.93f, 0.84f);
            key.intensity = 0.55f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.75f;

            // Cool rim light from behind the orchestra: separates the dark tailcoats from the dark shell.
            var rim = new GameObject("Rim Light").AddComponent<Light>();
            rim.transform.SetParent(transform, false);
            rim.type = LightType.Directional;
            rim.transform.position = new Vector3(0, 4f, 14f);   // low angle: lights backs and shoulders, barely the floor
            rim.transform.LookAt(new Vector3(0, 1.2f, 2f));
            rim.color = new Color(0.77f, 0.82f, 1f);
            rim.intensity = 0.45f;
            rim.shadows = LightShadows.None;

            // Stage wash spotlights hung in front of the stage.
            var spots = new System.Collections.Generic.List<Light>(); var houses = new System.Collections.Generic.List<Light>();
            float[] xs = MobileQuality ? new[] { -4f, 0f, 4f } : new[] { -6f, -3f, 0f, 3f, 6f };
            foreach (var x in xs)
            {
                var spot = new GameObject("Stage Spot").AddComponent<Light>();
                spot.transform.SetParent(transform, false);
                spot.type = LightType.Spot;
                spot.transform.position = new Vector3(x, 11.5f, -5f);
                spot.transform.LookAt(new Vector3(x * 0.55f, StageHeight, 3.2f));
                spot.spotAngle = 42f;
                spot.innerSpotAngle = 20f;
                spot.range = 30f;
                spot.intensity = MobileQuality ? 160f : 110f;
                spot.color = new Color(1f, 0.9f, 0.76f);
                spot.shadows = (!MobileQuality && Mathf.Approximately(x, 0f)) ? LightShadows.Soft : LightShadows.None;
                spots.Add(spot);
            }

            // Dim house lights over the audience.
            for (int i = 0; i < 2; i++)
            {
                var house = new GameObject("House Light").AddComponent<Light>();
                house.transform.SetParent(transform, false);
                house.type = LightType.Point;
                house.transform.position = new Vector3(0, 12f, -10f - i * 12f);
                house.range = 22f;
                house.intensity = 14f;
                house.color = new Color(1f, 0.82f, 0.6f);
                house.shadows = LightShadows.None;
                houses.Add(house);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.21f, 0.18f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.12f, 0.10f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.045f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.09f, 0.075f, 0.06f);
            RenderSettings.fogDensity = 0.012f;
            gameObject.AddComponent<HouseLights>().Init(key, spots, houses);

            // Reflections for the varnish, brass and floor.
            var probe = new GameObject("Reflection Probe").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(transform, false);
            probe.transform.position = new Vector3(0, 4f, 2f);
            probe.size = new Vector3(HallHalfWidth * 2f, HallHeight, HallBackZ - HallFrontZ);
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.resolution = MobileQuality ? 64 : 256;
            probe.RenderProbe();

#if !UNITY_EDITOR
            // Builds only: in the Editor this would permanently change the project's URP asset.
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null) pipeline.shadowDistance = 45f;
#endif
        }

        void BuildAtmosphere()
        {
            // Hall acoustics: Unity's built-in concert hall reverb applies to every AudioSource.
            var reverb = new GameObject("Hall Reverb").AddComponent<AudioReverbZone>();
            reverb.transform.SetParent(transform, false);
            reverb.transform.position = new Vector3(0, 4f, -8f);
            reverb.reverbPreset = AudioReverbPreset.Concerthall;
            reverb.roomLF = -1200;   // keep the low end out of the hall reverb: cleaner cellos and basses (switches the zone to a User preset based on Concerthall)
            reverb.minDistance = 40f;
            reverb.maxDistance = 70f;

            // Cinematic grading: ACES tone mapping, gentle bloom on lamps and brass, vignette.
            var volume = new GameObject("Post Processing").AddComponent<Volume>();
            volume.transform.SetParent(transform, false);
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.6f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.35f);
            color.contrast.Override(12f);
            color.saturation.Override(-4f);
            volume.sharedProfile = profile;
        }
    }
}
