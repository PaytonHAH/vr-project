// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Dialogue System Addon for OpenAI assistant window.
    /// </summary>
    [InitializeOnLoad]
    public class AssistantWindow : EditorWindow
    {

        private static AssistantWindow instance;

        [MenuItem("Tools/Pixel Crushers/Dialogue System/Addon for OpenAI/Assistant Window", false, 2)]
        public static void Open()
        {
            instance = GetWindow<AssistantWindow>(false, "AI Assistant");
            instance.minSize = new Vector2(350, 200);
        }

        private const string PromptControlName = "AssistantPrompt";
        private const float ChatHistoryWidth = 128;
        //private static string[] GPTToolbarContents = new string[] { "GPT-3.5", "GPT-4" };
        private GUIContent TemperatureLabel = new GUIContent("Temperature", "Randomness, where 0=predictable, 1=very random.");
        private GUIContent MaxTokensLabel = new GUIContent("Max Tokens", "Max tokens to spend on request. Fewer tokens will result in shorter responses.");
        private int BottomTokenRange = 128;

        private Vector2 chatScrollPosition = Vector2.zero;
        private Vector2 historyScrollPosition = Vector2.zero;
        //private int gptToolbarIndex = 0;
        private TextModelName selectedModel = TextModelName.GPT_4o_mini;
        private float temperature = 0.5f;
        private int maxTokens = 1024;
        private List<Chat> chats = new List<Chat>();
        private Chat currentChat = null;
        private string prompt;
        private bool isAwaitingReply = false;
        private bool mustScrollToBottom = false;
        protected List<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

        private class Chat
        {
            public string name;
            public List<string> history;
        }

        private void OnDisable()
        {
            instance = null;
        }

        private void OnGUI()
        {
            DrawChats();
            DrawHistory();
            DrawPrompt();
        }

        private string GetAPIKey()
        {
            return EditorPrefs.GetString(DialogueSystemOpenAIWindow.OpenAIKey);
        }

        private Model GetModel()
        {
            //return (gptToolbarIndex == 1) ? Model.GPT4_32K : Model.GPT3_5_Turbo_16K;
            return OpenAI.NameToModel(selectedModel);
        }

        private int GetTopTokenRange(Model model)
        {
            return model.MaxTokens;
            //return (gptToolbarIndex == 1) ? 32768 : 16384;
        }

        private void DrawChats()
        {
            GUILayout.BeginArea(new Rect(0, 0, ChatHistoryWidth, position.height), EditorStyles.helpBox);

            //gptToolbarIndex = GUILayout.Toolbar(gptToolbarIndex, GPTToolbarContents);
            selectedModel = (TextModelName)EditorGUILayout.EnumPopup(selectedModel);
            EditorGUILayout.LabelField(TemperatureLabel);
            temperature = EditorGUILayout.Slider(temperature, 0, 1);
            EditorGUILayout.LabelField(MaxTokensLabel);
            maxTokens = EditorGUILayout.IntSlider(maxTokens, BottomTokenRange, GetTopTokenRange(GetModel()));
            chatScrollPosition = EditorGUILayout.BeginScrollView(chatScrollPosition);
            EditorGUI.BeginDisabledGroup(isAwaitingReply);
            if (GUILayout.Button("New Chat"))
            {
                currentChat = null;
                ResetPrompt();
            }
            foreach (var chat in chats)
            {
                if (GUILayout.Button(chat.name))
                {
                    currentChat = chat;
                    ResetPrompt();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawHistory()
        {
            var historyWidth = position.width - ChatHistoryWidth;
            GUILayout.BeginArea(new Rect(ChatHistoryWidth, 0, position.width - ChatHistoryWidth, position.height - EditorGUIUtility.singleLineHeight - 4), EditorStyles.helpBox);
            historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition);
            float height = 0;
            if (currentChat != null)
            {
                foreach (var line in currentChat.history)
                {
                    if (mustScrollToBottom) height += EditorStyles.textArea.CalcHeight(new GUIContent(line), historyWidth);
                    GUILayout.TextArea(line);
                }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            if (mustScrollToBottom)
            {
                mustScrollToBottom = false;
                historyScrollPosition.y = height;
                Repaint();
            }
        }

        private void DrawPrompt()
        {
            var evt = UnityEngine.Event.current;
            if (!isAwaitingReply && evt.isKey && evt.type == EventType.KeyUp && evt.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == PromptControlName)
            {
                evt.Use();
                Submit();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(isAwaitingReply);
                GUI.SetNextControlName(PromptControlName);
                prompt = EditorGUI.TextField(new Rect(ChatHistoryWidth, position.height - EditorGUIUtility.singleLineHeight - 2, position.width - ChatHistoryWidth, EditorGUIUtility.singleLineHeight), prompt);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void ResetPrompt()
        {
            prompt = string.Empty;
            GUI.FocusControl(PromptControlName);
        }

        private void Submit()
        {
            if (isAwaitingReply) return;
            if (currentChat == null)
            {
                currentChat = new Chat();
                currentChat.name = prompt.Substring(0, Mathf.Min(20, prompt.Length));
                currentChat.history = new List<string> { prompt.Trim() };
                chats.Add(currentChat);
                SubmitPrompt(prompt, string.Empty);
            }
            else
            {
                currentChat.history.Add(prompt.Trim());
                SubmitEdit(prompt);
            }
        }

        protected void SubmitPrompt(string userPrompt, string assistantPrompt, string progressTitle = "Contacting OpenAI",
            float progress = 0.5f, bool debug = true)
        {
            var prompt = string.IsNullOrEmpty(assistantPrompt) ? userPrompt : $"{userPrompt} {assistantPrompt}";
            if (debug) Debug.Log($"Sending to OpenAI ({selectedModel}): {prompt}");
            try
            {
                isAwaitingReply = true;
                Messages.Clear();
                if (!string.IsNullOrEmpty(userPrompt)) Messages.Add(new ChatMessage("user", userPrompt));
                if (!string.IsNullOrEmpty(assistantPrompt)) Messages.Add(new ChatMessage("assistant", assistantPrompt));
                OpenAI.SubmitChatAsync(GetAPIKey(), GetModel(), temperature, maxTokens, Messages, OnReceivedResult);
            }
            catch (System.Exception)
            {
                isAwaitingReply = false;
            }
        }

        protected void SubmitEdit(string prompt, string progressTitle = "Revising Text",
            float progress = 0.5f, bool debug = true)
        {
            if (debug) Debug.Log($"Sending to OpenAI: {prompt}");
            try
            {
                isAwaitingReply = true;
                Messages.Clear();
                foreach (var line in currentChat.history)
                {
                    Messages.Add(new ChatMessage("user", line));
                }
                Messages.Add(new ChatMessage("user", prompt));
                OpenAI.SubmitChatAsync(GetAPIKey(), GetModel(), temperature, maxTokens, Messages, OnReceivedResult);
            }
            catch (System.Exception)
            {
                isAwaitingReply = false;
            }
        }

        private void OnReceivedResult(string result)
        {
            isAwaitingReply = false;
            currentChat.history.Add(result.Trim());
            ResetPrompt();
            mustScrollToBottom = true;
            Repaint();
        }

    }

}

#endif
