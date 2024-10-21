// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Base panel type for DialogueSystemOpenAIWindow.
    /// </summary>
    public abstract class BasePanel
    {

        protected string apiKey;
        protected DialogueDatabase database;
        protected Asset asset;
        protected DialogueEntry entry;
        protected Field field;

        protected GUIStyle labelWordWrappedStyle;
        protected GUIStyle labelHyperlinkStyle;
        protected GUIStyle readOnlyTextFieldStyle;
        protected GUIStyle readOnlyTextAreaStyle;
        private GUIStyle headingStyle;

        protected bool IsAwaitingReply { get; set; } = false;
        protected string ProgressText { get; set; } = "Retrieving response from OpenAI API. Please wait...";
        protected bool LogDebug { get; set; }

        private static GUIContent BackButtonLabel = new GUIContent("Main", "Return to main panel.");

        public BasePanel(string apiKey, DialogueDatabase database, Asset asset, DialogueEntry entry, Field field)
        {
            this.apiKey = apiKey;
            this.database = database;
            this.asset = asset;
            this.entry = entry;
            this.field = field;
        }

        public virtual void Draw()
        {
            CreateGUIStyles();

            EditorGUILayout.LabelField("Dialogue System Addon for OpenAI", headingStyle);
            EditorGUILayout.Space();

            var windowPosition = DialogueSystemOpenAIWindow.Instance.position;
            var gearPosition = new Rect(windowPosition.width - MoreEditorGuiUtility.GearWidth - 4, 4, MoreEditorGuiUtility.GearWidth, MoreEditorGuiUtility.GearHeight + 2);
            if (MoreEditorGuiUtility.DoGearMenu(gearPosition))
            {
                WelcomeWindow.Open();
            }
        }

        protected virtual void DrawHeading(GUIContent headingLabel, string helpText)
        {
            EditorGUILayout.BeginHorizontal();
            if (DrawBackButton()) return;
            EditorGUI.BeginDisabledGroup(true);
            var size = EditorStyles.miniButton.CalcSize(headingLabel);
            GUILayout.Button(headingLabel, EditorStyles.miniButton, GUILayout.Width(size.x));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(headingLabel, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(helpText, MessageType.None);
        }

        protected virtual void DrawStatus()
        {
            if (IsAwaitingReply)
            {
                EditorGUILayout.HelpBox(ProgressText, MessageType.Info);
            }
        }

        private void CreateGUIStyles()
        {
            if (labelWordWrappedStyle == null)
            {
                labelWordWrappedStyle = new GUIStyle(GUI.skin.label);
                labelWordWrappedStyle.wordWrap = true;
            }
            if (labelHyperlinkStyle == null)
            {
                labelHyperlinkStyle = new GUIStyle(GUI.skin.label);
                labelHyperlinkStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue;
            }
            if (readOnlyTextFieldStyle == null)
            {
                readOnlyTextFieldStyle = new GUIStyle(EditorStyles.textArea);
                readOnlyTextFieldStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
            if (readOnlyTextAreaStyle == null)
            {
                readOnlyTextAreaStyle = new GUIStyle(EditorStyles.textArea);
                readOnlyTextAreaStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
            if (headingStyle == null)
            {
                headingStyle = new GUIStyle(GUI.skin.label);
                headingStyle.fontStyle = FontStyle.Bold;
                headingStyle.fontSize = 16;
            }
        }

        protected bool DrawBackButton()
        {
            var size = EditorStyles.miniButton.CalcSize(BackButtonLabel);
            if (GUILayout.Button(BackButtonLabel, EditorStyles.miniButton, GUILayout.Width(size.x)))
            {
                DialogueSystemOpenAIWindow.Open(AIRequestType.General, database, asset, entry, field);
                GUIUtility.ExitGUI();
                return true;
            }
            return false;
        }

        public void Repaint()
        {
            if (DialogueSystemOpenAIWindow.Instance == null) return;
            DialogueSystemOpenAIWindow.Instance.Repaint();
        }

    }
}

#endif
