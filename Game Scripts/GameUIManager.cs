using System.Collections;
using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject gameMessageText;

    [SerializeField]
    private Transform logger;

    [SerializeField]
    private TextMeshProUGUI fpsCounter;

    private float deltaTime;

    public void LogMessageInGame(string message, float seconds)
    {
        StartCoroutine(IELogMessageInGame(message, seconds));
    }
    private IEnumerator IELogMessageInGame(string message, float seconds)
    {
        TextMeshProUGUI newText =  Instantiate(gameMessageText, logger).GetComponent<TextMeshProUGUI>();
        newText.text = message;
        yield return new WaitForSeconds(seconds);
        Destroy(newText.gameObject);

    }

    private void Update()
    {

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsCounter.text = Mathf.Ceil(fps).ToString() + " FPS";
    }
}
