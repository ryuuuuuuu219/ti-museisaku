using System.Collections;
using UnityEngine;

public sealed class Phase1EnvironmentController : MonoBehaviour
{
    const string SkyShaderName = "Phase1/StarrySkyTransition";
    static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    static readonly int TransitionOriginId = Shader.PropertyToID("_TransitionOriginWS");
    static readonly int TransitionExpansionRateId = Shader.PropertyToID("_TransitionExpansionRate");

    [SerializeField] Light moonLight;
    [SerializeField] [Range(0f, 2f)] float moonIntensity = 0.6f;
    [SerializeField] [Range(0f, 1f)] float initialSkyFillAmount = 0f;
    [SerializeField] float transitionSeconds = 2.5f;
    public Vector3 moonPosition = new Vector3(0f, 120f, 140f);
    [Range(0.1f, 100f)] public float transitionExpansionRate = 1.6f;
    [SerializeField] float skyRadius = 900f;
    [SerializeField] int skyGridRadius = 28;

    Material skyMaterial;
    Transform skyDome;
    Transform skyBackfillDome;
    Coroutine transitionRoutine;

    void Awake()
    {
        ConfigureMoonLight();
        ConfigureSkyDome();
    }

    void LateUpdate()
    {
        ApplyMoonSettings();
        ApplyTransitionSettings();
    }

    public void SetSkyFillAmount(float amount)
    {
        initialSkyFillAmount = Mathf.Clamp01(amount);

        if (skyMaterial != null)
        {
            skyMaterial.SetFloat(FillAmountId, initialSkyFillAmount);
            ApplyTransitionSettings();
        }
    }

    public void PlayPhase2Transition()
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionSkyFill(initialSkyFillAmount, 1f));
    }

    [ContextMenu("Reset Phase 1 Sky")]
    public void ResetPhase1Sky()
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        SetSkyFillAmount(0f);
    }

    void ConfigureMoonLight()
    {
        if (moonLight == null)
            moonLight = RenderSettings.sun != null ? RenderSettings.sun : FindAnyObjectByType<Light>();

        if (moonLight == null)
            return;

        ApplyMoonSettings();
    }

    void ConfigureSkyDome()
    {
        Shader skyShader = Shader.Find(SkyShaderName);
        if (skyShader == null)
        {
            Debug.LogWarning($"{SkyShaderName} shader was not found.");
            return;
        }

        skyMaterial = new Material(skyShader)
        {
            name = "Phase1 Starry Sky Runtime"
        };
        RenderSettings.skybox = null;

        GameObject dome = new GameObject("Phase1 Starry Sky Hemisphere");
        dome.transform.position = Vector3.zero;
        dome.transform.rotation = Quaternion.identity;
        dome.transform.localScale = Vector3.one;
        skyDome = dome.transform;

        MeshFilter meshFilter = dome.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = dome.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = CreatePentagonHexagonHemisphere();
        meshRenderer.sharedMaterial = skyMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        GameObject backfillDome = new GameObject("Phase1 Starry Sky Backfill");
        backfillDome.transform.position = Vector3.zero;
        backfillDome.transform.rotation = Quaternion.identity;
        backfillDome.transform.localScale = Vector3.one;
        skyBackfillDome = backfillDome.transform;

        MeshFilter backfillMeshFilter = backfillDome.AddComponent<MeshFilter>();
        MeshRenderer backfillMeshRenderer = backfillDome.AddComponent<MeshRenderer>();
        backfillMeshFilter.sharedMesh = CreateBackfillHemisphere();
        backfillMeshRenderer.sharedMaterial = skyMaterial;
        backfillMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backfillMeshRenderer.receiveShadows = false;

        SetSkyFillAmount(initialSkyFillAmount);
    }

    void ApplyMoonSettings()
    {
        if (moonLight == null)
            return;

        moonLight.type = LightType.Directional;
        moonLight.intensity = moonIntensity;
        moonLight.transform.position = moonPosition;
        moonLight.transform.rotation = Quaternion.LookRotation(-moonPosition.normalized, Vector3.up);
        RenderSettings.sun = moonLight;
    }

    void ApplyTransitionSettings()
    {
        if (skyMaterial == null)
            return;

        Vector3 origin = moonPosition.sqrMagnitude > 0.0001f ? moonPosition : Vector3.up;
        skyMaterial.SetVector(TransitionOriginId, origin.normalized);
        skyMaterial.SetFloat(TransitionExpansionRateId, Mathf.Clamp(transitionExpansionRate, 0.1f, 100f));
    }

    Mesh CreatePentagonHexagonHemisphere()
    {
        int gridRadius = Mathf.Max(4, skyGridRadius);
        float radius = Mathf.Max(100f, skyRadius);
        float cellSize = 1f / gridRadius;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        AddSkyCell(Vector2.zero, 5, cellSize * 0.82f, radius, vertices, triangles);

        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
            int r2 = Mathf.Min(gridRadius, -q + gridRadius);

            for (int r = r1; r <= r2; r++)
            {
                if (q == 0 && r == 0)
                    continue;

                Vector2 center = AxialToDisk(q, r, cellSize);
                if (center.sqrMagnitude > 0.98f)
                    continue;

                int sides = center.magnitude > 0.86f ? 5 : 6;
                AddSkyCell(center, sides, cellSize * 0.62f, radius, vertices, triangles);
            }
        }

        Mesh mesh = new Mesh
        {
            name = "Phase1 Pentagon Hexagon Hemisphere"
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh CreateBackfillHemisphere()
    {
        const int longitudeSegments = 96;
        const int latitudeSegments = 24;
        float radius = Mathf.Max(100f, skyRadius) * 1.01f;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int y = 0; y <= latitudeSegments; y++)
        {
            float v = y / (float)latitudeSegments;
            float polar = v * Mathf.PI * 0.5f;
            float ringRadius = Mathf.Sin(polar);
            float height = Mathf.Cos(polar);

            for (int x = 0; x <= longitudeSegments; x++)
            {
                float u = x / (float)longitudeSegments;
                float angle = u * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * ringRadius, height, Mathf.Sin(angle) * ringRadius) * radius);
            }
        }

        int rowSize = longitudeSegments + 1;
        for (int y = 0; y < latitudeSegments; y++)
        {
            for (int x = 0; x < longitudeSegments; x++)
            {
                int a = y * rowSize + x;
                int b = a + 1;
                int c = a + rowSize;
                int d = c + 1;

                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(b);
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        Mesh mesh = new Mesh
        {
            name = "Phase1 Continuous Backfill Hemisphere"
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    Vector2 AxialToDisk(int q, int r, float cellSize)
    {
        float x = cellSize * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float y = cellSize * 1.5f * r;
        return new Vector2(x, y);
    }

    void AddSkyCell(
        Vector2 center,
        int sides,
        float cellRadius,
        float domeRadius,
        System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<int> triangles)
    {
        int start = vertices.Count;
        vertices.Add(ProjectToHemisphere(center, domeRadius));

        float rotation = sides == 5 ? Mathf.PI * 0.5f : Mathf.PI / 6f;
        for (int i = 0; i < sides; i++)
        {
            float angle = rotation + Mathf.PI * 2f * i / sides;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cellRadius;
            vertices.Add(ProjectToHemisphere(point, domeRadius));
        }

        for (int i = 0; i < sides; i++)
        {
            int next = i == sides - 1 ? 1 : i + 2;
            triangles.Add(start);
            triangles.Add(start + next);
            triangles.Add(start + i + 1);
        }
    }

    Vector3 ProjectToHemisphere(Vector2 diskPoint, float radius)
    {
        Vector2 clamped = Vector2.ClampMagnitude(diskPoint, 0.995f);
        float y = Mathf.Sqrt(Mathf.Max(0f, 1f - clamped.sqrMagnitude));
        return new Vector3(clamped.x, y, clamped.y) * radius;
    }

    IEnumerator TransitionSkyFill(float from, float to)
    {
        float duration = Mathf.Max(0.01f, transitionSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetSkyFillAmount(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetSkyFillAmount(to);
        transitionRoutine = null;
    }
}
