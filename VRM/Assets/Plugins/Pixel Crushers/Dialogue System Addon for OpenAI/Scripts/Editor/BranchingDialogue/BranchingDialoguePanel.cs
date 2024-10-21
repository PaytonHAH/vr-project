// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;
using System;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to generate voiceover for a dialogue entry using an ElevenLabs voice actor.
    /// </summary>
    public class BranchingDialoguePanel : TextGenerationPanel
    {

        private static GUIContent HeadingLabel = new GUIContent("Branching Dialogue");
        private static GUIContent ContextLabel = new GUIContent("Context (Description)", "What the conversation is about.");
        private static GUIContent NoneLabel = new GUIContent("None", "Deselect all proposed texts.");
        private static GUIContent FirstLabel = new GUIContent("First", "Select only the first text of each entry.");
        private static GUIContent AllLabel = new GUIContent("All", "Select all proposed texts.");

        private Conversation conversation = null;
        private string context = string.Empty;
        private Actor actor = null;
        private Actor conversant = null;
        private string actorName = string.Empty;
        private string conversantName = string.Empty;
        private string actorDescription = string.Empty;
        private string conversantDescription = string.Empty;
        private BranchingDialogueData data = null;
        private int nodeIndex;
        private Dictionary<int, string> actorDict = new Dictionary<int, string>();

        public BranchingDialoguePanel(string apiKey, DialogueDatabase database, Conversation conversation)
            : base(apiKey, database, conversation, null, null)
        {
            this.data = null;
            this.conversation = conversation;
            if (conversation != null)
            {
                context = conversation.Description;
                actor = database.GetActor(conversation.ActorID);
                if (actor != null)
                {
                    actorName = actor.Name;
                    actorDescription = actor.Description;
                }
                conversant = database.GetActor(conversation.ConversantID);
                if (conversant != null)
                {
                    conversantName = conversant.Name;
                    conversantDescription = conversant.Description;
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Use Dialogue Smith to generate dialogue for a branching conversation whose Title fields are filled in.");
            if (conversation == null)
            {
                EditorGUILayout.LabelField("Select a conversation in the Dialogue Editor and click the 'AI' button.");
            }
            else if (actor == null || conversant == null)
            {
                EditorGUILayout.LabelField("Conversation must have an Actor and Conversant. Assign them and then click the 'AI' button again.");
            }
            else
            {
                DrawModelSettings();
                DrawGenerateButton();
                DrawStatus();
                DrawPreviewButton();
                DrawAcceptButton();
            }
        }

        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Conversation", conversation.Title, readOnlyTextFieldStyle);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Actor", actorName, readOnlyTextFieldStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.TextArea(actorDescription, readOnlyTextAreaStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Conversant", conversantName, readOnlyTextFieldStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.TextArea(conversantDescription, readOnlyTextAreaStyle);
            EditorGUI.EndDisabledGroup();

            DrawAssistantPrompt();

            EditorGUILayout.LabelField(ContextLabel, EditorStyles.boldLabel);
            context = EditorGUILayout.TextArea(context);

            EditorGUI.BeginDisabledGroup(IsAwaitingReply || string.IsNullOrEmpty(context));
            if (GUILayout.Button("Generate"))
            {
                Generate();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPreviewButton()
        {
            if (data == null || IsAwaitingReply) return;
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(NoneLabel)) SelectNone();
            if (GUILayout.Button(FirstLabel)) SelectFirst();
            if (GUILayout.Button(AllLabel)) SelectAll();
            EditorGUILayout.EndHorizontal();

            foreach (var node in data.nodes)
            {
                if (node.entryID == 0) continue; // Skip <START>.
                EditorGUILayout.LabelField($"Dialogue Entry [{node.entryID}]: {node.title}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                for (int i = 0; i < node.text.Count; i++)
                {
                    node.accept[i] = EditorGUILayout.ToggleLeft("Dialogue Text:", node.accept[i]);
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextArea(node.text[i], readOnlyTextAreaStyle);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                }
                //--- No. We previously forced one to be selected, but no longer:
                //var isAnySelected = false;
                //for (int i = 0; i < node.text.Count; i++)
                //{
                //    if (node.accept[i]) isAnySelected = true;
                //}
                //if (!isAnySelected && node.accept != null && node.accept.Count > 0) node.accept[0] = true;
                EditorGUI.indentLevel--;
            }
        }

        private void SelectNone()
        {
            foreach (var node in data.nodes)
            {
                for (int i = 0; i < node.text.Count; i++)
                {
                    node.accept[i] = false;
                }
            }
        }

        private void SelectFirst()
        {
            foreach (var node in data.nodes)
            {
                for (int i = 0; i < node.text.Count; i++)
                {
                    node.accept[i] = (i == 0);
                }
            }
        }

        private void SelectAll()
        {
            foreach (var node in data.nodes)
            {
                for (int i = 0; i < node.text.Count; i++)
                {
                    node.accept[i] = true;
                }
            }
        }

        private void DrawAcceptButton()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(data == null || IsAwaitingReply);
            if (GUILayout.Button("Accept"))
            {
                Accept();
                CloseWindow();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel"))
            {
                CloseWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Generate()
        {
            var actorDescriptions = AIConversationUtility.GetActorDescriptions(database,
                actor.Name, conversant.Name, conversation.Description).Trim();
            var context = actorDescriptions;
            context += context.Trim().EndsWith('.') ? " " : ". ";
            context += conversation.Description;

            var nodes = new List<Node>();
            var processedEntries = new HashSet<DialogueEntry>();
            ProcessEntryRecursive(conversation.GetFirstDialogueEntry(), new List<DialogueEntry>(), nodes, processedEntries);
            foreach (var entry in conversation.dialogueEntries)
            {
                if (processedEntries.Contains(entry)) continue;
                AddNode(entry, new List<DialogueEntry>(), nodes, processedEntries);
            }

            data = new BranchingDialogueData();
            data.context = context;
            data.nodes = nodes;

            IsAwaitingReply = true;
            nodeIndex = 1;
            GenerateTextForCurrentNode();
        }

        private void ProcessEntryRecursive(DialogueEntry entry, List<DialogueEntry> path, List<Node> nodes, HashSet<DialogueEntry> processedEntries)
        {
            if (entry == null || processedEntries.Contains(entry)) return;
            AddNode(entry, path, nodes, processedEntries);
            foreach (var link in entry.outgoingLinks)
            {
                if (link.destinationConversationID != entry.conversationID) continue;
                var child = conversation.GetDialogueEntry(link.destinationDialogueID);
                if (processedEntries.Contains(child)) continue;
                var childPath = new List<DialogueEntry>(path) { entry };
                ProcessEntryRecursive(child, childPath, nodes, processedEntries);
            }
        }

        private void AddNode(DialogueEntry entry, List<DialogueEntry> path, List<Node> nodes, HashSet<DialogueEntry> processedEntries)
        {
            var node = new Node();
            node.entryID = entry.id;
            node.title = entry.Title;
            node.actorName = GetActorName(entry.ActorID);
            node.pathToNode = path;
            node.text = new List<string>();
            var dialogueText = entry.DialogueText;
            if (!string.IsNullOrEmpty(dialogueText))
            {
                node.text.Add(dialogueText);
            }
            nodes.Add(node);
            processedEntries.Add(entry);
        }

        private string GetActorName(int actorID)
        {
            string actorName;
            if (!actorDict.TryGetValue(actorID, out actorName))
            {
                var actor = database.GetActor(actorID);
                actorName = (actor != null) ? actor.Name : $"Actor {actorID}";
            }
            return actorName;
        }

        private void GenerateTextForCurrentNode()
        {
            if (nodeIndex < data.nodes.Count)
            {
                var node = data.nodes[nodeIndex];
                ProgressText = $"Entry [{node.entryID}] {node.actorName}: {node.title}";
                GenerateText(node);
            }
            else
            {
                OnFinishedConversation(data);
            }
        }

        private void GenerateText(Node node)
        {
            // If we already have text, skip this node:
            if (node.text.Count > 0)
            {
                nodeIndex++;
                GenerateTextForCurrentNode();
                return;
            }

            // Otherwise send it to OpenAI for generation:
            var prompt = $"The text that follows is a fictional conversation. {data.context}";
            if (!prompt.EndsWith('.')) prompt += '.';
            prompt += '\n';
            for (int i = 0; i < node.pathToNode.Count; i++)
            {
                if (node.pathToNode[i].id == 0) continue;
                var prevNode = data.nodes.Find(x => x.entryID == node.pathToNode[i].id);
                var text = (prevNode != null && prevNode.text.Count > 0)
                    ? prevNode.text[0]
                    : prevNode.title;
                prompt += $"{prevNode.actorName} says: {text}\n";
            }
            prompt += $"The next line should be spoken by {node.actorName}. In this line, {node.title}. Generate the next line.";
            Debug.Log($"Generating text for entry [{node.entryID}] {node.actorName}: {node.title}");

            SubmitPrompt(prompt, AssistantPrompt);
        }

        protected override void SetResultText(string text)
        {
            if (LogDebug) Debug.Log($"Received from OpenAI: {text}");
            text = AITextUtility.RemoveSpeaker(data.nodes[nodeIndex].actorName, text);
            text = AITextUtility.RemoveSurroundingQuotes(text);
            data.nodes[nodeIndex].text.Add(text);
            nodeIndex++;
            GenerateTextForCurrentNode();
        }

        private void OnFinishedConversation(BranchingDialogueData response)
        {
            IsAwaitingReply = false;
            Debug.Log($"Finished generating text for branching dialogue.");
            data = response;
            if (data == null) return;
            foreach (var node in data.nodes)
            {
                node.accept = new List<bool>();
                for (int i = 0; i < node.text.Count; i++)
                {
                    node.accept.Add(true);
                }
            }
            Repaint();
        }

        private void Accept()
        {
            if (data == null) return;
            var playerID = -1;
            if (actor.IsPlayer) playerID = actor.id;
            if (conversant.IsPlayer) playerID = conversant.id;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var template = TemplateTools.LoadFromEditorPrefs();
            conversation = database.GetConversation(conversation.id);
            var addedGroup = false;

            foreach (var node in data.nodes)
            {
                if (node.entryID == 0) continue; // Skip <START>.

                var entry = conversation.GetDialogueEntry(node.entryID);
                if (entry == null) continue;

                // If entry already has text, skip it:
                if (!string.IsNullOrEmpty(entry.DialogueText)) continue;

                int numAccepted = node.accept.FindAll(x => x == true).Count;

                if (node.text.Count == 0 || numAccepted == 0)
                {
                    // If no text, skip it.
                }
                else if (node.text.Count == 1 || numAccepted == 1)
                {
                    // If only one text option, or only one selected, set entry's text:
                    for (int i = 0; i < node.text.Count; i++)
                    {
                        if (node.accept[i])
                        {
                            entry.DialogueText = node.text[i].Trim();
                            break;
                        }
                    }
                }
                else
                {
                    // If more than one option, add a group:
                    entry.DialogueText = string.Empty;
                    entry.isGroup = true;
                    if (entry.ActorID != playerID) entry.userScript = "RandomizeNextEntry()";
                    addedGroup = true;
                    var links = new List<Link>();

                    for (int i = 0; i < node.text.Count; i++)
                    {
                        if (!node.accept[i]) continue;
                        var text = node.text[i];
                        if (text.StartsWith("Option ") && text.EndsWith(":")) continue;
                        var dialogueText = text.Trim();
                        if (text.StartsWith("['") && text.EndsWith("']")) dialogueText = text.Substring(2, text.Length - 4);
                        var childEntry = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
                        childEntry.ActorID = entry.ActorID;
                        childEntry.ConversantID = entry.ConversantID;
                        conversation.dialogueEntries.Add(childEntry);
                        childEntry.DialogueText = dialogueText;
                        childEntry.outgoingLinks = new List<Link>(entry.outgoingLinks.ToArray());
                        links.Add(new Link(conversation.id, entry.id, conversation.id, childEntry.id));
                    }

                    entry.outgoingLinks = links;
                }
            }

            SaveDatabaseChanges();
            if (addedGroup) DialogueEditorWindow.instance.AutoArrangeNodes(true);
        }

        private void SaveDatabaseChanges()
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueEditorWindow.instance?.Reset();
            DialogueEditorWindow.OpenDialogueEntry(database, conversation.id, conversation.GetFirstDialogueEntry().id);
            DialogueEditorWindow.instance?.Repaint();
        }

        private void CloseWindow()
        {
            DialogueSystemOpenAIWindow.Instance.Close();
            GUIUtility.ExitGUI();
        }

    }

}

#endif
