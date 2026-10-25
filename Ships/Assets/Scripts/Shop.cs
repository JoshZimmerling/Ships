using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Shop : Singleton<Shop>
{
    [SerializeField] private GameObject shopButtonPrefab;
    [SerializeField] private GameObject shopThrusterPrefab;
    private bool shopOpen = false;
    private PlayerData playerData;
    private TMP_Text goldDisplay;
    private TMP_Text shopToggleButtonText;

    List<ParticleSystem> thrusterParticlesSystemList;

    float playerGold = 100f;

    public void SetupShop()
    {
        playerData = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>();

        transform.Find("Toggle Window Button").GetComponent<Button>().onClick.AddListener(() => ToggleShop()); ;

        goldDisplay = transform.Find("Money Display/Money Text").GetComponent<TMP_Text>();
        shopToggleButtonText = transform.Find("Toggle Window Button/Toggle Shop Text").GetComponent<TMP_Text>();

        Color playerColor = playerData.playerColor;

        int shipIndexInFaction = 0;
        thrusterParticlesSystemList = new List<ParticleSystem>();
        foreach (NetworkPrefab prefab in Factions.Singleton.factionList[MenuScreenManager.Singleton.GetSelectedFactionIndex()].factionShipList.PrefabList)
        {
            Transform shipPrefab = prefab.Prefab.transform;
            float shipCost = shipPrefab.GetComponent<Ship>().GetShipCost();
            int shipIndex = shipIndexInFaction; //Need to make a copy in the loop to avoid passing a reference that gets updated
            if (shipCost > 0)
            {
                Transform button = Instantiate(shopButtonPrefab, transform.Find("Shop Buttons")).transform;
                button.Find("Ship Name").GetComponent<TMP_Text>().text = shipPrefab.name;
                button.Find("Ship Sprite").GetComponent<Image>().sprite = shipPrefab.Find("Ship Sprite").GetComponent<SpriteRenderer>().sprite;
                button.Find("Ship Color").GetComponent<Image>().sprite = shipPrefab.Find("Ship Sprite/Ship Accent").GetComponent<SpriteRenderer>().sprite;
                button.Find("Ship Color").GetComponent<Image>().color = playerColor;
                button.Find("Ship Cost").GetComponent<TMP_Text>().text = "" + shipCost;

                button.GetComponent<Button>().onClick.AddListener(() => BuyShip(shipIndex, shipCost));

                foreach (Transform thruster in shipPrefab.transform.Find("Ship Sprite/Thrusters"))
                {
                    ParticleSystem srcThruster = thruster.GetComponent<ParticleSystem>();
                    ParticleSystem dstThruster = Instantiate(shopThrusterPrefab, button.Find("Ship Thrusters")).GetComponent<ParticleSystem>();

                    // 1. Main Module Settings
                    ParticleSystem.MainModule srcMain = srcThruster.main;
                    ParticleSystem.MainModule dstMain = dstThruster.main;

                    dstMain.duration = srcMain.duration;
                    dstMain.startLifetime = srcMain.startLifetime;
                    dstMain.startSpeed = srcMain.startSpeed;
                    dstMain.startSize = srcMain.startSize;
                    dstMain.simulationSpace = srcMain.simulationSpace;

                    // 2. Emission Module Settings
                    ParticleSystem.EmissionModule srcEmission = srcThruster.emission;
                    ParticleSystem.EmissionModule dstEmission = dstThruster.emission;

                    dstEmission.rateOverTime = srcEmission.rateOverTime;

                    // 3. Shape Module Settings
                    ParticleSystem.ShapeModule srcShape = srcThruster.shape;
                    ParticleSystem.ShapeModule dstShape = dstThruster.shape;

                    dstShape.shapeType = srcShape.shapeType;
                    dstShape.radius = srcShape.radius;
                    dstShape.position = srcShape.position;
                    dstShape.rotation = srcShape.rotation;

                    // 4. Color Over Lifetime Module Settings
                    ParticleSystem.ColorOverLifetimeModule srcCLT = srcThruster.colorOverLifetime;
                    ParticleSystem.ColorOverLifetimeModule dstCLT = dstThruster.colorOverLifetime;

                    dstCLT.color = srcCLT.color;

                    // 5. Size Over Lifetime Module Settings
                    ParticleSystem.SizeOverLifetimeModule srcSLT = srcThruster.sizeOverLifetime;
                    ParticleSystem.SizeOverLifetimeModule dstSLT = dstThruster.sizeOverLifetime;

                    dstSLT.size = srcSLT.size;

                    // 6. Start
                    dstThruster.Play();
                    thrusterParticlesSystemList.Add(dstThruster);
                }
            }
            shipIndexInFaction++;
        }

        UpdateGold();
    }

    private void UpdateGold()
    {
        goldDisplay.text = "$" + playerGold;
    }

    private void BuyShip(int shipIndex, float cost)
    {
        if (playerGold >= cost && playerData.IsMothershipAlive())
        {
            playerGold -= cost;
            playerData.SpawnShipServerRPC(MenuScreenManager.Singleton.GetSelectedFactionIndex(), shipIndex);
        }

        UpdateGold();
    }    

    public void AddGold(int increaseAmount)
    {
        playerGold += increaseAmount;
        UpdateGold();
    }

    public float GetGold()
    {
        return playerGold;
    }

    public void ToggleShop()
    {
        shopOpen = !shopOpen;
        if (shopOpen)
        {
            transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            shopToggleButtonText.text = ">";
            foreach (ParticleSystem thrusterParticles in thrusterParticlesSystemList)
                thrusterParticles.Play();
        }
        else
        {
            transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().rect.width, 0);
            shopToggleButtonText.text = "<";
            foreach (ParticleSystem thrusterParticles in thrusterParticlesSystemList)
                thrusterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
