using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class ConsoleLogger : MonoBehaviour
{
    static string myLog = "";
    private string output;
    private string stack;

    [SerializeField] private TextMeshProUGUI text;

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;
        myLog = output + '\n' + myLog;
        text.text = myLog;
    }
}