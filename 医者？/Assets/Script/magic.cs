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
        new MagicData("Fireball", 20f),
        new MagicData("Ice Shard", 15f),
        new MagicData("Lightning Bolt", 25f),
        new MagicData("Heal", 10f),
        new MagicData("Wind Gust", 5f)
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
