using UnityEngine;
using UnityEngine.UI;

public class BattleSideUI : MonoBehaviour
{
    [SerializeField]
    private Image portraitImage;
    [SerializeField]
    private GameObject nameplatePannel;
    [SerializeField]
    private GameObject nameplatePrefab;

    public void AddNameplate(UnitData data)
    {
        GameObject nameplateObj = Instantiate(nameplatePrefab, nameplatePannel.transform);
        Nameplate nameplate = nameplateObj.GetComponent<Nameplate>();
        nameplate.SetNameplate(data);
    }

    public void AddNameplate(EnemyData data)
    {
        GameObject nameplateObj = Instantiate(nameplatePrefab, nameplatePannel.transform);
        Nameplate nameplate = nameplateObj.GetComponent<Nameplate>();
        nameplate.SetNameplate(data);
    }

    public void ClearNameplates()
    {
        foreach (Transform child in nameplatePannel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetPortrait(Sprite portrait)
    {
        portraitImage.sprite = portrait;
    }
}
