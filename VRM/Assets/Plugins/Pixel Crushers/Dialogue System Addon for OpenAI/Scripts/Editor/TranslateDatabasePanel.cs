// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to translate the entire dialogue database.
    /// </summary>
    public class TranslateDatabasePanel : TextGenerationPanel
    {

        private class TranslationRequest
        {
            public Field localizedField;
            public string prompt;
            public string description;
            public float progress;

            public TranslationRequest(Field localizedField, string prompt, string description, float progress)
            {
                this.localizedField = localizedField;
                this.prompt = prompt;
                this.description = description;
                this.progress = progress;
            }
        }

        private List<string> languages = new List<string>();
        private List<string> languageNames = new List<string>();
        private List<bool> translate = new List<bool>();
        private Queue<TranslationRequest> translationRequestQueue = new Queue<TranslationRequest>();
        private bool retranslateAll = false;
        private bool cancel = false;
        private float timeNextRequestAllowed;
        private Field currentField;

        private static GUIContent HeadingLabel = new GUIContent("Translate Database");
        private static GUIContent RetranslateAllLabel = new GUIContent("Retranslate All", "Translate all fields, including nonblank fields. Untick to only translate blank fields.");

        public TranslateDatabasePanel(string apiKey, DialogueDatabase database)
            : base(apiKey, database, null, null, null)
        {
            SetModelByName(TextModelName.GPT3_5_Turbo_16K);
        }

        public void SetLanguages(List<string> languages)
        {
            this.languages = languages;
            languageNames = new List<string>();
            translate = new List<bool>();
            for (int i = 0; i < languages.Count; i++)
            {
                languageNames.Add(AITextUtility.DetermineLanguage(languages[i]));
                translate.Add(true);
            }
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "This will translate these fields:\n" +
                "· Actors: Display Name.\n" +
                "· Quests: Display Name, Group, Descriptions, and Entry text.\n" +
                "· Conversations: Dialogue Text and Menu Text.");

            DrawModelSettings();

            EditorGUILayout.Space();
            retranslateAll = EditorGUILayout.Toggle(RetranslateAllLabel, retranslateAll);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Languages:", EditorStyles.boldLabel);
            for (int i = 0; i < languageNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                translate[i] = EditorGUILayout.ToggleLeft(languages[i], translate[i]);
                languageNames[i] = EditorGUILayout.TextField(languageNames[i]);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(languages.Count == 0 || database == null);
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
                    if (EditorUtility.DisplayDialog("Translate Database",
                        "This operation may take a long time to complete. Proceed?", "OK", "Cancel"))
                    {
                        TranslateDatabase();
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

        private void TranslateDatabase()
        {
            cancel = false;
            timeNextRequestAllowed = 0;
            IsAwaitingReply = false;

            int numFields = database.actors.Count + database.items.Count + database.locations.Count;
            foreach (var conversation in database.conversations)
            {
                numFields += conversation.dialogueEntries.Count;
            }
            var numLangugagesToTranslate = translate.FindAll(x => x == true).Count;
            numFields *= numLangugagesToTranslate;
            int currentFieldNum = 0;

            for (int i = 0; i < languageNames.Count; i++)
            {
                if (cancel) return;
                if (translate[i])
                {
                    TranslateToLanguage(languages[i], languageNames[i], numFields, ref currentFieldNum);
                }
            }
        }

        private void TranslateToLanguage(string languageCode, string languageName, int numFields, ref int currentFieldNum)
        {
            foreach (var actor in database.actors)
            {
                float progress = (float)currentFieldNum++ / (float)numFields;
                TranslateField(actor.fields, "Display Name", languageCode, languageName, actor.Name, progress);
            }

            foreach (var item in database.items)
            {
                currentFieldNum++;
                if (item.IsItem) continue;
                var itemName = item.Name;
                float progress = (float)currentFieldNum / (float)numFields;
                TranslateField(item.fields, "Display Name", languageCode, languageName, itemName, progress);
                TranslateField(item.fields, "Group", languageCode, languageName, itemName, progress);
                TranslateField(item.fields, "Description", languageCode, languageName, itemName, progress);
                TranslateField(item.fields, "Success Description", languageCode, languageName, itemName, progress);
                TranslateField(item.fields, "Failure Description", languageCode, languageName, itemName, progress);
                if (item.IsFieldAssigned("Entry Count"))
                {
                    var entryCount = item.LookupInt("Entry Count");
                    for (int i = 0; i < entryCount; i++)
                    {
                        TranslateField(item.fields, $"Entry {i + 1}", languageCode, languageName, itemName, progress);
                    }
                }
            }

            foreach (var conversation in database.conversations)
            {
                var conversationTitle = conversation.Title;
                foreach (var entry in conversation.dialogueEntries)
                {
                    float progress = (float)currentFieldNum++ / (float)numFields;

                    TranslateField(entry.fields, "Dialogue Text", languageCode, languageName, conversationTitle, progress);
                    TranslateField(entry.fields, "Menu Text", languageCode, languageName, conversationTitle, progress);
                }
            }
        }

        private void TranslateField(List<Field> fields, string fieldTitle, string languageCode, string languageName,
            string description, float progress)
        {
            if (cancel) return;

            var field = Field.Lookup(fields, fieldTitle);
            if (field == null || string.IsNullOrEmpty(field.value)) return;
            var prompt = $"Translate this text to {languageName}: \"{AITextUtility.DoubleQuotesToSingle(field.value)}\"";
            var localizedFieldTitle = (field.title == "Dialogue Text") ? languageCode : $"{field.title} {languageCode}";
            var localizedField = Field.Lookup(fields, localizedFieldTitle);
            if (localizedField == null)
            {
                localizedField = new Field(localizedFieldTitle, "", FieldType.Localization);
                fields.Add(localizedField);
            }
            else if (!(retranslateAll || string.IsNullOrEmpty(localizedField.value)))
            {
                return;
            }
            if (IsAwaitingReply)
            {
                translationRequestQueue.Enqueue(new TranslationRequest(localizedField, prompt, description, progress));
            }
            else
            {
                TranslateFieldFromPrompt(localizedField, prompt, description, progress);
            }
        }

        private async void TranslateFieldFromPrompt(Field localizedField, string prompt, string description, float progress)
        {
            if (cancel) return;

            var percent = Mathf.RoundToInt(progress * 100);
            ProgressText = $"[{percent}%] Translating {description} to {localizedField.title}. Please wait...";
            Repaint();

            if (Time.realtimeSinceStartup < timeNextRequestAllowed)
            {
                int timeToWait = Mathf.RoundToInt((timeNextRequestAllowed - Time.realtimeSinceStartup) * 1000);
                await Task.Delay(timeToWait);
                Debug.Log("Delaying " + timeToWait);
                timeNextRequestAllowed = Time.realtimeSinceStartup + 1;
            }

            IsAwaitingReply = true;

            currentField = localizedField;

            SubmitPrompt(prompt, AssistantPrompt, "Contacting OpenAI", progress, LogDebug, false);
        }

        protected override void SetResultText(string text)
        {
            base.SetResultText(text);
            if (currentField != null)
            {
                currentField.value = AITextUtility.RemoveSurroundingQuotes(text);
            }
            if (!cancel)
            {
                if (translationRequestQueue.Count > 0)
                {
                    var request = translationRequestQueue.Dequeue();
                    TranslateFieldFromPrompt(request.localizedField, request.prompt, request.description, request.progress);
                }
                else
                {
                    Debug.Log("Translate Database operation completed.");
                }
            }
        }

        private void CancelTranslation()
        {
            Debug.Log("Cancelling Translate Database operation.");
            cancel = true;
            translationRequestQueue.Clear();
        }

    }
}

#endif
