using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraNoiseOverlayController : MonoBehaviour
{
    [Header("Base Noise")]
    [SerializeField, Range(0f, 1f)] private float noiseLevel = 0.065f;
    [SerializeField] private float noisePlaneDistance = 0.35f;
    [SerializeField] private float noiseScale = 160f;
    [SerializeField] private float noiseFlickerSpeed = 22f;

    [Header("Block Noise")]
    [SerializeField, Range(0f, 1f)] private float blockNoiseLevel = 0.055f;
    [SerializeField] private Vector2 blockNoiseRegionSize = new Vector2(0.24f, 0.12f);
    [SerializeField] private Vector2 blockNoiseGrid = new Vector2(8f, 5f);
    [SerializeField] private float blockNoiseInterval = 0.12f;

    [Header("RGB Glitch")]
    [SerializeField, Range(0f, 1f)] private float rgbGlitchLevel = 0.018f;
    [SerializeField] private float rgbGlitchOffset = 0.012f;

    private Camera targetCamera;
    private Material noiseMaterial;
    private Transform noisePlane;
    private float nextBlockJumpTime;
    private Vector2 blockNoiseCenter = new Vector2(0.5f, 0.5f);

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        BuildNoisePlane();
        JumpBlockRegion();
    }

    private void Update()
    {
        if (Time.time >= nextBlockJumpTime)
        {
            JumpBlockRegion();
        }

        UpdateNoisePlane();
    }

    private void BuildNoisePlane()
    {
        Shader noiseShader = Shader.Find("Horror/CameraNoiseOverlay");
        if (noiseShader == null)
        {
            Debug.LogWarning("Horror/CameraNoiseOverlay shader was not found.");
            return;
        }

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "Camera Noise Overlay";
        plane.transform.SetParent(transform, false);
        noisePlane = plane.transform;

        Collider planeCollider = plane.GetComponent<Collider>();
        if (planeCollider != null)
        {
            Destroy(planeCollider);
        }

        noiseMaterial = new Material(noiseShader);
        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = noiseMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void JumpBlockRegion()
    {
        float halfWidth = Mathf.Clamp01(blockNoiseRegionSize.x) * 0.5f;
        float halfHeight = Mathf.Clamp01(blockNoiseRegionSize.y) * 0.5f;
        blockNoiseCenter = new Vector2(
            Random.Range(halfWidth, 1f - halfWidth),
            Random.Range(halfHeight, 1f - halfHeight));
        nextBlockJumpTime = Time.time + Mathf.Max(0.02f, blockNoiseInterval);
    }

    private void UpdateNoisePlane()
    {
        if (noisePlane == null || noiseMaterial == null || targetCamera == null)
        {
            return;
        }

        float distance = Mathf.Max(targetCamera.nearClipPlane + 0.01f, noisePlaneDistance);
        float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * targetCamera.aspect;

        noisePlane.localPosition = new Vector3(0f, 0f, distance);
        noisePlane.localRotation = Quaternion.identity;
        noisePlane.localScale = new Vector3(width, height, 1f);

        noiseMaterial.SetFloat("_NoiseLevel", noiseLevel);
        noiseMaterial.SetFloat("_NoiseScale", noiseScale);
        noiseMaterial.SetFloat("_FlickerSpeed", noiseFlickerSpeed);
        noiseMaterial.SetFloat("_BlockNoiseLevel", blockNoiseLevel);
        noiseMaterial.SetVector("_BlockNoiseCenter", blockNoiseCenter);
        noiseMaterial.SetVector("_BlockNoiseRegionSize", blockNoiseRegionSize);
        noiseMaterial.SetVector("_BlockNoiseGrid", blockNoiseGrid);
        noiseMaterial.SetFloat("_RgbGlitchLevel", rgbGlitchLevel);
        noiseMaterial.SetFloat("_RgbGlitchOffset", rgbGlitchOffset);
    }
}
