using UnityEngine;
using UnityEngine.UI;

public class TutorialHole : MonoBehaviour
{
    [SerializeField] RectTransform target;

    [SerializeField] RectTransform top;
    [SerializeField] RectTransform bottom;
    [SerializeField] RectTransform left;
    [SerializeField] RectTransform right;

    public float padding = 20f;

    void Update()
    {
        if (target == null)
            return;

        UpdateHole();
    }

    void UpdateHole()
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];

        float leftX = bottomLeft.x - padding;
        float rightX = topRight.x + padding;
        float bottomY = bottomLeft.y - padding;
        float topY = topRight.y + padding;

        top.position = new Vector3(Screen.width / 2, (topY + Screen.height) / 2);
        top.sizeDelta = new Vector2(Screen.width, Screen.height - topY);

        bottom.position = new Vector3(Screen.width / 2, bottomY / 2);
        bottom.sizeDelta = new Vector2(Screen.width, bottomY);

        left.position = new Vector3(leftX / 2, (topY + bottomY) / 2);
        left.sizeDelta = new Vector2(leftX, topY - bottomY);

        right.position = new Vector3((rightX + Screen.width) / 2, (topY + bottomY) / 2);
        right.sizeDelta = new Vector2(Screen.width - rightX, topY - bottomY);

        target.gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
    }
}
