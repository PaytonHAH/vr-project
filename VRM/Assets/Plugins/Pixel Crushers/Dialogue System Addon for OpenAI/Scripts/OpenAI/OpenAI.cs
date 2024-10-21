// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    public enum TextModelName 
    { 
        GPT_4o, GPT_4o_mini, GPT_4_Turbo, GPT_4, GPT_4_32K, 
        GPT3_5_Turbo, GPT3_5_Turbo_16K, 
        Davinci, Curie, Babbage, Ada,
        FineTune
    }

    /// <summary>
    /// Handles web requests to OpenAI API.
    /// </summary>
    public static class OpenAI
    {

        /// <summary>
        /// If you want to use a different base URL that conforms to the same API,
        /// change BaseURL.
        /// </summary>
        public static string BaseURL { get; set; } = "https://api.openai.com/v1/";

        public const string OpenAIURL = "https://api.openai.com/v1/";

        public static string CompletionsURL => $"{BaseURL}/completions";
        public static string ChatURL => $"{BaseURL}/chat/completions";
        public static string EditsURL => $"{BaseURL}/edits";
        public static string FineTunesURL => $"{BaseURL}/fine-tunes";
        public static string FineTuningURL => $"{BaseURL}/fine_tuning";
        public static string AudioTranscriptionsURL => $"{BaseURL}/audio/transcriptions";
        public static string AudioTranslationsURL => $"{BaseURL}/translations";
        public static string AudioSpeechURL => $"{BaseURL}/audio/speech";
        public static string ImageGenerationsURL => $"{BaseURL}/images/generations";
        public static string ImageEditsURL => $"{BaseURL}/images/edits";
        public static string ImageVariationsURL => $"{BaseURL}/images/variations";

        public static string TTSModel1 = "tts-1";
        public static string TTSModel1HD = "tts-1-hd";

        public const string ResponseFormatURL = "url";
        public const string ResponseFormatB64JSON = "b64_json";

        public static bool IsApiKeyValid(string apiKey)
        {
            return !string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("sk-");
        }

        #region Text Generation

        public static Model NameToModel(TextModelName modelName)
        {
            switch (modelName)
            {
                default: return Model.GPT3_5_Turbo_16K;
                case TextModelName.GPT3_5_Turbo: return Model.GPT3_5_Turbo;
                case TextModelName.Ada: return Model.Ada;
                case TextModelName.Babbage: return Model.Babbage;
                case TextModelName.Curie: return Model.Curie;
                case TextModelName.Davinci: return Model.Davinci_003;
                case TextModelName.GPT_4: return Model.GPT4;
                case TextModelName.GPT_4_32K: return Model.GPT4_32K;
                case TextModelName.GPT_4_Turbo: return Model.GPT4_Turbo;
                case TextModelName.GPT_4o: return Model.GPT4o;
                case TextModelName.GPT_4o_mini: return Model.GPT4o_mini;
            }
        }

        /// <summary>
        /// Given a prompt, the model will return one or more predicted completions, and can also return the probabilities of alternative tokens at each position.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">Model to use.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.</param>
        /// <param name="prompt">The prompt(s) to generate completions for, encoded as a string, array of strings, array of tokens, or array of token arrays.</param>
        /// <param name="callback">This event handler will be passed the API result.</param>
        public static UnityWebRequestAsyncOperation SubmitCompletionAsync(string apiKey, Model model,
            float temperature, int maxTokens,
            string prompt, Action<string> callback)
        {
            return SubmitCompletionAsync(apiKey, model,
                temperature, top_p: 1, frequency_penalty: 0, presence_penalty: 0,
                maxTokens, prompt, callback);
        }

        /// <summary>
        /// Given a prompt, the model will return one or more predicted completions, and can also return the probabilities of alternative tokens at each position.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">Model to use.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. We generally recommend altering this or top_p but not both.</param>
        /// <param name="top_p">An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend altering this or temperature but not both.</param>
        /// <param name="frequency_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.</param>
        /// <param name="presence_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.</param>
        /// <param name="prompt">The prompt(s) to generate completions for, encoded as a string, array of strings, array of tokens, or array of token arrays.</param>
        /// <param name="callback">This event handler will be passed the API result.</param>
        public static UnityWebRequestAsyncOperation SubmitCompletionAsync(string apiKey, Model model,
            float temperature, float top_p,
            float frequency_penalty, float presence_penalty,
            int maxTokens,
            string prompt, Action<string> callback)
        {
            var completionRequest = new CompletionRequest(model.Name, prompt, temperature, top_p,
                frequency_penalty, presence_penalty, maxTokens);
            string jsonData = JsonUtility.ToJson(completionRequest);

            UnityWebRequest webRequest = WebRequestUtility.CreateWebRequest(apiKey, CompletionsURL, jsonData);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                if (!string.IsNullOrEmpty(text))
                {
                    var responseData = JsonUtility.FromJson<CompletionResponse>(text);
                    text = responseData.choices[0].text.Trim();
                }
                callback?.Invoke(text);
            };

            return asyncOp;
        }

        /// <summary>
        /// Given a prompt and an instruction, the model will return an edited version of the prompt.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">Model to use.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.</param>
        /// <param name="input">The prompt(s) to generate completions for, encoded as a string, array of strings, array of tokens, or array of token arrays.</param>
        /// <param name="instruction">The instruction that tells the model how to edit the prompt.</param>
        /// <param name="callback">This event handler will be passed the API result.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation SubmitEditAsync(string apiKey, Model model, float temperature, int maxTokens,
            string input, string instruction, Action<string> callback)
        {
            return SubmitEditAsync(apiKey, model, temperature,
                top_p: 1, frequency_penalty: 0, presence_penalty: 0,
                maxTokens, input, instruction, callback);
        }

        /// <summary>
        /// Given a prompt and an instruction, the model will return an edited version of the prompt.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">Model to use.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.</param>
        /// <param name="top_p">An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend altering this or temperature but not both.</param>
        /// <param name="frequency_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.</param>
        /// <param name="presence_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.</param>
        /// <param name="input">The prompt(s) to generate completions for, encoded as a string, array of strings, array of tokens, or array of token arrays.</param>
        /// <param name="instruction">The instruction that tells the model how to edit the prompt.</param>
        /// <param name="callback">This event handler will be passed the API result.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation SubmitEditAsync(string apiKey, Model model,
            float temperature, float top_p,
            float frequency_penalty, float presence_penalty,
            int maxTokens,
            string input, string instruction, Action<string> callback)
        {
            // Note: Must use text-davinci-edit-001 for Edits.
            var editRequest = new EditRequest(Model.Davinci_Edit_001.Name, input, instruction,
                temperature, top_p, frequency_penalty, presence_penalty, maxTokens);
            string jsonData = JsonUtility.ToJson(editRequest);

            UnityWebRequest webRequest = WebRequestUtility.CreateWebRequest(apiKey, EditsURL, jsonData);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                if (!string.IsNullOrEmpty(text))
                {
                    var responseData = JsonUtility.FromJson<EditResponse>(text);
                    text = responseData.choices[0].text.Trim();
                }
                callback?.Invoke(text);
            };

            return asyncOp;
        }

        /// <summary>
        /// Given a list of messages comprising a conversation, the model will return a response.
        /// </summary>
        /// 
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">Model to use.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.</param>
        /// <param name="messages">A list of messages comprising the conversation so far.</param>
        /// <param name="callback">This event handler will be passed the API result.</param>
        public static UnityWebRequestAsyncOperation SubmitChatAsync(string apiKey, Model model, float temperature, int maxTokens,
            List<ChatMessage> messages, Action<string> callback)
        {
            return SubmitChatAsync(apiKey, model, temperature,
                top_p: 1, frequency_penalty: 0, presence_penalty: 0, maxTokens, messages, callback);
        }

        /// <summary>
        /// Given a list of messages comprising a conversation, the model will return a response.
        /// </summary>
        /// 
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">Model to use.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.</param>
        /// <param name="top_p">An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend altering this or temperature but not both.</param>
        /// <param name="frequency_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.</param>
        /// <param name="presence_penalty">Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.</param>
        /// <param name="messages">A list of messages comprising the conversation so far.</param>
        /// <param name="callback">This event handler will be passed the API result.</param>
        public static UnityWebRequestAsyncOperation SubmitChatAsync(string apiKey, Model model, 
            float temperature, float top_p,
            float frequency_penalty, float presence_penalty, 
            int maxTokens,
            List<ChatMessage> messages, Action<string> callback)
        {
            var chatRequest = new ChatRequest(model.Name, messages, 
                temperature, top_p, frequency_penalty, presence_penalty,
                maxTokens);
            string jsonData = JsonUtility.ToJson(chatRequest);

            UnityWebRequest webRequest = WebRequestUtility.CreateWebRequest(apiKey, ChatURL, jsonData);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                if (!string.IsNullOrEmpty(text))
                {
                    var responseData = JsonUtility.FromJson<ChatResponse>(text);
                    text = responseData.choices[0].message.content.Trim();
                }
                callback?.Invoke(text);
            };

            return asyncOp;
        }

        #endregion

        #region Image Generation

        /// <summary>
        /// Given a prompt and/or an input image, the model will generate a new image.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="n">The number of images to generate. Must be between 1 and 10.</param>
        /// <param name="imageSize">The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024.</param>
        /// <param name="response_format">The format in which the generated images are returned. Must be one of url or b64_json.</param>
        /// <param name="user">A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.</param>
        /// <param name="prompt">A text description of the desired image(s). The maximum length is 1000 characters.</param>
        /// <param name="callback">The data returned by the API call.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation SubmitImageGenerationAsync(string apiKey, int n, string imageSize,
            string response_format, string user, string prompt, Action<List<string>> callback)
        {
            var imageRequest = new ImageGenerationRequest(prompt, n, imageSize, response_format, user);
            string jsonData = JsonUtility.ToJson(imageRequest);

            UnityWebRequest webRequest = WebRequestUtility.CreateWebRequest(apiKey, ImageGenerationsURL, jsonData);

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                var result = new List<string>();

                if (!string.IsNullOrEmpty(text))
                {
                    var responseData = JsonUtility.FromJson<ImagesResponse>(text);
                    foreach (var imageResponse in responseData.data)
                    {
                        result.Add((response_format == ResponseFormatB64JSON) ? imageResponse.b64_json : imageResponse.url);
                    }
                }
                callback?.Invoke(result);
            };

            return asyncOp;
        }

        /// <summary>
        /// Creates an edited or extended image given an original image and a prompt.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="image">The image to edit. Must be a valid PNG file, less than 4MB, and square. If mask is not provided, image must have transparency, which will be used as the mask.</param>
        /// <param name="mask">An additional image whose fully transparent areas (e.g. where alpha is zero) indicate where image should be edited. Must be a valid PNG file, less than 4MB, and have the same dimensions as image.</param>
        /// <param name="n">The number of images to generate. Must be between 1 and 10.</param>
        /// <param name="imageSize">The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024.</param>
        /// <param name="response_format">The format in which the generated images are returned. Must be one of url or b64_json.</param>
        /// <param name="user">A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.</param>
        /// <param name="prompt">A text description of the desired image(s). The maximum length is 1000 characters.</param>
        /// <param name="callback">The data returned by the API call.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation SubmitImageEditAsync(string apiKey, Texture2D image, Texture2D mask, 
            int n, string imageSize, string response_format, string user, string prompt, Action<List<string>> callback)
        {
            var imageBytes = image.EncodeToPNG();
            if (mask == null)
            {
                mask = new Texture2D(image.width, image.height);
                var resetColor = new Color32(255, 255, 255, 0);
                var colorArray = mask.GetPixels32();
                for (int i = 0; i < colorArray.Length; i++)
                {
                    colorArray[i] = resetColor;
                }
                mask.SetPixels32(colorArray);
                mask.Apply();
            }
            var maskBytes = mask.EncodeToPNG();

            List<IMultipartFormSection> formParts = new List<IMultipartFormSection>();
            formParts.Add(new MultipartFormDataSection("response_format", response_format));
            formParts.Add(new MultipartFormDataSection("size", imageSize));
            formParts.Add(new MultipartFormFileSection("image", imageBytes, "image.png", "image/png"));
            formParts.Add(new MultipartFormDataSection("prompt", prompt));
            formParts.Add(new MultipartFormFileSection("mask", maskBytes, "mask.png", "image/png"));
            if (n > 1) formParts.Add(new MultipartFormDataSection("n", n.ToString()));
            if (!string.IsNullOrEmpty(user)) formParts.Add(new MultipartFormDataSection("user", user));

            var webRequest = UnityWebRequest.Post(ImageEditsURL, formParts);

            webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;

                var result = new List<string>();

                if (!string.IsNullOrEmpty(text))
                {
                    var responseData = JsonUtility.FromJson<ImagesResponse>(text);
                    foreach (var imageResponse in responseData.data)
                    {
                        result.Add((response_format == ResponseFormatB64JSON) ? imageResponse.b64_json : imageResponse.url);
                    }
                }
                callback?.Invoke(result);
            };

            return asyncOp;
        }

        #endregion

        #region Audio

        public static string TTSModelToString(TTSModel model)
        {
            switch (model)
            {
                default:
                    return TTSModel1;
                case TTSModel.TTSModel1HD:
                    return TTSModel1HD;
            }
        }

        public static string VoiceOutputFormatToString(VoiceOutputFormat format)
        {
            return format.ToString().ToLower();
        }

        public static AudioType VoiceOutputFormatToAudioType(VoiceOutputFormat format)
        {
            switch (format)
            {
                default:
                    return AudioType.WAV;
                case VoiceOutputFormat.MP3:
                    return AudioType.MPEG;
            }
        }

        /// <summary>
        /// Transcribes audio into the input language.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="audioClip">Audio clip to transcribe.</param>
        /// <param name="prompt">An optional text to guide the model's style or continue a previous audio segment. The prompt should match the audio language.</param>
        /// <param name="responseFormat">The format of the transcript output, in one of these options: json, text, srt, verbose_json, or vtt.</param>
        /// <param name="temperature">The sampling temperature, between 0 and 1. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. If set to 0, the model will use log probability to automatically increase the temperature until certain thresholds are hit.</param>
        /// <param name="language">(Optional) The language of the input audio. Supplying the input language in ISO-639-1 format will improve accuracy and latency.</param>
        /// <param name="callback">The value returned by the API call.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation SubmitAudioTranscriptionAsync(string apiKey, 
            AudioClip audioClip, string prompt, AudioResponseFormat responseFormat,
            float temperature, string language, Action<string> callback)
        {
            const string model = "whisper-1";

            List<IMultipartFormSection> formParts = new List<IMultipartFormSection>();
            using (MemoryStream stream = new MemoryStream())
            {
                SavWav.ConvertAndWrite(stream, audioClip);
                SavWav.WriteHeader(stream, audioClip);
                var bytes = stream.ToArray();
                formParts.Add(new MultipartFormFileSection("file", bytes, "audio.wav", "audio/wav"));
            }
            formParts.Add(new MultipartFormDataSection("model", model));
            if (!string.IsNullOrEmpty(prompt))
            {
                formParts.Add(new MultipartFormDataSection("prompt", prompt));
            }
            formParts.Add(new MultipartFormDataSection("response_format", responseFormat.ToString().ToLowerInvariant()));
            formParts.Add(new MultipartFormDataSection("temperature", temperature.ToString()));
            if (!string.IsNullOrEmpty(language))
            {
                formParts.Add(new MultipartFormDataSection("language", language));
            }

            var webRequest = UnityWebRequest.Post(AudioTranscriptionsURL, formParts);

            webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                var text = success ? webRequest.downloadHandler.text : string.Empty;
                if (!success) Debug.Log($"{webRequest.error}\n{webRequest.downloadHandler.text}");
                webRequest.Dispose();
                webRequest = null;
                if (!string.IsNullOrEmpty(text))
                {
                    var responseData = JsonUtility.FromJson<AudioTranscriptionResponse>(text);
                    text = responseData.text.Trim();
                }
                else
                {
                    text = string.Empty;
                }
                callback?.Invoke(text);
            };

            return asyncOp;
        }

        /// <summary>
        /// Generates voice acting audio from text. A DownloadHandlerAudioClip bug in some 
        /// Unity versions makes it unable to handle some WAV files. This method variant
        /// returns the raw bytes of the generated audio in case the AudioClip isn't valid.
        /// If DownloadHandlerAudioClip wasn't able to load the audio clip, the length will be 0.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">TTS model to use.</param>
        /// <param name="voice">Voice actor to use.</param>
        /// <param name="outputFormat">Audio clip output format.</param>
        /// <param name="speed">Speed from 0.25 to 4.0.</param>
        /// <param name="input">Text to generate voice acting from.</param>
        /// <param name="callback">The audio clip and bytes returned by the API call, or null on failure.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation SubmitVoiceGenerationAsync(string apiKey, 
            TTSModel model, Voices voice, VoiceOutputFormat outputFormat, float speed, string input,
            Action<AudioClip, byte[]> callback)
        {
            var voiceRequest = new AudioSpeechRequest(TTSModelToString(model), input, 
                voice.ToString().ToLower(), VoiceOutputFormatToString(outputFormat), speed);
            string jsonData = JsonUtility.ToJson(voiceRequest);

            UnityWebRequest webRequest = WebRequestUtility.CreateWebRequest(apiKey, AudioSpeechURL, jsonData);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                if (!success) Debug.Log(webRequest.error);

                if (success)
                {
                    var bytes = webRequest.downloadHandler.data;
                    var ext = VoiceOutputFormatToString(outputFormat);
                    var filePath = Path.Combine(Application.persistentDataPath, $"audio.{ext}");
                    filePath = filePath.Replace("/", "\\");
                    File.WriteAllBytes(filePath, bytes);
                    var url = "file://" + filePath.Replace("\\", "/");
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, VoiceOutputFormatToAudioType(outputFormat));
                    var wwwOp = www.SendWebRequest();
                    wwwOp.completed += (op2) =>
                    {
                        if (www.result == UnityWebRequest.Result.Success)
                        {
                            var audioClip = DownloadHandlerAudioClip.GetContent(www);
                            callback?.Invoke(audioClip, bytes);
                        }
                        else
                        {
                            callback?.Invoke(null, bytes);
                        }
                        File.Delete(filePath);
                        www.Dispose();
                        www = null;
                    };
                }
                else
                {
                    callback?.Invoke(null, null);
                }
                webRequest.Dispose();
                webRequest = null;
            };

            return asyncOp;
        }

        /// <summary>
        /// Generates voice acting audio from text.
        /// </summary>
        /// <param name="apiKey">OpenAI API key.</param>
        /// <param name="model">TTS model to use.</param>
        /// <param name="voice">Voice actor to use.</param>
        /// <param name="outputFormat">Audio clip output format.</param>
        /// <param name="speed">Speed from 0.25 to 4.0.</param>
        /// <param name="input">Text to generate voice acting from.</param>
        /// <param name="callback">The audio clip returned by the API call, or null on failure.</param>
        /// <returns></returns>
        public static UnityWebRequestAsyncOperation SubmitVoiceGenerationAsync(string apiKey,
            TTSModel model, Voices voice, VoiceOutputFormat outputFormat, float speed, string input,
            Action<AudioClip> callback)
        {
            var voiceRequest = new AudioSpeechRequest(TTSModelToString(model), input,
                voice.ToString().ToLower(), VoiceOutputFormatToString(outputFormat), speed);
            string jsonData = JsonUtility.ToJson(voiceRequest);

            UnityWebRequest webRequest = WebRequestUtility.CreateWebRequest(apiKey, AudioSpeechURL, jsonData);
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                if (!success) Debug.Log(webRequest.error);

                if (success)
                {
                    var bytes = webRequest.downloadHandler.data;
                    var ext = VoiceOutputFormatToString(outputFormat);
                    var filePath = Path.Combine(Application.persistentDataPath, $"audio.{ext}");
                    filePath = filePath.Replace("/", "\\");
                    File.WriteAllBytes(filePath, bytes);
                    var url = "file://" + filePath.Replace("\\", "/");
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, VoiceOutputFormatToAudioType(outputFormat));
                    var wwwOp = www.SendWebRequest();
                    wwwOp.completed += (op2) =>
                    {
                        if (www.result == UnityWebRequest.Result.Success)
                        {
                            var audioClip = DownloadHandlerAudioClip.GetContent(www);
                            callback?.Invoke(audioClip);
                        }
                        else
                        {
                            callback?.Invoke(null);
                        }
                        File.Delete(filePath);
                        www.Dispose();
                        www = null;
                    };
                }
                else
                {
                    callback?.Invoke(null);
                }
                webRequest.Dispose();
                webRequest = null;
            };

            return asyncOp;
        }

        #endregion

    }

}

#endif
