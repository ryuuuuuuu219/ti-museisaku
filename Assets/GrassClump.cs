using UnityEngine;

public sealed class GrassClump : MonoBehaviour
{
    [SerializeField, Min(1)] private int bladeCount = 14;
    [SerializeField] private float radius = 0.45f;
    [SerializeField] private Vector2 heightRange = new Vector2(0.35f, 0.85f);
    [SerializeField] private Vector2 widthRange = new Vector2(0.025f, 0.055f);
    [SerializeField] private Color baseColor = new Color(0.12f, 0.38f, 0.12f);
    [SerializeField] private Color tipColor = new Color(0.35f, 0.62f, 0.22f);

    private void Awake()
    {
        if (transform.childCount > 0)
        {
            return;
        }

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = Color.Lerp(baseColor, tipColor, 0.45f);

        for (int i = 0; i < bladeCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0f, radius);
            float height = Random.Range(heightRange.x, heightRange.y);
            float width = Random.Range(widthRange.x, widthRange.y);

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Grass Blade";
            blade.transform.SetParent(transform, false);
            blade.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance, height * 0.5f, Mathf.Sin(angle) * distance);
            blade.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
            blade.transform.localScale = new Vector3(width, height, width);

            Renderer renderer = blade.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            Collider collider = blade.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
