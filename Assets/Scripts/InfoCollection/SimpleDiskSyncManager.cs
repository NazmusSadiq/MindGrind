using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleDiskSyncManager : MonoBehaviour
{
    private const string ServerUploadUrl = "https://script.google.com/macros/s/AKfycbw4VOcDrxU6-Yg6j2_WbJSrfOeKO-eH11BnpygPxx3zJGrVu93EHEpNA5GpKEXjAAYOdw/exec";

    private const string ApiKey = "t7K#mQ9!zB2$vX4&wP6*eY1(nU3)jZ5[";

    private static SimpleDiskSyncManager runnerInstance;

    private static SimpleDiskSyncManager Runner
    {
        get
        {
            if (runnerInstance == null)
            {
                GameObject host = new GameObject("SimpleDiskSyncManager_Runner");
                Object.DontDestroyOnLoad(host);
                runnerInstance = host.AddComponent<SimpleDiskSyncManager>();
            }
            return runnerInstance;
        }
    }

    public static void PushLocalJsonToServer()
    {
        string localFilePath = Path.Combine(Application.persistentDataPath, "player_analytics.json");

        if (!File.Exists(localFilePath))
        {
            return;
        }

        try
        {
            string jsonTextString = File.ReadAllText(localFilePath);
            Runner.StartCoroutine(TransmitPayloadCoroutine(jsonTextString));
        }
        catch (System.Exception ex)
        {
            //Debug.LogError($"[Live Server Sync] Failed accessing data file stream: {ex.Message}");
        }
    }

    private static IEnumerator TransmitPayloadCoroutine(string rawJson)
    {
        string wrappedPayload = "{\"apiKey\":\"" + ApiKey + "\",\"payload\":" + rawJson + "}";

        using (UnityWebRequest request = new UnityWebRequest(ServerUploadUrl, "POST"))
        {
            byte[] rawBytes = Encoding.UTF8.GetBytes(wrappedPayload);
            request.uploadHandler = new UploadHandlerRaw(rawBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("User-Agent", "MindGrindUnityClient/1.0");
            request.redirectLimit = 5; 

            yield return request.SendWebRequest();

        }
    }
}