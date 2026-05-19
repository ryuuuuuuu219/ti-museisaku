using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class LightConeTrigger : MonoBehaviour
{
    [SerializeField, Min(8)] private int segments = 32;

    private Light sourceLight;
    private Mesh coneMesh;
    private MeshCollider meshCollider;
    private Rigidbody triggerRigidbody;
    private float lastRange;
    private float lastSpotAngle;
    private int lastSegments;

    public void Configure(Light lightToFollow)
    {
        sourceLight = lightToFollow;
        EnsureCollider();
        RebuildIfNeeded(true);
    }

    private void Awake()
    {
        if (sourceLight == null)
        {
            sourceLight = GetComponent<Light>();
        }

        EnsureCollider();
    }

    private void LateUpdate()
    {
        RebuildIfNeeded(false);
    }

    private void EnsureCollider()
    {
        if (meshCollider != null)
        {
            return;
        }

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        meshCollider.convex = true;
        meshCollider.isTrigger = true;

        triggerRigidbody = GetComponent<Rigidbody>();
        if (triggerRigidbody == null)
        {
            triggerRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        triggerRigidbody.isKinematic = true;
        triggerRigidbody.useGravity = false;
    }

    private void RebuildIfNeeded(bool force)
    {
        if (sourceLight == null || sourceLight.type != LightType.Spot)
        {
            return;
        }

        int safeSegments = Mathf.Max(8, segments);
        if (!force
            && Mathf.Approximately(lastRange, sourceLight.range)
            && Mathf.Approximately(lastSpotAngle, sourceLight.spotAngle)
            && lastSegments == safeSegments)
        {
            return;
        }

        lastRange = sourceLight.range;
        lastSpotAngle = sourceLight.spotAngle;
        lastSegments = safeSegments;
        BuildConeMesh(sourceLight.range, sourceLight.spotAngle, safeSegments);
    }

    private void BuildConeMesh(float range, float spotAngle, int safeSegments)
    {
        if (coneMesh == null)
        {
            coneMesh = new Mesh();
            coneMesh.name = "Light Cone Trigger Mesh";
        }
        else
        {
            coneMesh.Clear();
        }

        float radius = Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * range;
        Vector3[] vertices = new Vector3[safeSegments + 2];
        int[] triangles = new int[safeSegments * 6];

        vertices[0] = Vector3.zero;
        vertices[1] = Vector3.forward * range;

        for (int i = 0; i < safeSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / safeSegments;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, range);
        }

        int triangleIndex = 0;
        for (int i = 0; i < safeSegments; i++)
        {
            int current = i + 2;
            int next = ((i + 1) % safeSegments) + 2;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;

            triangles[triangleIndex++] = 1;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();
        meshCollider.sharedMesh = coneMesh;
    }
}
