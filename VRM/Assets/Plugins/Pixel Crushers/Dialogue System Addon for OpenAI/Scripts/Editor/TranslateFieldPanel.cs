// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to translate a single field.
    /// </summary>
    public class TranslateFieldPanel : TextGenerationPanel
    {

        private string mainFieldTitle;
        private string mainFieldText;
        private string language;
        private string refinementPrompt;
        private string label = null;

        private static GUIContent HeadingLabel = new GUIContent("Translate Field");
        private static GUIContent RefineLabel = new GUIContent("Refinement Instructions:", "(Optional) Refine translation using this prompt.");
        private static GUIContent RefineButtonLabel = new GUIContent("Translate", "Refine translation using this prompt.");

        public TranslateFieldPanel(string apiKey, DialogueDatabase database,
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
            SetModelByName(TextModelName.GPT3_5_Turbo_16K);
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Translate a text field.");
            DrawModelSettings();
            DrawExistingField();
            DrawPrompt();
            DrawResult();
        }

        private void DrawExistingField()
        {
            if (label == null)
            {
                if (IsDialogueEntry && !field.title.Contains(" "))
                {
                    // Dialogue Text localization:
                    mainFieldTitle = "Dialogue Text";
                    mainFieldText = entry.DialogueText;
                    language = AITextUtility.DetermineLanguage(field.title);
                }
                else
                {
                    var rIndex = field.title.LastIndexOf(' ');
                    mainFieldTitle = field.title.Substring(0, rIndex);
                    mainFieldText = IsDialogueEntry
                        ? Field.LookupValue(entry.fields, mainFieldTitle)
                        : asset.LookupValue(mainFieldTitle);
                    var code = field.title.Substring(rIndex + 1);
                    language = AITextUtility.DetermineLanguage(code);
                }
                label = (IsDialogueEntry ? "Dialogue Entry" : asset.Name) + ": " + mainFieldTitle;
            }
            EditorGUILayout.LabelField(label);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(mainFieldText, readOnlyTextAreaStyle);
            language = EditorGUILayout.TextField("Translate To", language, readOnlyTextFieldStyle);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPrompt()
        {
            EditorGUILayout.BeginHorizontal();
            refinementPrompt = EditorGUILayout.TextField(RefineLabel, refinementPrompt);
            var size = GUI.skin.button.CalcSize(RefineButtonLabel);
            if (GUILayout.Button(RefineButtonLabel, GUILayout.Width(size.x)))
            {
                TranslateText(apiKey);
            }
            EditorGUILayout.EndHorizontal();
            DrawStatus();
        }

        private void DrawResult()
        {
            EditorGUILayout.LabelField("Proposed Translation:");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(ResultText, readOnlyTextAreaStyle);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(ResultText));
            if (GUILayout.Button("Accept"))
            {
                Undo.RecordObject(database, "Revise Text");
                field.value = AITextUtility.RemoveSurroundingQuotes(ResultText);
                EditorUtility.SetDirty(database);
                DialogueEditorWindow.instance?.Reset();
                if (IsDialogueEntry && entry != null)
                {
                    DialogueEditorWindow.OpenDialogueEntry(database, entry.conversationID, entry.id);
                }
                DialogueEditorWindow.instance?.Repaint();
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel"))
            {
                DialogueSystemOpenAIWindow.Instance.Close();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void TranslateText(string apiKey)
        {
            var prompt = $"Translate this text to {language}: \"{AITextUtility.DoubleQuotesToSingle(mainFieldText)}\". {refinementPrompt}";
            SubmitPrompt(prompt, AssistantPrompt);
        }

    }
}

#endif
