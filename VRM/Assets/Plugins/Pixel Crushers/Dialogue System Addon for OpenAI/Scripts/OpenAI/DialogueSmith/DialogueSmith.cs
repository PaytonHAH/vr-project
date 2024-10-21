//--- Dialogue Smith has discontinued their service.

//// Copyright (c) Pixel Crushers. All rights reserved.

//#if USE_OPENAI

//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;

//namespace PixelCrushers.DialogueSystem.OpenAIAddon.DialogueSmith
//{

//    /// <summary>
//    /// Handles web requests to Dialogue Smith API.
//    /// </summary>
//    public static class DialogueSmith
//    {

//        public const string BranchingDialogueURL = "https://api.dialoguesmith.com/v1/integrations/branching-dialogue";

//        public static bool IsApiKeyValid(string apiKey)
//        {
//            return !string.IsNullOrEmpty(apiKey) && apiKey.Length == 36;
//        }

//        /// <summary>
//        ///  Given a dialogue tree with only node titles, generate the dialogue for the entire tree using those titles as a guide, will return 3 variations for each node. Token cost: 20
//        /// </summary>
//        /// <param name="apiKey">Dialogue Smith API key.</param>
//        /// <param name="request">Conversation tree data whose nodes' titles contain prompts.</param>
//        /// <param name="callback">Conversation tree with dialogue text filled in. Nodes whose dialogue text was already present are left unchanged.</param>
//        /// <returns></returns>
//        public static UnityWebRequestAsyncOperation SubmitBranchingDialogueAsync(string apiKey, BranchingDialogueData request, Action<BranchingDialogueData> callback)
//        {
//            string jsonData = JsonUtility.ToJson(request, true);
//            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

//#if UNITY_2022_1_OR_NEWER
//            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(BranchingDialogueURL, jsonData);
//#else
//            UnityWebRequest webRequest = UnityWebRequest.Post(BranchingDialogueURL, jsonData);
//#endif
//            webRequest.uploadHandler.Dispose();
//            webRequest.uploadHandler = new UploadHandlerRaw(postData);
//            webRequest.disposeUploadHandlerOnDispose = true;
//            webRequest.disposeDownloadHandlerOnDispose = true;
//            webRequest.SetRequestHeader("accept", "application/json");
//            webRequest.SetRequestHeader("api-key", apiKey);
//            webRequest.SetRequestHeader("Content-Type", "application/json");

//            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

//            asyncOp.completed += (op) =>
//            {
//                var success = webRequest.result == UnityWebRequest.Result.Success;
//                BranchingDialogueData data = null;
//                if (success)
//                {
//                    var text = webRequest.downloadHandler.text;
//                    if (text.StartsWith(@"{""message"""))
//                    {
//                        text = @"{""user_message""" + text.Substring(@"{""message""".Length);
//                    }
//                    data = JsonUtility.FromJson<BranchingDialogueData>(text);
//                }
//                else
//                {
//                    Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}"); 
//                }
//                webRequest.Dispose();
//                webRequest = null;

//                callback?.Invoke(data);
//            };

//            return asyncOp;
//        }

//    }

//}

//#endif
