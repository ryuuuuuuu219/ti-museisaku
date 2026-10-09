using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToolInputManager : MonoBehaviour
{
    public ConversationController ConversationController;

    [Tooltip("道具のUIオブジェクト。Buttonがなければ実行時に自動追加します。")]
    public List<GameObject> tool = new List<GameObject>();
    [Tooltip("toolと同じ番号のボタンで送信する文字列")]
    public List<string> toolname = new List<string>();

    private readonly List<Button> registeredButtons = new List<Button>();
    private readonly List<UnityAction> registeredActions = new List<UnityAction>();

    private void OnEnable()
    {
        if (tool == null || toolname == null)
            return;

        if (tool.Count != toolname.Count)
            Debug.LogWarning("toolとtoolnameの要素数を揃えてください。対応する文字列のないボタンは登録しません。", this);

        for (int i = 0; i < tool.Count && i < toolname.Count; i++)
        {
            var obj = tool[i];
            if (obj == null)
            {
                Debug.LogWarning("tool[" + i + "]にGameObjectを設定してください。", this);
                continue;
            }

            var button = obj.GetComponent<Button>();
            if (button == null)
                button = obj.AddComponent<Button>();

            if (button.targetGraphic == null)
                button.targetGraphic = obj.GetComponent<Graphic>();
            if (button.targetGraphic != null)
                button.targetGraphic.raycastTarget = true;

            // ボタンごとの番号を保持する。クリックされるまで送信しない。
            int id = i;
            UnityAction action = () => UseTool(id);
            button.onClick.AddListener(action);
            registeredButtons.Add(button);
            registeredActions.Add(action);
        }
    }

    public void UseTool(int id)
    {
        if (!isActiveAndEnabled)
            return;

        if (toolname == null || id < 0 || id >= toolname.Count || string.IsNullOrWhiteSpace(toolname[id]))
        {
            Debug.LogWarning("道具のプリセット入力が未設定です。番号: " + id, this);
            return;
        }

        if (ConversationController == null || ConversationController.InputField == null)
        {
            Debug.LogWarning("ConversationControllerとInputFieldを設定してください。", this);
            return;
        }

        ConversationController.InputField.text = toolname[id]+"を使用する";
        ConversationController.Onclick_send();
    }

    private void OnDisable()
    {
        // このマネージャーが登録した処理だけを解除する。
        for (int i = 0; i < registeredButtons.Count; i++)
        {
            if (registeredButtons[i] != null)
                registeredButtons[i].onClick.RemoveListener(registeredActions[i]);
        }
        registeredButtons.Clear();
        registeredActions.Clear();
    }
}
