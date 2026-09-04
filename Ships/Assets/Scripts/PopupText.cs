using TMPro;
using UnityEngine;

public class PopupText : MonoBehaviour
{
    private bool isSetup = false;
    private float textFloatingTime = 2.5f;
    private float remainingTextTime = 2.5f;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isSetup) return;

        remainingTextTime -= Time.deltaTime;
        transform.position = new Vector2(transform.position.x, transform.position.y + .05f);
        gameObject.GetComponent<TMP_Text>().color = new Color(gameObject.GetComponent<TMP_Text>().color.r, gameObject.GetComponent<TMP_Text>().color.g, gameObject.GetComponent<TMP_Text>().color.b, remainingTextTime/textFloatingTime);
    
        if (remainingTextTime < 0f)
            Destroy(gameObject);
    }

    public void SetupText(string textMessage)
    {
        gameObject.GetComponent<TMP_Text>().text = textMessage;
        gameObject.GetComponent<TMP_Text>().color = Color.white;

        isSetup = true;
    }

    public void SetupText(string textMessage, Color textColor, float timeTilDisappear)
    {
        gameObject.GetComponent<TMP_Text>().text = textMessage;
        gameObject.GetComponent<TMP_Text>().color = textColor;
        textFloatingTime = timeTilDisappear;
        remainingTextTime = textFloatingTime;

        isSetup = true;
    }
}
