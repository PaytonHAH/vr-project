// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to rewrite the text of a single field.
    /// </summary>
    public class ReviseTextPanel : ActorTextGenerationPanel
    {

        private int numVariations = 1;
        private string refinementPrompt;
        private List<string> variants = null;
        private string label = null;

        private const int MaxVariations = 10;

        private static GUIContent HeadingLabel = new GUIContent("Revise Text");
        private static GUIContent RefineLabel = new GUIContent("Refinement Instructions:", "Refine text using this prompt.");
        private static GUIContent RefineButtonLabel = new GUIContent("Refine", "Refine text using this prompt.");
        private static GUIContent GrammarSpellingButtonLabel = new GUIContent("Fix Grammar & Spelling", "Fix grammar and spelling.");
        private static GUIContent AcceptLabel = new GUIContent("Accept");

        public ReviseTextPanel(string apiKey, DialogueDatabase database, 
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Revise the text of a field.");
            DrawModelSettings();
            DrawExistingField();
            DrawPrompt();
            DrawResult();
        }

        private void DrawExistingField()
        {
            if (label == null)
            {
                label = (IsDialogueEntry ? "Dialogue Entry" : asset.Name) + ": " + field.title;
            }
            EditorGUILayout.LabelField(label);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(field.value, readOnlyTextAreaStyle);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPrompt()
        {
            numVariations = EditorGUILayout.IntSlider("Num Variations", numVariations, 1, MaxVariations);
            EditorGUILayout.BeginHorizontal();
            refinementPrompt = EditorGUILayout.TextField(RefineLabel, refinementPrompt);
            EditorGUI.BeginDisabledGroup(IsAwaitingReply || (string.IsNullOrEmpty(refinementPrompt) && numVariations == 1));
            var size = GUI.skin.button.CalcSize(RefineButtonLabel);
            if (GUILayout.Button(RefineButtonLabel, GUILayout.Width(size.x)))
            {
                RefineText(field, refinementPrompt);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            DrawAssistantPrompt();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(IsAwaitingReply);
            var fixSize = GUI.skin.button.CalcSize(GrammarSpellingButtonLabel);
            if (GUILayout.Button(GrammarSpellingButtonLabel, GUILayout.Width(fixSize.x)))
            {
                RefineText(field, "Fix the grammar and spelling.");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            DrawStatus();
        }

        private void DrawResult()
        {
            if (numVariations == 1)
            {
                DrawSingleResult();
            }
            else
            {
                DrawResultVariations();
            }
        }

        private void DrawSingleResult()
        { 
            EditorGUILayout.LabelField("Proposed Revision:");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(ResultText, readOnlyTextAreaStyle);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(ResultText));
            if (GUILayout.Button("Accept"))
            {
                AcceptText(ResultText);
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel"))
            {
                DialogueSystemOpenAIWindow.Instance.Close();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResultVariations()
        {
            if (variants == null && !string.IsNullOrEmpty(ResultText) && !IsAwaitingReply)
            {
                var isNumbered = ResultText.StartsWith("1");
                variants = new List<string>();
                var texts = ResultText.Split('\n');
                foreach (var text in texts)
                {
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    // If numbered, format is "1. text".
                    // Otherwise format is "Description: text".
                    var variant = isNumbered ? text.Substring(text.IndexOf(".") + 1)
                        : text.Substring(text.IndexOf(":") + 1);
                    variants.Add(AITextUtility.RemoveSurroundingQuotes(variant.Trim()));
                }
            }

            EditorGUILayout.LabelField("Proposed Revisions:");

            if (variants == null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(string.Empty);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                var size = GUI.skin.button.CalcSize(AcceptLabel);
                foreach (var variant in variants)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextArea(variant, readOnlyTextAreaStyle);
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button("Accept", GUILayout.Width(size.x)))
                    {
                        AcceptText(variant);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(ResultText) || !IsDialogueEntry);
            if (GUILayout.Button("Use All"))
            {
                AcceptAll();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel"))
            {
                DialogueSystemOpenAIWindow.Instance.Close();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefineText(Field field, string refinementPrompt)
        {
            var prompt = (numVariations == 1) ? "Rewrite" : $"Rewrite {numVariations} variations of";
            prompt += $" this text: \"{AITextUtility.DoubleQuotesToSingle(field.value)}\". {refinementPrompt}";
            variants = null;
            SubmitPrompt(prompt, AssistantPrompt);
        }

        private void AcceptText(string text)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            field.value = AITextUtility.RemoveSurroundingQuotes(text.Trim());
            RefreshEditor();
        }

        private void AcceptAll()
        {
            // Is dialogue entry, and has generated multiple variants.
            // Replace entry with a group entry that adds all variants as children.
            // If a non-player entry, set Script field to RandomizeNextEntry().
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var template = TemplateTools.LoadFromEditorPrefs();
            var conversation = database.GetConversation(entry.conversationID);
            var actor = database.GetActor(entry.ActorID);

            var rect = entry.canvasRect;
            var oldSequence = entry.Sequence;
            var oldOutgoingLinks = new List<Link>(entry.outgoingLinks);
            entry.outgoingLinks.Clear();
            entry.isGroup = true;
            entry.DialogueText = string.Empty;
            entry.Sequence = string.Empty;
            if (!actor.IsPlayer)
            {
                entry.userScript = $"RandomizeNextEntry(); {entry.userScript}";
            }
            for (int i = 0; i < variants.Count; i++)
            {
                var variant = variants[i];
                var node = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
                node.ActorID = entry.ActorID;
                node.ConversantID = entry.ConversantID;
                node.DialogueText = variant;
                node.Sequence = oldSequence;
                node.canvasRect = new Rect(rect.x + (i + 1) * 20, rect.y + rect.height + i * 20, rect.width, rect.height);
                conversation.dialogueEntries.Add(node);
                entry.outgoingLinks.Add(new Link(conversation.id, entry.id, conversation.id, node.id));
                foreach (var oldLink in oldOutgoingLinks)
                {
                    node.outgoingLinks.Add(new Link(conversation.id, node.id, oldLink.destinationConversationID, oldLink.destinationDialogueID));
                }
            }
            RefreshEditor();
        }

        private void RefreshEditor()
        {
            Undo.RecordObject(database, "Revise Text");
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Reset();
            if (IsDialogueEntry && entry != null)
            {
                DialogueEditorWindow.OpenDialogueEntry(database, entry.conversationID, entry.id);
            }
            DialogueEditorWindow.instance?.Repaint();
            GUIUtility.ExitGUI();
        }

    }
}

#endif
