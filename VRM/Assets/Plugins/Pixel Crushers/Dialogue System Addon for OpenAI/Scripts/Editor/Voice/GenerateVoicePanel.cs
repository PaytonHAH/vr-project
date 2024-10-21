// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;
using static PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs.ElevenLabsAPI;
using static UnityEngine.Rendering.DebugUI.MessageBox;
using UnityEditor.Rendering;
#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif
#if USE_DEEPVOICE
using PixelCrushers.DialogueSystem.OpenAIAddon.DeepVoice;
#endif

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{

    public enum AudioSequencerCommands { None, AudioWait, SALSA, Other }

    /// <summary>
    /// Panel to generate voiceover for a dialogue entry using an ElevenLabs voice actor.
    /// </summary>
    public class GenerateVoicePanel : ElevenLabsPanel
    {

        private string textForVoiceover;
        private Actor actor;
        private string voiceName;
        private string voiceID;
        private bool isLocalizationField;

#if USE_DEEPVOICE
        private float variability = 0.3f;
        private float clarity = 0.75f;
#endif

        private static string lastFilename;
        private static AudioSequencerCommands sequencerCommand = AudioSequencerCommands.None;
        private static string otherSequencerCommand = "";

        private static GUIContent HeadingLabel = new GUIContent("Generate Voiceover");
        public static GUIContent SequencerCommandLabel = new GUIContent("Sequencer Command", "Add sequencer command to dialogue entry's Sequence field.");

        protected override string Operation => "Generate Voiceover";

        public GenerateVoicePanel(string apiKey, DialogueDatabase database,
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
            textForVoiceover = (field != null && field.type == FieldType.Localization) ? field.value
                : (entry != null) ? entry.DialogueText : string.Empty;
            actor = (entry != null) ? database.GetActor(entry.ActorID) : null;
            voiceName = (actor != null) ? actor.LookupValue(DialogueSystemFields.Voice) : null;
            voiceID = (actor != null) ? actor.LookupValue(DialogueSystemFields.VoiceID) : null;
            lastFilename = EditorPrefs.GetString(DialogueSystemOpenAIWindow.ElevenLabsLastFilename);
            isLocalizationField = field != null && field.type == FieldType.Localization;
        }

        ~GenerateVoicePanel()
        {
            DestroyAudioClip();
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Generate voiceover for this line using the actor's selected voice.");
            if (actor == null)
            {
                EditorGUILayout.LabelField("Assign an actor to this dialogue entry first.");
            }
            else if (string.IsNullOrEmpty(voiceID))
            {
                EditorGUILayout.LabelField("Select a voice for this actor first.");
                if (GUILayout.Button("Select Voice"))
                {
                    DialogueSystemOpenAIWindow.Open(AIRequestType.SelectVoice, database, actor, null, null);
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                DrawGenerateButton();
                DrawPreviewButton();
                DrawAcceptButton();
            }
            DrawStatus();
        }

        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("Dialogue Text");
            EditorGUILayout.TextArea(textForVoiceover);
            EditorGUILayout.TextField("Voice", voiceName);
            EditorGUI.EndDisabledGroup();

#if USE_DEEPVOICE
            variability = EditorGUILayout.Slider("Variability", variability, 0, 1);
            clarity = EditorGUILayout.Slider("Clarity", clarity, 0, 1);
#endif

            EditorGUI.BeginDisabledGroup(isLocalizationField);
            sequencerCommand = (AudioSequencerCommands)EditorGUILayout.EnumPopup(SequencerCommandLabel, sequencerCommand);
            if (sequencerCommand == AudioSequencerCommands.Other)
            {
                otherSequencerCommand = EditorGUILayout.TextField("Command", otherSequencerCommand);
            }
            var needToInputSequencerCommand = sequencerCommand == AudioSequencerCommands.Other && string.IsNullOrEmpty(otherSequencerCommand);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(IsAwaitingReply || needToInputSequencerCommand);
            if (GUILayout.Button("Generate"))
            {
                GenerateAudio();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPreviewButton()
        {
            EditorGUI.BeginDisabledGroup(audioClip == null || IsAwaitingReply);
            if (GUILayout.Button("Preview"))
            {
                EditorAudioUtility.PlayAudioClip(audioClip);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void GenerateAudio()
        {
            DestroyAudioClip();
            IsAwaitingReply = true;
            ProgressText = $"Generating voiceover: {textForVoiceover}";

            // Handle OpenAI voice generation.
            if (voiceID == "OpenAI")
            {
                var openAIVoice = System.Enum.Parse<Voices>(voiceName);
                OpenAI.SubmitVoiceGenerationAsync(
                    openAIKey,
                    TTSModel.TTSModel1HD,
                    openAIVoice,
                    VoiceOutputFormat.MP3,
                    1,
                    textForVoiceover,
                    OnReceivedTextToSpeech
                );
                return;
            }

            // Handle DeepVoice voice generation if applicable.
#if USE_DEEPVOICE
    if (voiceID.StartsWith("DeepVoice"))
    {
        Debug.Log($"Generating DeepVoice voiceover for: {textForVoiceover}");
        DeepVoiceAPI.GetTextToSpeech(
            DeepVoiceAPI.GetDeepVoiceModel(voiceID),
            voiceName, 
            textForVoiceover,
            variability, 
            clarity,
            OnReceivedTextToSpeech
        );
        return;
    }
#endif

            // Handle ElevenLabs voice generation.
            Debug.Log($"Generating voiceover for: {textForVoiceover}");

            // Ensure you define values for `stability`, `similarityBoost`, and `style`.
            float stability = 0.5f;             // Example value for stability.
            float similarityBoost = 0.8f;        // Example value for similarity boost.
            float style = 0.3f;                  // Example value for style.

            ElevenLabsAPI.GetTextToSpeech(
                apiKey,                          // Your API key
                modelId,                         // The model ID you're using
                voiceName,                       // The voice name
                voiceID,                         // The voice ID
                stability,                       // Stability (float value)
                similarityBoost,                 // Similarity boost (float value)
                style,                           // Style (float value)
                textForVoiceover,                // The text to convert into speech
                OnReceivedTextToSpeech           // Callback method to handle the AudioClip
            );
        }


        private void OnReceivedTextToSpeech(AudioClip audioClip)
        {
            IsAwaitingReply = false;
            if (audioClip == null) return;
            this.audioClip = audioClip;
            Debug.Log($"Playing: {textForVoiceover}");
            EditorAudioUtility.PlayAudioClip(audioClip);
            Repaint();
        }

        private void DrawAcceptButton()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(audioClip == null || IsAwaitingReply);
            if (GUILayout.Button("Accept"))
            {
                var filename = SaveAudioClip();
                if (!string.IsNullOrEmpty(filename))
                {
                    if (!isLocalizationField)
                    {
                        AddSelectedSequencerCommand(sequencerCommand, System.IO.Path.GetFileNameWithoutExtension(filename), database, entry);
                    }
                    SaveDatabaseChanges();
                    RefreshEditor();
                    CloseWindow();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel"))
            {
                CloseWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void AddSelectedSequencerCommand(AudioSequencerCommands sequencerCommand, string entrytag, DialogueDatabase database, DialogueEntry entry)
        {
            switch (sequencerCommand)
            {
                case AudioSequencerCommands.AudioWait:
                case AudioSequencerCommands.SALSA:
                    AddSequencerCommand(sequencerCommand.ToString(), entrytag, database, entry);
                    break;
                case AudioSequencerCommands.Other:
                    AddSequencerCommand(otherSequencerCommand, entrytag, database, entry);
                    break;
            }
        }

        public static void AddSequencerCommand(string command, string entrytag, DialogueDatabase database, DialogueEntry entry)
        {
            var sequence = entry.Sequence;
            if (!(string.IsNullOrEmpty(sequence) || sequence.EndsWith(";")))
            {
                sequence += ";\n";
            }
            sequence += $"{command}({entrytag})";
            entry.Sequence = sequence;
            PrefabUtility.RecordPrefabInstancePropertyModifications(database);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
            AssetDatabase.Refresh();
            Debug.Log($"Sequence: {entry.Sequence}");
        }

        private string SaveAudioClip()
        {
            if (audioClip == null) return string.Empty;
            var conversation = database.GetConversation(entry.conversationID);
            var defaultName = database.GetEntrytag(conversation, entry, GetEntrytagFormat());
            if (isLocalizationField) defaultName += "_" + field.title;
            var path = "Assets";
            if (!string.IsNullOrEmpty(lastFilename))
            {
                path = Application.dataPath + "/" + System.IO.Path.GetDirectoryName(lastFilename).Replace("\\", "/").Substring("Assets/".Length);
            }
#if USE_ADDRESSABLES
            var title = "Save Audio Clip";
#else
            var title = "Save Audio Clip in Resources Folder";
#endif
            var filename = EditorUtility.SaveFilePanelInProject(title, defaultName, "wav", "", path);
            if (string.IsNullOrEmpty(filename)) return string.Empty;
            lastFilename = filename;
            EditorPrefs.SetString(DialogueSystemOpenAIWindow.ElevenLabsLastFilename, lastFilename);
            // Remove extra Assets/ & extension:
            var fullPath = Application.dataPath + "/" + filename.Substring("Assets/".Length); 
            Debug.Log($"Saving audio clip to {filename}");
            SavWav.Save(fullPath, audioClip);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(filename);

#if USE_ADDRESSABLES
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings != null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AudioClip>(filename);
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                settings.CreateAssetReference(assetGUID);
                AddressableAssetEntry addressableEntry = settings.FindAssetEntry(assetGUID);
                if (addressableEntry != null)
                {
                    addressableEntry.address = System.IO.Path.GetFileNameWithoutExtension(filename);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                    AssetDatabase.SaveAssets();
                }
            }
#endif

            return filename;
        }

        public static EntrytagFormat GetEntrytagFormat()
        {
            var dialogueManager = GameObject.FindObjectOfType<DialogueSystemController>();
            if (dialogueManager != null)
            {
                return dialogueManager.displaySettings.cameraSettings.entrytagFormat;
            }
            else
            {
                return EntrytagFormat.ActorName_ConversationID_EntryID;
            }
        }

        private void SaveDatabaseChanges()
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Repaint();
        }

        protected override void RefreshEditor()
        {
            Undo.RecordObject(database, Operation);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Reset();
            DialogueEditorWindow.OpenDialogueEntry(database, entry.conversationID, entry.id);
            DialogueEditorWindow.instance?.Repaint();
            GUIUtility.ExitGUI();
        }

    }
}

#endif
