// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{

    /// <summary>
    /// Handles web requests to ElevenLabs API.
    /// </summary>
    public static class ElevenLabsAPI
    {

        public const string ModelListURL = "https://api.elevenlabs.io/v1/models";
        public const string VoiceListURL = "https://api.elevenlabs.io/v1/voices";
        public const string TextToSpeechURL = "https://api.elevenlabs.io/v1/text-to-speech";

        public enum Models
        {
            Monolingual_v1,
            Multilingual_v1,
            Multilingual_v2,
            Turbo_v2
        }

        public static bool IsApiKeyValid(string apiKey)
        {
            return !string.IsNullOrEmpty(apiKey);
        }

        public static string GetDefaultModelId()
        {
            return GetModelId(Models.Monolingual_v1);
        }

        public static string GetModelId(Models model)
        {
            switch (model)
            {
                default:
                case Models.Monolingual_v1:
                    return "eleven_monolingual_v1";
                case Models.Multilingual_v1:
                    return "eleven_multilingual_v1";
                case Models.Multilingual_v2:
                    return "eleven_multilingual_v2";
                case Models.Turbo_v2:
                    return "eleven_turbo_v2";
            }
        }

        /// <summary>
        /// Gets a list of all available voices for a user.
        /// </summary>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <param name="callback">List of voices.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation GetVoiceList(string apiKey, Action<VoiceList> callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(VoiceListURL);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.SetRequestHeader("xi-api-key", apiKey);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.LogError($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                VoiceList voiceList = null;
                if (!string.IsNullOrEmpty(text))
                {
                    voiceList = JsonUtility.FromJson<VoiceList>(text);
                }
                callback?.Invoke(voiceList);
            };

            return asyncOp;
        }

        /// <summary>
        /// Converts text into speech using a voice of your choice and returns audio.
        /// </summary>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <param name="modelId">ID of the model to use.</param>
        /// <param name="voiceName">Name of voice to use.</param>
        /// <param name="voiceId">ID of voice to use.</param>
        /// <param name="stability">Stability parameter (0.0 - 1.0).</param>
        /// <param name="similarityBoost">Similarity boost parameter (0.0 - 1.0).</param>
        /// <param name="styleExaggeration">Style exaggeration parameter (0.0 - 2.0).</param>
        /// <param name="text">Text to convert to speech audio.</param>
        /// <param name="callback">Resulting audio clip.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation GetTextToSpeech(
            string apiKey,
            string modelId,
            string voiceName,
            string voiceId,
            float stability,
            float similarityBoost,
            float styleExaggeration,
            string text,
            Action<AudioClip> callback)
        {
            var url = $"{TextToSpeechURL}/{voiceId}/stream";

            var voiceSettings = new VoiceSettings
            {
                stability = stability,
                similarity_boost = similarityBoost,
                style = styleExaggeration,
                use_speaker_boost = false
            };

            var ttsRequest = new TextToSpeechRequest
            {
                text = text,
                model_id = modelId,
                voice_settings = voiceSettings
            };

            string jsonData = JsonUtility.ToJson(ttsRequest);

            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

#if UNITY_2022_2_OR_NEWER
            UnityWebRequest webRequest = UnityWebRequest.PostWwwForm(url, jsonData);
#else
            UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
#endif
            webRequest.uploadHandler = new UploadHandlerRaw(postData);
            webRequest.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("accept", "audio/mpeg");
            webRequest.SetRequestHeader("xi-api-key", apiKey);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                AudioClip audioClip = null;
                var success = webRequest.result == UnityWebRequest.Result.Success;
                if (success)
                {
                    audioClip = DownloadHandlerAudioClip.GetContent(webRequest);
                }
                else
                {
                    Debug.LogError($"Error generating speech: {webRequest.error}\n{webRequest.downloadHandler.text}");
                }
                webRequest.Dispose();
                webRequest = null;

                callback?.Invoke(audioClip);
            };

            return asyncOp;
        }

        // Additional classes needed for serialization
        [Serializable]
        public class TextToSpeechRequest
        {
            public string text;
            public string model_id;
            public VoiceSettings voice_settings;
        }

        [Serializable]
        public class VoiceSettings
        {
            public float stability;
            public float similarity_boost;
            public float style;
            public bool use_speaker_boost;
        }

    }

    [Serializable]
    public class VoiceList
    {
        public Voice[] voices;
    }

    [Serializable]
    public class Voice
    {
        public string voice_id;
        public string name;
        // Add other fields as necessary
    }
}

#endif
