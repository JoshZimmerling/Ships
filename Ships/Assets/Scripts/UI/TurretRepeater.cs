using UnityEngine;
using UnityEngine.UI;

public class TurretRepeater : MonoBehaviour
{
    private Button selectedButton;

    public Transform turretTransform;

    void Awake()
    {
        selectedButton = GetComponent<Button>();
        selectedButton.onClick.AddListener(() => Select());
    }

    public void Setup(Transform turret)
    {
        turretTransform = turret;

        transform.localPosition = turretTransform.localPosition * 50;
    }

    public void Select()
    {
        MenuScreenManager.Singleton.SelectedTurretChange(this);
    }
}
