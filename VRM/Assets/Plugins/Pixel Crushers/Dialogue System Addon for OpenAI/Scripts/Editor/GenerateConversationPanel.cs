// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem.DialogueEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to generate conversations or barks.
    /// </summary>
    public class GenerateConversationPanel : ActorTextGenerationPanel
    {

        private string conversationTitle;
        private string verb = "regarding";
        private string topic;
        private string refinementPrompt;
        private int numBarks = 10;

        private int toolbarIndex = 0;

        private bool IsBarks { get { return toolbarIndex == 1; } }

        private const int MaxBarks = 32;

        private static string[] ToolbarNames = { "Generate Conversation", "Generate Barks" };
        private static GUIContent HeadingLabel = new GUIContent("Generate Conversation");
        private static GUIContent ConversationTitleLabel = new GUIContent("Conversation Title");
        private static GUIContent VerbLabel = new GUIContent("Action", "Examples: 'regarding', 'arguing about', 'reminiscing over'");
        private static GUIContent TopicLabel = new GUIContent("Topic", "Subject the Actor and Conversant are talking about.");
        private static GUIContent GenerateButtonLabel = new GUIContent("Generate", "Send request to OpenAI to generate conversation.");
        private static GUIContent RefineLabel = new GUIContent("Refinement Instructions:", "Refine dialogue using this prompt.");
        private static GUIContent RefineButtonLabel = new GUIContent("Refine", "Refine dialogue using this prompt.");

        public GenerateConversationPanel(string apiKey, DialogueDatabase database)
            : base(apiKey, database, null, null, null)
        {
            AssistantPrompt = "Keep dialogue short.";
        }

        public override void Draw()
        {
            base.Draw();
            DrawHeading(HeadingLabel, "Generate a conversation between two actors or a set of barks.");
            DrawToolbar();
            DrawModelSettings();
            DrawConversationProperties();
            DrawPrompt();
            DrawResult();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.Space();
            toolbarIndex = GUILayout.Toolbar(toolbarIndex, ToolbarNames);
        }

        private void DrawConversationProperties()
        {
            var originalGUIColor = GUI.color;
            GUI.color = string.IsNullOrEmpty(conversationTitle) ? Color.red : originalGUIColor;
            conversationTitle = EditorGUILayout.TextField(ConversationTitleLabel, conversationTitle);
            GUI.color = originalGUIColor;
            DrawActorAndConversant();
            if (!IsBarks) DrawExtraActors();
            DrawIncludeLocations();
        }

        private void DrawPrompt()
        {
            var originalGUIColor = GUI.color;

            GUI.color = string.IsNullOrEmpty(verb) ? Color.red : originalGUIColor;
            verb = EditorGUILayout.TextField(VerbLabel, verb);
            GUI.color = originalGUIColor;

            EditorGUILayout.LabelField(TopicLabel);
            GUI.color = string.IsNullOrEmpty(topic) ? Color.red : originalGUIColor;
            topic = EditorGUILayout.TextArea(topic);
            GUI.color = originalGUIColor;

            DrawAssistantPrompt();

            if (IsBarks)
            {
                numBarks = EditorGUILayout.IntSlider("Num Barks", numBarks, 1, MaxBarks);
            }

            EditorGUI.BeginDisabledGroup(IsAwaitingReply || !CanGenerateConversation() || (IsBarks && numBarks < 1));
            if (IsBarks)
            {
                if (GUILayout.Button("Generate Barks"))
                {
                    GenerateBarks();
                }
            }
            else
            {
                if (GUILayout.Button(GenerateButtonLabel))
                {
                    GenerateConversation();
                }
            }

            EditorGUI.EndDisabledGroup();

            if (IsAwaitingReply && string.IsNullOrEmpty(refinementPrompt))
            {
                EditorGUILayout.HelpBox(ProgressText, MessageType.Info);
            }
        }

        private void DrawResult()
        {
            EditorGUI.BeginDisabledGroup(true);
            var outputHeight = string.IsNullOrEmpty(ResultText) ? 3 * EditorGUIUtility.singleLineHeight
                : GUI.skin.textArea.CalcHeight(new GUIContent(ResultText), DialogueSystemOpenAIWindow.Instance.position.width) +
                2 * EditorGUIUtility.singleLineHeight;
            outputHeight = Mathf.Max(outputHeight, 100f);
            EditorGUILayout.TextArea(ResultText, readOnlyTextAreaStyle, GUILayout.Height(outputHeight));
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(IsAwaitingReply || string.IsNullOrEmpty(ResultText));
            if (!IsBarks)
            {
                EditorGUILayout.BeginHorizontal();
                refinementPrompt = EditorGUILayout.TextField(RefineLabel, refinementPrompt);
                var size = GUI.skin.button.CalcSize(RefineButtonLabel);
                if (GUILayout.Button("Refine", GUILayout.Width(size.x)))
                {
                    RefineConversation(refinementPrompt);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();

            if (IsAwaitingReply && !string.IsNullOrEmpty(refinementPrompt))
            {
                EditorGUILayout.HelpBox(ProgressText, MessageType.Info);
            }


            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(ResultText));
            if (GUILayout.Button("Accept"))
            {
                if (IsBarks)
                {
                    AcceptBarks();
                }
                else
                {
                    AcceptConversation();
                }
                EditorUtility.SetDirty(database);
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

        private bool CanGenerateConversation()
        {
            return !string.IsNullOrEmpty(conversationTitle) &&
                !string.IsNullOrEmpty(verb) &&
                !string.IsNullOrEmpty(topic) &&
                actorIndex >= 0 && conversantIndex >= 0;
        }

        private void GenerateConversation()
        {
            var conversationActor = actorNames[actorIndex];
            var conversationConversant = actorNames[conversantIndex];
            var prompt = GetLocationDescriptions() + 
                GetActorDescriptions(conversationActor, conversationConversant, topic) +                
                $"Write a dialogue between {conversationActor} and {conversationConversant} {verb} {topic}.";
            if (extraActorIndices.Count > 0)
            {
                prompt += " Also involve ";
                for (int i = 0; i < extraActorIndices.Count; i++) 
                {
                    var isLast = i == extraActorIndices.Count - 1;
                    if (i > 0) prompt += ", ";
                    if (isLast && extraActorIndices.Count > 1) prompt += "and ";
                    prompt += actorNames[extraActorIndices[i]];
                    if (isLast) prompt += " in the dialogue.";
                }
            }
            SubmitPrompt(prompt, AssistantPrompt, "Generating Conversation");
        }

        private void GenerateBarks()
        {
            var conversationActor = actorNames[actorIndex];
            var prompt = GetLocationDescriptions() + 
                GetActorDescriptions(conversationActor, string.Empty, topic) +                
                $"Write {numBarks} barks spoken by {conversationActor} {verb} {topic}.";
            SubmitPrompt(prompt, AssistantPrompt, "Generating Barks");
        }

        void RefineConversation(string refinementPrompt)
        {
            SubmitEdit(refinementPrompt, "Refining Conversation");
        }

        private void AcceptConversation()
        {
            Debug.Log($"Adding conversation '{conversationTitle}' to database {database}.", database);

            var template = TemplateTools.LoadFromEditorPrefs();
            var actorName = actorNames[actorIndex];
            var conversantName = actorNames[conversantIndex];
            var conversation = AIConversationUtility.CreateConversation(database, template, conversationTitle,
                actorName, conversantName, ResultText);
            var startNode = conversation.dialogueEntries[0];
            EditorUtility.SetDirty(database);

            DialogueEditorWindow.OpenDialogueEntry(database, startNode.conversationID, startNode.id);
        }

        private void AcceptBarks()
        {
            Debug.Log($"Adding bark conversation '{conversationTitle}' to database {database}.", database);

            var template = TemplateTools.LoadFromEditorPrefs();
            var conversation = template.CreateConversation(template.GetNextConversationID(database), conversationTitle);
            database.conversations.Add(conversation);

            var actorName = actorNames[actorIndex];
            var conversantName = actorNames[conversantIndex];

            var actorID = database.GetActor(actorName).id;
            var conversantID = database.GetActor(conversantName).id;

            conversation.ActorID = actorID;
            conversation.ConversantID = conversantID;

            // START node: (Every conversation starts with a START node with ID 0)
            var startNode = template.CreateDialogueEntry(0, conversation.id, "START");
            startNode.ActorID = actorID;
            startNode.ConversantID = conversantID;
            startNode.Sequence = "None()"; // START node usually shouldn't play a sequence.
            conversation.dialogueEntries.Add(startNode);

            // Add barks:
            var lines = ResultText.Split('\n');
            foreach (var line in lines)
            {
                var s = line.Trim();
                if (string.IsNullOrWhiteSpace(s)) continue;
                var pos = s.IndexOf(". ");
                var text = s.Substring(pos + 2);
                if (text.StartsWith("\""))
                {
                    text = text.Substring(1, text.Length - 2);
                }

                var node = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
                node.ActorID = actorID;
                node.ConversantID = conversantID;
                node.DialogueText = text;
                conversation.dialogueEntries.Add(node);

                var link = new Link(conversation.id, startNode.id, conversation.id, node.id);
                startNode.outgoingLinks.Add(link);
            }
            EditorUtility.SetDirty(database);

            DialogueEditorWindow.OpenDialogueEntry(database, startNode.conversationID, startNode.id);
        }

    }
}

#endif
