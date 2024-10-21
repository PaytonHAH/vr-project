// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to generate portraits for an actor.
    /// </summary>
    public class GeneratePortraitsPanel : BasePanel
    {

        private static GUIContent HeadingLabel = new GUIContent("Generate Portraits");
        private static GUIContent PortraitStyleLabel = new GUIContent("Portrait Style", "How much of actor's body to show in portrait.");

        private const int NumVariations = 3;
        private static string[] ImageSizes = new string[] { "256x256", "512x512", "1024x1024" };
        private static int[] ImagePixels = new int[] { 256, 512, 1024 };

        private enum PortraitStyle { Head, HeadAndShoulders, WaistUp, FullBody, Other }

        private Actor actor;
        private string actorName;
        private string actorLabel;
        private int imageSizeIndex = 0;
        private Vector2 imagePivot = new Vector2(0.5f, 0.5f);
        private int PPU = 100;
        private PortraitStyle portraitStyle = PortraitStyle.HeadAndShoulders;

        private string mainPrompt;
        private List<string> alternatePrompts = new List<string>();
        private List<List<Sprite>> workingImages = new List<List<Sprite>>();
        private int portraitNumber;

        private int ImageSize => ImagePixels[imageSizeIndex];

        public GeneratePortraitsPanel(string apiKey, DialogueDatabase database, Asset asset)
            : base(apiKey, database, asset, null, null)
        {
            actor = asset as Actor;
            actorName = actor.Name;
            actorLabel = $"Actor: {actorName}";
            mainPrompt = actor.Description;
            LogDebug = true;
        }

        ~GeneratePortraitsPanel()
        {
            DestroyWorkingImages();
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Generate portraits for an actor.");
            DrawSettings();
            DrawPortraits();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            portraitStyle = (PortraitStyle)EditorGUILayout.EnumPopup(PortraitStyleLabel, portraitStyle);
            imageSizeIndex = EditorGUILayout.Popup("Image Size", imageSizeIndex, ImageSizes);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pivot", GUILayout.Width(150));
            imagePivot = EditorGUILayout.Vector2Field(GUIContent.none, imagePivot);
            EditorGUILayout.EndHorizontal();
            PPU = EditorGUILayout.IntField("PPU", PPU);
        }

        private void DrawPortraits()
        {
            if (actor == null) return;

            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField(actorLabel, EditorStyles.boldLabel);
            EditorGUI.EndDisabledGroup();

            DrawStatus();

            DrawPortrait(1);
            for (int i = 0; i < actor.spritePortraits.Count; i++)
            {
                DrawPortrait(i + 2);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Portrait"))
            {
                actor.spritePortraits.Add(null);
                SaveDatabaseChanges();
            }
            if (GUILayout.Button("Close"))
            {
                DestroyWorkingImages();
                DialogueSystemOpenAIWindow.Instance.Close();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPortrait(int portraitNum)
        {
            var isMainPortrait = portraitNum == 1;

            EditorGUILayout.Space();
            var labelText = (portraitNum == 1) ? "Main Portrait" : $"Portrait {portraitNum}";
            var currentSprite = isMainPortrait ? actor.spritePortrait : actor.spritePortraits[portraitNum - 2];
            EditorGUILayout.LabelField(labelText, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(GUIContent.none, currentSprite, typeof(Sprite), false, GUILayout.Width(70), GUILayout.Height(64));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField("Current", GUILayout.Width(70));
            EditorGUILayout.EndVertical();

            for (int i = 0; i < NumVariations; i++)
            {
                while (workingImages.Count < portraitNum)
                {
                    workingImages.Add(new List<Sprite>());
                }
                var imageList = workingImages[portraitNum - 1];
                while (imageList.Count < NumVariations)
                {
                    imageList.Add(null);
                }
                var workingImage = imageList[i];
                EditorGUILayout.BeginVertical();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(GUIContent.none, workingImage, typeof(Sprite), false, GUILayout.Width(70), GUILayout.Height(64));
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(workingImage == null);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(2));
                if (GUILayout.Button("Use", GUILayout.Width(46)))
                {
                    var sprite = SaveSprite(workingImage, portraitNum);
                    if (sprite != null)
                    {
                        if (isMainPortrait)
                        {
                            actor.spritePortrait = sprite;
                        }
                        else
                        {
                            while (actor.spritePortraits.Count < portraitNum - 1)
                            {
                                actor.spritePortraits.Add(null);
                            }
                            actor.spritePortraits[portraitNum - 2] = sprite;
                        }
                        SaveDatabaseChanges();
                    }
                }
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    DestroySprite(imageList[i]);
                    imageList[i] = null;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(isMainPortrait ? "Prompt" : "Variation Prompt");
            EditorGUI.BeginDisabledGroup(IsAwaitingReply || (!isMainPortrait && actor.spritePortrait == null));
            if (isMainPortrait)
            {
                mainPrompt = EditorGUILayout.TextArea(mainPrompt);
            }
            else
            {
                while (alternatePrompts.Count < portraitNum - 1)
                {
                    alternatePrompts.Add(string.Empty);
                }
                alternatePrompts[portraitNum - 2] = EditorGUILayout.TextArea(alternatePrompts[portraitNum - 2]);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(IsAwaitingReply || (!isMainPortrait && (actor.spritePortrait == null || string.IsNullOrEmpty(alternatePrompts[portraitNum - 2]))));
            if (GUILayout.Button(isMainPortrait ? "Generate Main Portrait" : $"Generate Portrait {portraitNum}"))
            {
                GeneratePortrait(portraitNum, isMainPortrait ? mainPrompt : alternatePrompts[portraitNum - 2]);
            }
            EditorGUI.EndDisabledGroup();

            if (portraitNum > 1)
            {
                EditorGUI.BeginDisabledGroup(IsAwaitingReply);
                if (GUILayout.Button($"Remove Portrait {portraitNum}"))
                {
                    actor.spritePortraits.RemoveAt(portraitNum - 2);
                    SaveDatabaseChanges();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void GeneratePortrait(int portraitNumber, string prompt)
        {
            this.portraitNumber = portraitNumber;
            var fullPrompt = ApplyPortraitStyleToPrompt(prompt);
            if (LogDebug) Debug.Log($"Sending to OpenAI: {prompt}");
            try
            {
                IsAwaitingReply = true;
                string user = string.Empty;
                if (portraitNumber == 1)
                {
                    OpenAI.SubmitImageGenerationAsync(apiKey, NumVariations, ImageSizes[imageSizeIndex],
                        OpenAI.ResponseFormatB64JSON, user, fullPrompt, ReceivePortraits);
                }
                else
                {
                    OpenAI.SubmitImageEditAsync(apiKey, actor.spritePortrait.texture, null, NumVariations,
                        ImageSizes[imageSizeIndex], OpenAI.ResponseFormatB64JSON, user, fullPrompt, ReceivePortraits);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                IsAwaitingReply = false;
            }
        }

        private string ApplyPortraitStyleToPrompt(string prompt)
        {
            switch (portraitStyle)
            {
                default:
                case PortraitStyle.Other:
                    return prompt;
                case PortraitStyle.Head:
                    return $"{prompt}. Generate a portrait image showing {actorName}'s head.";
                case PortraitStyle.HeadAndShoulders:
                    return $"{prompt}. Generate a portrait image showing {actorName}'s head and shoulders.";
                case PortraitStyle.WaistUp:
                    return $"{prompt}. Generate a portrait image from the waist up.";
                case PortraitStyle.FullBody:
                    return $"{prompt}. Generate a portrait image showing the whole body.";
            }
        }

        private void ReceivePortraits(List<string> b64_jsons)
        {
            IsAwaitingReply = false;
            if (b64_jsons == null || b64_jsons.Count == 0 || string.IsNullOrEmpty(b64_jsons[0]))
            {
                Debug.LogWarning($"Received no images from OpenAI.");
                return;
            }
            if (LogDebug) Debug.Log($"Received from OpenAI: {b64_jsons.Count} images.");
            for (int i = 0; i < b64_jsons.Count; i++)
            {
                var b64_json = b64_jsons[i];

                byte[] bytes = System.Convert.FromBase64String(b64_json);
                var texture2D = new Texture2D(ImageSize, ImageSize);
                texture2D.LoadImage(bytes);
                var sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), imagePivot, PPU);
                var imageList = workingImages[portraitNumber - 1];
                DestroySprite(imageList[i]);
                imageList[i] = sprite;
            }
            Repaint();
        }

        private Sprite SaveSprite(Sprite sprite, int portraitNum)
        {
            if (sprite == null) return null;
            var defaultName = (portraitNum == 1) ? actorName : $"{actorName} {portraitNum}";
            var filename = EditorUtility.SaveFilePanelInProject("Save Sprite", defaultName, "png", "");
            if (string.IsNullOrEmpty(filename)) return null;
            var fullPath = Application.dataPath + "/" + filename.Substring("Assets/".Length); // Remove extra Assets/.
            byte[] bytes = sprite.texture.EncodeToPNG();
            Debug.Log($"Saving portrait image to {filename}");
            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(filename);
            TextureImporter importer = AssetImporter.GetAtPath(filename) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.isReadable = true;
            AssetDatabase.WriteImportSettingsIfDirty(filename);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(filename);
            var asset = AssetDatabase.LoadAssetAtPath<Sprite>(filename);
            return asset;
        }

        private void SaveDatabaseChanges()
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Repaint();
        }

        private void DestroyWorkingImages()
        {
            foreach (var list in workingImages)
            {
                if (list == null) continue;
                foreach (var sprite in list)
                {
                    DestroySprite(sprite);
                }
                list.Clear();
            }
            workingImages.Clear();
            workingImages = new List<List<Sprite>>();
        }

        private void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

    }
}

#endif
