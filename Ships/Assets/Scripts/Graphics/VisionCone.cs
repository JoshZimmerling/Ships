using UnityEngine;

public class VisionCone : MonoBehaviour
{
    [SerializeField] private bool decays = true;

    public float decayDuration = 0.5f;
    private float timer;
    public float visionRange;
    public float alphaValue = 1f;

    private void Start()
    {
        timer = decayDuration;
    }

    // Update is called once per frame
    void Update()
    {
        if (decays)
        {
            timer -= Time.deltaTime;
            //visionRange -= Time.deltaTime * 0.2f;
            alphaValue = timer / decayDuration;
            if (alphaValue < 0f) alphaValue = 0f;
        }
    }
}
