// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine.Networking;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Common web request utility methods.
    /// </summary>
    public static class WebRequestUtility
    {

        public static UnityWebRequest CreateWebRequest(string apiKey, string url, string jsonData)
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

#if UNITY_2022_2_OR_NEWER
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(url, jsonData);
#else
            UnityWebRequest webRequest = UnityWebRequest.Post(url, jsonData);
#endif
            webRequest.uploadHandler.Dispose();
            webRequest.uploadHandler = new UploadHandlerRaw(postData);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);

            return webRequest;
        }
    }

}

#endif
