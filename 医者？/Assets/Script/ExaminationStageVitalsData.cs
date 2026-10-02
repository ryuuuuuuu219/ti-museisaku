using UnityEngine;

[System.Serializable]
public sealed class ExaminationLesionDefinition
{
    [Tooltip("ステージ内で重複しない患部ID。発見状態の保存にも使用する。")]
    public string id;

    [TextArea]
    [Tooltip("患部を発見したときに表示する観測事実。")]
    public string findingText;

    [Tooltip("この患部を観測できる検査魔法。")]
    public ExaminationMagicMode requiredMagic;

    [Tooltip("この患部を観測できるレイヤー名。")]
    public string requiredLayer;

    [Tooltip("患者模型上の患部中心。ワールド座標のXY。")]
    public Vector2 position;

    [Min(0.01f)]
    [Tooltip("患部の表示半径と接触判定半径。")]
    public float radius = 0.3f;

    [Min(0.01f)]
    [Tooltip("患部を発見するまでに必要な累積観察時間（秒）。")]
    public float requiredContactSeconds = 2.5f;

    [Range(0f, 1f)]
    [Tooltip("患部発見後の赤線の最大不透明度。")]
    public float maxAlpha = 0.82f;
}

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

    [Tooltip("このステージで検査できる患部の一覧。配列順に生成する。")]
    public ExaminationLesionDefinition[] lesions = System.Array.Empty<ExaminationLesionDefinition>();

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
