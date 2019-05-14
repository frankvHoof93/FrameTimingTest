using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{ 
    public Text uiText;

    float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void LateUpdate()
    {
        uiText.text = string.Format("{0:0.0} ms ({1:0.} fps)", (deltaTime * 1000.0f), 1.0f / deltaTime);
    }
}