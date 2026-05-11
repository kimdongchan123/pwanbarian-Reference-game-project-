using UnityEngine;
using UnityEngine.UI;

public class EnemySlot : MonoBehaviour
{
    [SerializeField]
    private Enemy enemyPrefab;

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

    public void SetEnemyPrefab(Enemy prefab)
    {
        enemyPrefab = prefab;
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
