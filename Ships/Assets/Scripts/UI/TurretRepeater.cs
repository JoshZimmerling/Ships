using UnityEngine;
using UnityEngine.UI;

public class TurretRepeater : MonoBehaviour
{
    private Button selectedButton;

    public Transform turretTransform;
    private bool droneTurret = false;

    void Awake()
    {
        selectedButton = GetComponent<Button>();
        selectedButton.onClick.AddListener(() => Select());
    }

    public void Setup(Transform turret)
    {
        turretTransform = turret;
        droneTurret = false;

        transform.localPosition = turretTransform.localPosition * 50;
    }

    public void SetupDroneTurret(Transform turret)
    {
        turretTransform = turret;
        droneTurret = true;

        transform.localPosition = turretTransform.localPosition * 50;
    }

    public void Select()
    {
        if (droneTurret)
            MenuScreenManager.Singleton.SelectedDroneTurretChange(this);
        else
            MenuScreenManager.Singleton.SelectedTurretChange(this);
    }
}
