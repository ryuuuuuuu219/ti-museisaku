using UnityEngine;

public class RingBufferLineGraph : MonoBehaviour
{
    public const int SampleCount = 80;

    [SerializeField]
    private float[] samples = new float[SampleCount];

    [SerializeField]
    private int initialAddress;

    private int address;
    private LineRenderer lineRenderer;

    public float[] Samples => samples;
    public int InitialAddress => initialAddress;

    public void Configure(LineRenderer targetLineRenderer, float initialValue)
    {
        lineRenderer = targetLineRenderer;

        if (samples == null || samples.Length != SampleCount)
        {
            samples = new float[SampleCount];
        }

        for (int i = 0; i < SampleCount; i++)
        {
            samples[i] = initialValue;
        }

        initialAddress = 0;
        Redraw();
    }

    public void AddSample(float value)
    {
        samples[initialAddress] = value;
        initialAddress = (initialAddress + 1) % SampleCount;
        Redraw();
    }

    public void Redraw()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = SampleCount;

        for (int i = 0; i < SampleCount; i++)
        {
            address = (initialAddress + i) % SampleCount;
            lineRenderer.SetPosition(i, new Vector3(100f + i * 5f, samples[address], 0f));
        }
    }
}
