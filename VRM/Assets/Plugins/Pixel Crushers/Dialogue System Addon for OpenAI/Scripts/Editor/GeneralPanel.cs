// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// General info panel that appears after reloading assemblies (e.g., changing play mode) or
    /// after logging into OpenAI API from ApiConfigurationPanel.
    /// </summary>
    public class GeneralPanel : BasePanel
    {

        public GeneralPanel(string apiKey, DialogueDatabase database)
            : base(apiKey, database, null, null, null)
        {
        }

        public override void Draw()
        {
            base.Draw();
            EditorGUILayout.LabelField("The Dialogue System Addon for OpenAI will use your personal OpenAI API key. " +
                "You are responsible for any usage charges that OpenAI applies to your API key.", labelWordWrappedStyle);
            if (GUILayout.Button("OpenAI Pricing", labelHyperlinkStyle))
            {
                Application.OpenURL("https://openai.com/api/pricing/");
            }

            EditorGUILayout.Space();
            database = EditorGUILayout.ObjectField("Database", database, typeof(DialogueDatabase), false) as DialogueDatabase;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(database == null);
            if (GUILayout.Button("Generate Conversation", GUILayout.Width(200)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.GenerateConversation, database, asset, entry, field);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField("Generate dialogue or barks.");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(database == null);
            if (GUILayout.Button("Translate Database", GUILayout.Width(200)))
            {
                DialogueEditorWindow.OpenDialogueEditorWindow();
                DialogueEditorWindow.inspectorSelection = database;
                DialogueEditorWindow.instance.OpenTranslateDatabasePanel(); // Need this method to get language list.
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField("Translate all database content.");
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            //EditorGUI.BeginDisabledGroup(textTable == null);
            if (GUILayout.Button("Translate Text Table", GUILayout.Width(200)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.LocalizeTextTable, null, null, null, null);
            }
            //EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField("Translate text table.");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Freeform Chat", GUILayout.Width(200)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.Freeform, database, asset, entry, field);
            }
            EditorGUILayout.LabelField("Talk with OpenAI.");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fine-Tuned Models", GUILayout.Width(200)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.FineTunedModels, database, asset, entry, field);
            }
            EditorGUILayout.LabelField("Add your fine-tuned models.");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Other Actions", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("To perform other actions, click the Dialogue Editor window's 'AI' buttons:\n" +
                "· Actor, Quests/Items, Locations: Revise or translate a field.\n" +
                "· Dialogue entry node: Revise or translate a field.\n" +
                "· Conversation dropdown: Generate a new conversation.\n" +
                "· Database > OpenAI Addon: Generate conversation, translate database, or freeform query.",
                labelWordWrappedStyle);

            //EditorGUILayout.Space();
            //EditorGUILayout.LabelField("Editor Note", EditorStyles.boldLabel);
            //EditorGUILayout.LabelField("Some versions of Unity 2021 contain a bug that intermittently results in a small " +
            //    "editor-only memory leak during web requests, such as to OpenAI. This is an editor-only issue and does " +
            //    "NOT occur at runtime or in builds. Newer versions of Unity have fixed this small bug.",
            //    labelWordWrappedStyle);
        }
    }
}

#endif
