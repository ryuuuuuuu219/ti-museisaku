using UnityEngine;

public enum ExaminationMpZeroPolicy
{
    Stable,
    Fatal
}

[CreateAssetMenu(
    fileName = "ExaminationStageVitals",
    menuName = "Medical Game/Examination Stage Vitals")]
public class ExaminationStageVitalsData : ScriptableObject
{
    [Tooltip("Communicate.stageと対応するステージ番号。")]
    public int stage;

    [Tooltip("MPが0になったときの患者状態。Stableはf(0)を維持し、Fatalは心拍0・苦痛度N/Aになる。")]
    public ExaminationMpZeroPolicy mpZeroPolicy = ExaminationMpZeroPolicy.Stable;

    [Tooltip("横軸は現在MP、縦軸は心拍数(bpm)。MPは消費により右から左へ進む。")]
    public AnimationCurve heartRateByMp = AnimationCurve.Linear(0f, 88f, 100f, 88f);

    [Tooltip("横軸は現在MP、縦軸は苦痛度(0～10)。")]
    public AnimationCurve painLevelByMp = AnimationCurve.Linear(0f, 5f, 100f, 5f);

    [Tooltip("横軸は現在MP、縦軸は苦痛度のばらつき(0～10)。苦痛波形の最大振幅になる。")]
    public AnimationCurve painVariationByMp = AnimationCurve.Linear(0f, 2f, 100f, 2f);

    public float EvaluateHeartRate(float mp)
    {
        return Mathf.Max(0f, heartRateByMp.Evaluate(mp));
    }

    public float EvaluatePainLevel(float mp)
    {
        return Mathf.Clamp(painLevelByMp.Evaluate(mp), 0f, 10f);
    }

    public float EvaluatePainVariation(float mp)
    {
        return Mathf.Clamp(painVariationByMp.Evaluate(mp), 0f, 10f);
    }
}
