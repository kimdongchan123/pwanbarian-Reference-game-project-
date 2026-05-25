using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Nameplate : MonoBehaviour
{
    [SerializeField]
    private Image nameplateImage;
    [SerializeField]
    private TextMeshProUGUI nameText;

    public void SetNameplate(UnitData data)
    {
        nameText.text = data.unitName;
        nameplateImage.sprite = data.portraitSprite;
    }

    public void SetNameplate(EnemyData data)
    {
        nameText.text = data.unitName;
        nameplateImage.sprite = data.portraitSprite;
    }


}
