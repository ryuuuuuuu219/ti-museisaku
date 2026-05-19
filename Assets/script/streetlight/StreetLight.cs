using UnityEngine;

public sealed class StreetLight : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private float lightRange = 45f;
    [SerializeField] private float lightIntensity = 8f;
    [SerializeField] private float spotAngle = 48f;
    [SerializeField, Range(0.01f, 0.5f)] private float coneOpacity = 0.16f;
    [SerializeField, Min(8)] private int coneSegments = 48;

    [Header("Pole")]
    [SerializeField] private float poleHeight = 15f;
    [SerializeField] private float poleRadius = 0.18f;

    private const string VisibleConeName = "Visible Light Cone";

    private void Awake()
    {
        BuildPoleIfNeeded();
        BuildLampIfNeeded();
    }

    private void BuildPoleIfNeeded()
    {
        if (transform.Find("Pole") != null)
        {
            return;
        }

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(transform, false);
        pole.transform.localPosition = new Vector3(0f, -poleHeight * 0.5f, 0f);
        pole.transform.localScale = new Vector3(poleRadius, poleHeight * 0.5f, poleRadius);

        Renderer renderer = pole.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(0.08f, 0.08f, 0.075f);
    }

    private void BuildLampIfNeeded()
    {
        Transform lampTransform = transform.Find("Lamp");
        if (lampTransform == null)
        {
            GameObject lamp = new GameObject("Lamp");
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = Vector3.zero;
            lamp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lampTransform = lamp.transform;
        }

        Light spotLight = lampTransform.GetComponent<Light>();
        if (spotLight == null)
        {
            spotLight = lampTransform.gameObject.AddComponent<Light>();
        }

        spotLight.type = LightType.Spot;
        spotLight.range = lightRange;
        spotLight.intensity = lightIntensity;
        spotLight.spotAngle = spotAngle;
        spotLight.innerSpotAngle = spotAngle * 0.55f;
        spotLight.shadows = LightShadows.Soft;
        spotLight.color = new Color(1f, 0.9f, 0.68f);

        LightConeTrigger coneTrigger = lampTransform.GetComponent<LightConeTrigger>();
        if (coneTrigger == null)
        {
            coneTrigger = lampTransform.gameObject.AddComponent<LightConeTrigger>();
        }

        coneTrigger.Configure(spotLight);

        BuildVisibleCone(lampTransform, spotLight);
    }

    private void BuildVisibleCone(Transform lampTransform, Light spotLight)
    {
        Transform coneTransform = lampTransform.Find(VisibleConeName);
        if (coneTransform == null)
        {
            GameObject cone = new GameObject(VisibleConeName);
            cone.transform.SetParent(lampTransform, false);
            coneTransform = cone.transform;
        }

        MeshFilter meshFilter = coneTransform.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = coneTransform.gameObject.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = coneTransform.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = coneTransform.gameObject.AddComponent<MeshRenderer>();
        }

        meshFilter.sharedMesh = CreateVisibleConeMesh(spotLight.range, spotLight.spotAngle, coneSegments);
        meshRenderer.sharedMaterial = CreateConeMaterial(spotLight.color);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private Mesh CreateVisibleConeMesh(float range, float angle, int segments)
    {
        int safeSegments = Mathf.Max(8, segments);
        float radius = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * range;
        Vector3[] vertices = new Vector3[safeSegments + 1];
        int[] triangles = new int[safeSegments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < safeSegments; i++)
        {
            float circleAngle = (Mathf.PI * 2f * i) / safeSegments;
            vertices[i + 1] = new Vector3(Mathf.Cos(circleAngle) * radius, Mathf.Sin(circleAngle) * radius, range);
        }

        int triangleIndex = 0;
        for (int i = 0; i < safeSegments; i++)
        {
            int current = i + 1;
            int next = ((i + 1) % safeSegments) + 1;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Visible Light Cone Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateConeMaterial(Color lightColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = "Visible Light Cone Material";
        Color coneColor = new Color(lightColor.r, lightColor.g, lightColor.b, coneOpacity);
        material.color = coneColor;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", coneColor);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }
}
