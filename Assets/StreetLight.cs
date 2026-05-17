using UnityEngine;

public sealed class StreetLight : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private float lightRange = 45f;
    [SerializeField] private float lightIntensity = 8f;
    [SerializeField] private float spotAngle = 48f;

    [Header("Pole")]
    [SerializeField] private float poleHeight = 15f;
    [SerializeField] private float poleRadius = 0.18f;

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
    }
}
