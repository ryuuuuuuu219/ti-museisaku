using UnityEngine;

public sealed class StreetLightSpawner : MonoBehaviour
{
    [SerializeField] private GameObject streetLightPrefab;
    [SerializeField] private float spacingFromOrigin = 100f;
    [SerializeField] private float lightHeight = 15f;

    private static readonly Vector3[] AxisDirections =
    {
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back,
    };

    private void Awake()
    {
        if (streetLightPrefab == null)
        {
            Debug.LogWarning("StreetLightSpawner has no streetLightPrefab assigned.");
            return;
        }

        foreach (Vector3 direction in AxisDirections)
        {
            Vector3 position = direction * spacingFromOrigin;
            position.y = lightHeight;

            GameObject streetLight = Instantiate(streetLightPrefab, position, Quaternion.identity, transform);
            streetLight.name = $"Street Light {direction.x:0},{direction.z:0}";
        }
    }
}
