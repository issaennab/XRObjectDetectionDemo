using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TestRequest : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[TestRequest] Awake() called — script is loaded and active on GameObject: " + gameObject.name);
    }

    void OnEnable()
    {
        Debug.Log("[TestRequest] OnEnable() called — component enabled.");
    }

    void Start()
    {
        Debug.Log("[TestRequest] Start() called — starting coroutine...");
        StartCoroutine(SendFakeRequest());
    }

    IEnumerator SendFakeRequest()
    {
        string url = "https://jsonplaceholder.typicode.com/todos/1";
        Debug.Log("[TestRequest] SendFakeRequest() started. URL: " + url);

        UnityWebRequest req = UnityWebRequest.Get(url);

        Debug.Log("[TestRequest] Sending request...");
        yield return req.SendWebRequest();

        Debug.Log("[TestRequest] Request finished. Result: " + req.result);

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[TestRequest] SUCCESS — Response received:");
            Debug.Log("[TestRequest] Raw JSON: " + req.downloadHandler.text);
        }
        else
        {
            Debug.LogError("[TestRequest] ERROR — Request failed: " + req.error);
        }

        Debug.Log("[TestRequest] Coroutine end.");
    }

    void OnDisable()
    {
        Debug.Log("[TestRequest] OnDisable() called — component disabled.");
    }

    void OnDestroy()
    {
        Debug.Log("[TestRequest] OnDestroy() called — script removed or GameObject destroyed.");
    }
}