using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Shop : Singleton<Shop>
{
    [SerializeField] 
    private GameObject shopButtonPrefab;
    private bool shopOpen = false;
    private ulong playerId;

    // TODO: build in autofind functionality
    [SerializeField] RectTransform buttonContainer;
    [SerializeField] TMP_Text goldDisplay;

    public void SetupShop()
    {
        playerId = NetworkManager.Singleton.LocalClientId;
        Debug.Log(playerId);
        transform.Find("Toggle Window Button").GetComponent<Button>().onClick.AddListener(() => ToggleShop()); ;

        Color playerColor = GameManager.Singleton.playerColors[playerId];
        foreach (NetworkPrefab prefab in GameManager.Singleton.shipList.PrefabList)
        {
            Transform shipPrefab = prefab.Prefab.transform;
            Transform button = Instantiate(shopButtonPrefab, buttonContainer).transform;
            button.Find("Ship Name").GetComponent<TMP_Text>().text = shipPrefab.GetComponent<Ship>().GetShipType().ToString();
            button.Find("Ship Sprite").GetComponent<Image>().sprite = shipPrefab.GetComponent<SpriteRenderer>().sprite;
            button.Find("Ship Color").GetComponent<Image>().sprite = shipPrefab.Find("Ship Accent").GetComponent<SpriteRenderer>().sprite;
            button.Find("Ship Color").GetComponent<Image>().color = playerColor;
            float shipCost = shipPrefab.GetComponent<Ship>().GetShipCost();
            button.Find("Ship Cost").GetComponent<TMP_Text>().text = "" + shipCost;

            button.GetComponent<Button>().onClick.AddListener(() => BuyShip(shipPrefab.GetComponent<Ship>().GetShipType(), shipCost));
        }

        UpdateGold();
    }

    float playerGold = 200f;

    private void UpdateGold()
    {
        goldDisplay.text = "$" + playerGold;
    }

    private void BuyShip(Ship.ShipTypes type, float cost)
    {
        if (playerGold >= cost)
        {
            playerGold -= cost;
            GameManager.Singleton.players[playerId].SpawnShipServerRPC(type);
        }

        UpdateGold();
    }    

    public void ToggleShop()
    {
        shopOpen = !shopOpen;
        if (shopOpen)
            transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        else
            transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, 0);
    }
}
