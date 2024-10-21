// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.OpenAIAddon;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// The Dialogue Editor window calls implementations of DrawAI* methods.
    /// This version provides implementations for the OpenAI addon.
    /// </summary>
    public partial class DialogueEditorWindow
    {

        private static GUIContent GeneratePortraitsGUIContent = new GUIContent("Generate AI Portraits", "Generate portrait images using OpenAI");
        private static GUIContent VoiceFieldGUIContent = new GUIContent("Voice", "AI voice actor to use for text to speech.");

        private bool DrawAIDatabaseFoldout(bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, new GUIContent("OpenAI Addon", "Addon for OpenAI features."));
            if (foldout)
            {
                if (GUILayout.Button("General"))
                {
                    DialogueSystemOpenAIWindow.Open(AIRequestType.General, database, null, null, null);
                }
                if (GUILayout.Button("Generate Conversation"))
                {
                    DialogueSystemOpenAIWindow.Open(AIRequestType.GenerateConversation, database, null, null, null);
                }
                if (GUILayout.Button("Translate Database"))
                {
                    OpenTranslateDatabasePanel();
                }
                if (GUILayout.Button("Freeform Chat"))
                {
                    DialogueSystemOpenAIWindow.Open(AIRequestType.Freeform, database, null, null, null);
                }
            }
            return foldout;
        }

        public void OpenTranslateDatabasePanel()
        {
            FindLanguagesForLocalizationExportImport();
            DialogueSystemOpenAIWindow.Open(AIRequestType.LocalizeDatabase, database, null, null, null);
            DialogueSystemOpenAIWindow.Instance.SetLocalizationLanguages(localizationLanguages.languages);
        }

        private void DrawAIGenerateConversationButton()
        {
            if (GUILayout.Button(new GUIContent("AI", "Generate new conversation using OpenAI"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.GenerateConversation, database, null, null, null);
            }
        }

        private void DrawAIReviseTextButton(Asset asset, DialogueEntry entry, Field field)
        {
            if (GUILayout.Button(new GUIContent("AI", "Revise text using OpenAI"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.ReviseText, database, asset, entry, field);
            }
        }

        public void DrawAIBranchingConversationButton(Conversation conversation)
        {
            if (GUILayout.Button(new GUIContent("AI", "Generate dialogue text options for this conversation."), EditorStyles.miniButtonRight, GUILayout.Width(21)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.BranchingDialogue, database, conversation, null, null);
            }
        }

        private void DrawAILocalizeTextButton(Asset asset, DialogueEntry entry, Field field)
        {
            if (GUILayout.Button(new GUIContent("AI", "Localize text or generate voiceover audio."), EditorStyles.toolbarPopup, GUILayout.Width(36)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Translate", "Localize text using OpenAI"), false,
                    () => { DialogueSystemOpenAIWindow.Open(AIRequestType.LocalizeField, database, asset, entry, field); });
                menu.AddItem(new GUIContent("Voiceover", "Generate voiceover audio"), false,
                    () => { DialogueSystemOpenAIWindow.Open(AIRequestType.GenerateVoice, database, null, entry, field); });
                menu.ShowAsContext();
            }
        }

        private void DrawAIPortraitSprites(Asset asset)
        {
            var size = GUI.skin.button.CalcSize(GeneratePortraitsGUIContent);
            if (GUILayout.Button(GeneratePortraitsGUIContent, EditorStyles.miniButtonRight, GUILayout.Width(size.x)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.GeneratePortraits, database, asset, null, null);
            }
        }

        public void DrawAIVoiceSelection(Actor actor)
        {
            var size = GUI.skin.button.CalcSize(VoiceFieldGUIContent);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(VoiceFieldGUIContent, actor.LookupValue(DialogueSystemFields.Voice));
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(new GUIContent("Select", "Assign an ElevenLabs AI voice actor"), EditorStyles.miniButtonRight, GUILayout.Width(size.x)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.SelectVoice, database, actor, null, null);
            }
            EditorGUILayout.EndHorizontal();
        }

        public void DrawAISequence(DialogueEntry entry, Field field)
        {
            if (GUILayout.Button(new GUIContent("AI", "Generate text to speech using ElevenLabs"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.GenerateVoice, database, null, entry, field);
            }
        }

    }
}

#endif
