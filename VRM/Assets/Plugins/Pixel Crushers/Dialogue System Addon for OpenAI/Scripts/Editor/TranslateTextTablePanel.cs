// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to translate the entire text table.
    /// </summary>
    public class TranslateTextTablePanel : TextGenerationPanel
    {

        private class TranslationRequest
        {
            public TextTableField field;
            public int languageID;
            public string languageName;
            public string prompt;
            public string originalText;
            public float progress;

            public TranslationRequest(TextTableField field, int languageID, string languageName, string prompt, string originalText, float progress)
            {
                this.field = field;
                this.languageID = languageID;
                this.languageName = languageName;
                this.prompt = prompt;
                this.originalText = originalText;
                this.progress = progress;
            }
        }

        private TextTable textTable;
        private List<string> languages = new List<string>();
        private List<string> languageNames = new List<string>();
        private List<bool> translate = new List<bool>();
        private Queue<TranslationRequest> translationRequestQueue = new Queue<TranslationRequest>();
        private bool retranslateAll = false;
        private bool cancel = false;
        private float timeNextRequestAllowed;
        private TextTableField currentField;
        private int currentLanguageID;
        private bool needToRefreshLanguages = true;

        private static GUIContent HeadingLabel = new GUIContent("Translate Text Table");
        private static GUIContent RetranslateAllLabel = new GUIContent("Retranslate All", "Translate all fields, including nonblank fields. Untick to only translate blank fields.");
        private static GUIContent DebugLabel = new GUIContent("Debug", "Log prompts and return values to Console.");

        public TranslateTextTablePanel(string apiKey, TextTable textTable)
            : base(apiKey, null, null, null, null)
        {
            this.textTable = textTable;
            if (textTable == null)
            {
                var lastGuid = EditorPrefs.GetString(DialogueSystemOpenAIWindow.TextTableAssetGuid);
                if (!string.IsNullOrEmpty(lastGuid))
                { 
                    var assetPath = AssetDatabase.GUIDToAssetPath(lastGuid);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        this.textTable = AssetDatabase.LoadAssetAtPath<TextTable>(assetPath);
                    }
                }
            }
            needToRefreshLanguages = true;
            SetModelByName(TextModelName.GPT3_5_Turbo_16K);
        }

        private void RefreshLanguages()
        {
            if (textTable == null) return;
            SetLanguages(new List<string>(textTable.languages.Keys));
        }

        public void SetLanguages(List<string> languages)
        {
            this.languages = languages;
            languages.RemoveAll(x => IsDefaultLanguage(x));
            languageNames = new List<string>();
            translate = new List<bool>();
            for (int i = 0; i < languages.Count; i++)
            {
                languageNames.Add(AITextUtility.DetermineLanguage(languages[i]));
                translate.Add(true);
            }
        }

        private bool IsDefaultLanguage(string language)
        {
            return string.IsNullOrEmpty(language) || language == "Default";
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "This will translate the selected languages in the text table.");

            EditorGUI.BeginChangeCheck();
            textTable = EditorGUILayout.ObjectField("Text Table", textTable, typeof(TextTable), false) as TextTable;
            if (EditorGUI.EndChangeCheck())
            {
                needToRefreshLanguages = true;
                var guid = (textTable == null) ? string.Empty
                    : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textTable));
                EditorPrefs.SetString(DialogueSystemOpenAIWindow.TextTableAssetGuid, guid);
            }

            DrawModelSettings();
            LogDebug = EditorGUILayout.Toggle(DebugLabel, LogDebug);

            EditorGUILayout.Space();
            retranslateAll = EditorGUILayout.Toggle(RetranslateAllLabel, retranslateAll);

            if (needToRefreshLanguages)
            {
                needToRefreshLanguages = false;
                RefreshLanguages(); 
            }
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Languages:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh")) RefreshLanguages();
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < languageNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                translate[i] = EditorGUILayout.ToggleLeft(languages[i], translate[i]);
                languageNames[i] = EditorGUILayout.TextField(languageNames[i]);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(languages.Count == 0 || textTable == null);
            if (IsAwaitingReply)
            {
                if (GUILayout.Button("Stop"))
                {
                    CancelTranslation();
                }
            }
            else
            {
                if (GUILayout.Button("Translate"))
                {
                    if (EditorUtility.DisplayDialog("Translate Text Table",
                        "This operation may take a long time to complete. Proceed?", "OK", "Cancel"))
                    {
                        TranslateTextTable();
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Cancel"))
            {
                CancelTranslation();
                DialogueSystemOpenAIWindow.Instance.Close();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            DrawStatus();
        }

        private void TranslateTextTable()
        {
            cancel = false;
            timeNextRequestAllowed = 0;
            IsAwaitingReply = false;

            int numFields = textTable.fields.Count;
            var numLanguagesToTranslate = translate.FindAll(x => x == true).Count;
            numFields *= numLanguagesToTranslate;
            int currentFieldNum = 0;

            for (int i = 0; i < languageNames.Count; i++)
            {
                if (cancel) return;
                if (translate[i])
                {
                    var languageID = textTable.GetLanguageID(languages[i]);
                    if (languageID == 0) continue;
                    TranslateToLanguage(languageID, languageNames[i], numFields, ref currentFieldNum);
                }
            }
        }

        private void TranslateToLanguage(int languageID, string languageName, int numFields, ref int currentFieldNum)
        {
            foreach (var field in textTable.fields.Values)
            { 
                float progress = (float)currentFieldNum++ / (float)numFields;
                TranslateField(field, languageID, languageName, progress);
            }
        }

        private void TranslateField(TextTableField field, int languageID, string languageName, float progress)
        {
            if (cancel) return;

            if (field == null) return;
            if (!retranslateAll)
            {
                string currentTranslation;
                if (field.texts.TryGetValue(languageID, out currentTranslation) &&
                    !string.IsNullOrEmpty(currentTranslation))
                {
                    return;
                }
            }

            var originalText = field.texts[0];
            var prompt = $"Translate this text to {languageName}: \"{AITextUtility.DoubleQuotesToSingle(originalText)}\"";
            if (IsAwaitingReply)
            {
                translationRequestQueue.Enqueue(new TranslationRequest(field, languageID, languageName, prompt, originalText, progress));
            }
            else
            {
                TranslateFieldFromPrompt(field, languageID, languageName, prompt, originalText, progress);
            }
        }

        private async void TranslateFieldFromPrompt(TextTableField field, int languageID, string languageName, string prompt, string originalText, float progress)
        {
            if (cancel) return;

            var percent = Mathf.RoundToInt(progress * 100);
            ProgressText = $"[{percent}%] Translating {originalText} to {languageName}. Please wait...";
            Repaint();

            if (Time.realtimeSinceStartup < timeNextRequestAllowed)
            {
                int timeToWait = Mathf.RoundToInt((timeNextRequestAllowed - Time.realtimeSinceStartup) * 1000);
                await Task.Delay(timeToWait);
                Debug.Log("Delaying " + timeToWait);
                timeNextRequestAllowed = Time.realtimeSinceStartup + 1;
            }

            IsAwaitingReply = true;

            currentField = field;
            currentLanguageID = languageID;

            SubmitPrompt(prompt, AssistantPrompt, "Contacting OpenAI", progress, LogDebug, false);
        }

        protected override void SetResultText(string text)
        {
            base.SetResultText(text);
            if (currentField != null)
            {
                currentField.texts[currentLanguageID] = AITextUtility.RemoveSurroundingQuotes(text);
            }
            if (!cancel)
            {
                if (translationRequestQueue.Count > 0)
                {
                    var request = translationRequestQueue.Dequeue();
                    TranslateFieldFromPrompt(request.field, request.languageID, request.languageName, request.prompt, request.originalText, request.progress);
                }
                else
                {
                    Debug.Log("Translate Text Table operation completed.");
                    RepaintTextTable();
                }
            }
        }

        private void CancelTranslation()
        {
            Debug.Log("Cancelling Translate Text Table operation.");
            cancel = true;
            translationRequestQueue.Clear();
            RepaintTextTable();
        }

        private void RepaintTextTable()
        {
            if (TextTableEditorWindow.instance != null)
            {
                TextTableEditorWindow.instance.Close();
                TextTableEditorWindow.ShowWindow();
                Selection.activeObject = textTable;
            }
        }

    }
}

#endif
