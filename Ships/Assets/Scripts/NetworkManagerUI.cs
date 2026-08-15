using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button serverButton;
    [SerializeField] private TMPro.TMP_InputField ipAddressInput;

    [SerializeField] private Button kaidenButton;
    [SerializeField] private Button drewButton;
    [SerializeField] private Button testButton;

    private void Awake()
    {
        hostButton.onClick.AddListener(() => {
            gameObject.SetActive(false);
            NetworkManager.Singleton.StartHost();
            GameManager.Singleton.ChangeState(GameState.Gameplay);
        });

        joinButton.onClick.AddListener(() => {
            // TODO: Check if actual IP
            //if (ipAddressInput.text != "")
            //{
                //NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = ipAddressInput.text;
                NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = "73.127.93.157";
                gameObject.SetActive(false);
                NetworkManager.Singleton.StartClient();
                GameManager.Singleton.ChangeState(GameState.Gameplay);
            //}
        });

        kaidenButton.onClick.AddListener(() => {
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = "131.93.247.207";
            gameObject.SetActive(false);
            NetworkManager.Singleton.StartClient();
            GameManager.Singleton.ChangeState(GameState.Gameplay);
        });

        drewButton.onClick.AddListener(() => {
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = "184.98.3.110";
            gameObject.SetActive(false);
            NetworkManager.Singleton.StartClient();
            GameManager.Singleton.ChangeState(GameState.Gameplay);
        });

        testButton.onClick.AddListener(() => {
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = "127.0.0.1";
            gameObject.SetActive(false);
            NetworkManager.Singleton.StartClient();
            GameManager.Singleton.ChangeState(GameState.Gameplay);
        });
    }
}

