using UnityEngine;

public class menuAnime : MonoBehaviour
{
    public GameObject menu;

    public Vector3 CollapsemenuPos;
    public Vector3 ExpandmenuPos;

    public bool isExpanded = false;

    bool isTransitioning = false;
    float timeElapsed = 0f;

    public void OnClick_Menu()
    {
        if (isTransitioning)
            return;

        isExpanded = !isExpanded;

        timeElapsed = 0f;
        isTransitioning = true;
    }

    private void Update()
    {
        if (isTransitioning)
        {
            timeElapsed += Time.deltaTime;

            float t = Mathf.Clamp01(timeElapsed / 0.5f);

            Vector3 startPos =
                isExpanded ? CollapsemenuPos : ExpandmenuPos;

            Vector3 endPos =
                isExpanded ? ExpandmenuPos : CollapsemenuPos;

            menu.transform.position =
                Vector3.Lerp(startPos, endPos, t);

            if (t >= 1f)
            {
                isTransitioning = false;
            }
        }
    }
}