using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemySlot : MonoBehaviour
{
    [SerializeField]
    private Enemy enemyPrefab;

    [SerializeField]
    private TextMeshProUGUI enemyCount;

    private Image enemyImage;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        enemyImage = GetComponent<Image>();
    }

    public void OnPointerEnter()
    {
        rectTransform.localScale = Vector3.one * 1.1f;
        UnitInspector.Instance.ShowEnemyInfo(enemyPrefab);
    }

    public void OnPointerExit()
    {
        rectTransform.localScale = Vector3.one;
        UnitInspector.Instance.HideInfo();
    }

    public void SetEnemyPrefab(Enemy prefab, int count = 1)
    {
        enemyPrefab = prefab;
        enemyCount.text = count > 1 ? $"x{count}" : "";
        if (enemyPrefab != null)
        {
            SpriteRenderer spriteRenderer = enemyPrefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                enemyImage.sprite = spriteRenderer.sprite;
                enemyImage.color = Color.white;
                enemyImage.preserveAspect = true;
            }
        }
        else
        {
            enemyImage.color = Color.clear;
        }
    }
}
