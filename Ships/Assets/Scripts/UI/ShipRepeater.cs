using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ShipRepeater : MonoBehaviour
{
    private NetworkPrefab shipPrefab;

    private TextMeshProUGUI shipName;
    private TextMeshProUGUI shipCost;
    private GameObject starIcon;
    private Image backgroundColor;
    private Button selectedButton;

    private Color selectedColor = new Color(150f / 255f, 150f / 255f, 150f / 255f);
    private Color unselectedColor = new Color(200f / 255f, 200f / 255f, 200f / 255f);

    void Awake()
    {
        shipName = transform.Find("Ship Name Text").GetComponent<TextMeshProUGUI>();
        shipCost = transform.Find("Ship Cost Text").GetComponent<TextMeshProUGUI>();
        starIcon = transform.Find("Ship Star Icon").gameObject;
        backgroundColor = transform.Find("Background Color").GetComponent<Image>();
        selectedButton = GetComponent<Button>();
        selectedButton.onClick.AddListener(() => Select());
    }

    public void Setup(NetworkPrefab ship)
    {
        shipPrefab = ship;

        shipName.text = shipPrefab.Prefab.GetComponent<Ship>().GetShipType().ToString();
        float shipCostAmount = shipPrefab.Prefab.GetComponent<Ship>().GetShipCost();
        if (shipCostAmount >= 0)
        {
            shipCost.text = "$" + shipCostAmount.ToString();
            starIcon.SetActive(false);
        }
        else //This is only running for mothership since mothership cost is -1
        {
            shipCost.text = "";
            starIcon.SetActive(true);
        }
    }

    public void Select()
    {
        MenuScreenManager.Singleton.SelectedShipChange(shipPrefab);
        backgroundColor.color = selectedColor;
    }

    public void Unselect()
    {
        backgroundColor.color = unselectedColor;
    }
}
