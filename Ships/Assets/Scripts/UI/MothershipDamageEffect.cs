using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MothershipDamageEffect : Singleton<MothershipDamageEffect>
{
    public Image[] effects;
    public float defaultOpacity;
    public float currentOpacity;
    public AudioSource effectSource;
    public ParticleSystem mothershipSource;

    void Start()
    {
        effectSource = GetComponent<AudioSource>();

        effects = transform.GetComponentsInChildren<Image>();
        defaultOpacity = effects[0].color.a;

        Color c = effects[0].color;
        c.a = 0;

        foreach (var effect in effects)
            effect.color = c;
    }

    void FixedUpdate()
    {
        if (mothershipSource == null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().GetMothership() != null)
        { 
            mothershipSource = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().GetMothership().transform.Find("Minimap Marker").GetComponent<ParticleSystem>();
        }

        if (currentOpacity > 0)
        {
            currentOpacity -= Time.deltaTime;
            if (currentOpacity < 0) currentOpacity = 0;

            Color c = effects[0].color;
            c.a = currentOpacity;

            foreach (var effect in effects)
                effect.color = c;
        }
    }

    public void TakeDamage()
    {
        currentOpacity = defaultOpacity;
        if (!effectSource.isPlaying)
            effectSource.Play();
        if (mothershipSource != null)
            mothershipSource.Play();
    }
}
