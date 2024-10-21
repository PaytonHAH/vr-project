// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel that provides general-purpose freeform communication with OpenAI.
    /// </summary>
    public class FreeformChatPanel : TextGenerationPanel
    {

        private string prompt;
        private bool isEdit = false;

        private static GUIContent HeadingLabel = new GUIContent("Freeform Chat");
        private static GUIContent PromptLabel = new GUIContent("Prompt", "Query to send to OpenAI.");
        private static GUIContent SubmitButtonLabel = new GUIContent("Submit", "Submit query.");

        public FreeformChatPanel(string apiKey, DialogueDatabase database)
            : base(apiKey, database, null, null, null)
        {
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Talk with OpenAI here, similarly to how ChatGPT works.");
            DrawModelSettings();
            DrawPrompt();
            DrawResult();
        }

        private void DrawPrompt()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            var guiEvent = UnityEngine.Event.current;
            if (!IsAwaitingReply && guiEvent.isKey && guiEvent.type == EventType.KeyUp && 
                guiEvent.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == "FreeformPrompt")
            {
                guiEvent.Use();
                Submit();
            }
            else
            {
                GUI.SetNextControlName("FreeformPrompt");
                prompt = EditorGUILayout.TextField(PromptLabel, prompt);
            }

            var size = GUI.skin.button.CalcSize(SubmitButtonLabel);
            EditorGUI.BeginDisabledGroup(IsAwaitingReply);
            if (GUILayout.Button("Submit", GUILayout.Width(size.x)))
            {
                if (!IsAwaitingReply)
                {
                    Submit();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            DrawStatus();
        }

        private void DrawResult()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Result:");
            EditorGUI.BeginDisabledGroup(true);
            var outputHeight = string.IsNullOrEmpty(ResultText) ? 3 * EditorGUIUtility.singleLineHeight
                : GUI.skin.textArea.CalcHeight(new GUIContent(ResultText), DialogueSystemOpenAIWindow.Instance.position.width) +
                2 * EditorGUIUtility.singleLineHeight;
            outputHeight = Mathf.Max(outputHeight, 100f);
            EditorGUILayout.TextArea(ResultText, readOnlyTextAreaStyle, GUILayout.Height(outputHeight));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = ResultText;
            }
            if (GUILayout.Button("Close"))
            {
                DialogueSystemOpenAIWindow.Instance.Close();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Submit()
        {
            if (IsAwaitingReply) return;
            if (!isEdit)
            {
                SubmitPrompt(prompt, string.Empty);
                isEdit = true;
            }
            else
            {
                SubmitEdit(prompt);
            }
        }

    }
}

#endif
