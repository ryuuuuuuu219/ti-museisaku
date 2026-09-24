using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeywordGroupDisplay : MonoBehaviour
{
    public info infoScript;
    public Communicate communicateScript;
    public Transform content;
    public GameObject keywordItemPrefab;

    private readonly List<GameObject> displayedItems = new();

    public void ShowKeywords()
    {
        ClearDisplayedKeywords();

        string[] keywords = infoScript.GetAvailableKeywords(communicateScript.stage);
        foreach (string keyword in keywords)
        {
            GameObject item = Instantiate(keywordItemPrefab, content);
            TextMeshProUGUI textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = keyword;
            }

            Button button = item.GetComponent<Button>();
            if (button != null)
            {
                button.enabled = false;
            }

            displayedItems.Add(item);
        }
    }

    private void ClearDisplayedKeywords()
    {
        foreach (GameObject item in displayedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        displayedItems.Clear();
    }
}
