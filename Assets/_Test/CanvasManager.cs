using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] bool testing;
    [SerializeField] string defaultServerIP = "73.127.93.157";
    [SerializeField] string localServerIP = "10.0.0.248";

    // Start is called before the first frame update
    private void Start()
    {
        Button hostButton = transform.Find("Host Button").GetComponent<Button>();
        hostButton.onClick.AddListener(() => {
            gameObject.SetActive(false);
            NetworkManager.Singleton.StartHost();
        });

        Button joinButton = transform.Find("Join Button").GetComponent<Button>();
        joinButton.onClick.AddListener(() => {
            JoinServer(defaultServerIP);
        });

        // TODO: Setup joining of local server for myself

        //NetworkManager.Singleton.OnClientConnectedCallback += (e) => { Debug.Log("Connected"); };
        //NetworkManager.Singleton.OnClientDisconnectCallback += (e) => { Debug.Log("hi"); };

        //NetworkManager.Singleton.GetComponent<NetworkTransport>().OnTransportEvent += OnTransportEvent;
    }

    private void OnTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
    {
        Debug.Log("test");
        Debug.Log(eventType);
        JoinServer(localServerIP);
    }

    private void JoinServer(string ip)
    {
        if (!testing)
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = ip;
        gameObject.SetActive(false);
        NetworkManager.Singleton.StartClient();
    }
} 
