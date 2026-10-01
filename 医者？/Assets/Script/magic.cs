using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MagicData
{
    public string magicName;
    public float manaCost;
    public MagicData(string name, float cost)
    {
        magicName = name;
        manaCost = cost;
    }
}

public class magic : MonoBehaviour
{
    public const string ComprehensiveExaminationName = "総合検査";

    private static readonly HashSet<string> IntegratedExaminationMagicNames = new()
    {
        "スキャン魔法",
        "視覚強化魔法",
        "超音波",
        "CT",
        "レントゲン",
        "MRI（雷魔法＝磁場）",
        "MRI魔力版",
        "神経系走査",
        "生体電気確認"
    };

    public InputButton buttonScript;
    public TextMeshProUGUI MPlabel;
    public GameObject Parent_scrollView;
    public GameObject ScrollPrefab;

    public List<Button> magicButtons = new List<Button>();

    [SerializeField]
    private float currentMP = 100f;

    public float CurrentMP => currentMP;

    public MagicData[] magicList = new MagicData[]
    {
        new MagicData("血液検査", 10f),
        new MagicData("スキャン魔法", 10f),
        new MagicData("毒性解析魔法", 10f),
        new MagicData("解毒", 10f),
        new MagicData("浄化", 10f),
        new MagicData("呪い解除", 10f),
        new MagicData("憑依解除", 10f),
        new MagicData("虫下し", 10f),
        new MagicData("組織修復", 10f),
        new MagicData("異物排出", 10f),
        new MagicData("魔力正常化", 10f),
        new MagicData("視覚強化魔法", 10f),
        new MagicData("超音波", 10f),
        new MagicData("CT", 10f),
        new MagicData("レントゲン", 10f),
        new MagicData("MRI（雷魔法＝磁場）", 10f),
        new MagicData("MRI魔力版", 10f),
        new MagicData("神経系走査", 10f),
        new MagicData("生体電気確認", 10f),
        new MagicData("分子解析", 10f),
        new MagicData("化学反応加減速", 10f),
        new MagicData("分子構造操作", 10f),
        new MagicData("瀉血", 10f),
        new MagicData("老眼鏡（モノクル？）とピンセットで異物排除", 10f)
    };

    private void Start()
    {
        ConsolidateExaminationMagic();

        foreach (var magic in magicList)
        {
            GameObject newItem = Instantiate(ScrollPrefab, Parent_scrollView.transform);
            TextMeshProUGUI textComponent = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = magic.magicName + " (MP: " + magic.manaCost + ")";
            }
            newItem.GetComponent<Button>().onClick.AddListener(() => OnMagicSelected(magic.magicName));
            magicButtons.Add(newItem.GetComponent<Button>());
        }

        RefreshMPDisplay();
        MPCheck();
    }

    private void ConsolidateExaminationMagic()
    {
        var consolidated = new List<MagicData>();
        bool examinationAdded = false;

        foreach (MagicData magicData in magicList)
        {
            if (magicData == null)
            {
                continue;
            }

            if (magicData.magicName == ComprehensiveExaminationName ||
                IntegratedExaminationMagicNames.Contains(magicData.magicName))
            {
                if (!examinationAdded)
                {
                    consolidated.Add(new MagicData(ComprehensiveExaminationName, 20f));
                    examinationAdded = true;
                }

                continue;
            }

            consolidated.Add(magicData);
        }

        if (!examinationAdded)
        {
            consolidated.Add(new MagicData(ComprehensiveExaminationName, 20f));
        }

        magicList = consolidated.ToArray();
    }

    private void MPCheck()
    {
        for(var i = 0; i < magicList.Length; i++)
        {
            if(currentMP < magicList[i].manaCost)
            {
                magicButtons[i].interactable = false;
            }
            else
            {
                magicButtons[i].interactable = true;
            }
        }
    }

    public void OnMagicSelected(string magicName)
    {
        buttonScript.inputfield.text = magicName;
    }

    public bool TryConsumeMagic(string input)
    {
        foreach (var magic in magicList)
        {
            if (magic.magicName != input)
            {
                continue;
            }

            return TrySpendMana(magic.manaCost);
        }

        // 魔法名ではない通常の入力はMP消費の対象外。
        return true;
    }

    public bool TrySpendMana(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (currentMP + 0.0001f < amount)
        {
            return false;
        }

        currentMP = Mathf.Max(0f, currentMP - amount);
        RefreshMPDisplay();
        MPCheck();
        return true;
    }

    private void RefreshMPDisplay()
    {
        if (MPlabel != null)
        {
            MPlabel.text = "MP: " + currentMP.ToString("0.0");
        }
    }

}
