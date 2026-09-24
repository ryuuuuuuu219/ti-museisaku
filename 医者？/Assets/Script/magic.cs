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
    public InputButton buttonScript;
    public TextMeshProUGUI MPlabel;
    public GameObject Parent_scrollView;
    public GameObject ScrollPrefab;

    public List<Button> magicButtons = new List<Button>();

    float currentMP = 100f;

    public MagicData[] magicList = new MagicData[]
    {
        new MagicData("血液検査", 10f),
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
        new MagicData("浄化", 10f),
        new MagicData("瀉血", 10f),
        new MagicData("老眼鏡（モノクル？）とピンセットで異物排除", 10f)
    };

    private void Start()
    {
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

            if (currentMP < magic.manaCost)
            {
                return false;
            }

            currentMP -= magic.manaCost;
            MPlabel.text = "MP: " + currentMP.ToString("F0");
            MPCheck();
            return true;
        }

        // 魔法名ではない通常の入力はMP消費の対象外。
        return true;
    }

}
