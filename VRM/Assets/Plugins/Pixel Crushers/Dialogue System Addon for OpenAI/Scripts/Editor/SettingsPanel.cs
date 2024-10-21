// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// OpenAI Addon panel that shows settings such as API key configuration.
    /// </summary>
    public class SettingsPanel : BasePanel
    {

        private string key;

        public SettingsPanel(string apiKey, DialogueDatabase database)
            : base(apiKey, database, null, null, null)
        {
            key = EditorPrefs.GetString(DialogueSystemOpenAIWindow.OpenAIKey);
        }

        public override void Draw()
        {
            base.Draw();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Configure OpenAI Access", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The Dialogue System Addon for OpenAI will use your personal OpenAI API key. " +
                "You are responsible for any usage charges that OpenAI applies to your API key.", labelWordWrappedStyle);
            if (GUILayout.Button("OpenAI Pricing", labelHyperlinkStyle))
            {
                Application.OpenURL("https://openai.com/api/pricing/");
            }
            EditorGUILayout.LabelField("If you don't have an OpenAI API key, click here to create one:", labelWordWrappedStyle);
            if (GUILayout.Button("Create OpenAI API Key"))
            {
                Application.OpenURL("https://platform.openai.com/account/api-keys");
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            key = EditorGUILayout.TextField("Open API Key", key);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(key) || !key.StartsWith("sk-"));
            var connectButtonWidth = GUI.skin.button.CalcSize(new GUIContent("Connect")).x;
            if (GUILayout.Button("Connect", GUILayout.Width(connectButtonWidth)))
            {
                EditorPrefs.SetString(DialogueSystemOpenAIWindow.OpenAIKey, key);
                DialogueSystemOpenAIWindow.Open(AIRequestType.OriginalRequest, database, asset, entry, field);
            }
            EditorGUILayout.EndHorizontal();
        }

    }
}

#endif
