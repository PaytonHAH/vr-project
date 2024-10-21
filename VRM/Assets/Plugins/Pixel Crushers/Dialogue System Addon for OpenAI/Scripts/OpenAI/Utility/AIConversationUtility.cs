// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Diagnostics;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Conversation utility methods for OpenAI addon.
    /// </summary>
    public static class AIConversationUtility
    {

        /// <summary>
        /// Returns a string containing the Description, Knowledge, and Goals fields
        /// of an actor, conversant, and any other actors mentioned in a topic string. 
        /// </summary>
        public static string GetActorDescriptions(DialogueDatabase database,
            string conversationActor, string conversationConversant, string topic)
        {
            var prompt = string.Empty;
            foreach (var actor in database.actors)
            {
                var actorName = actor.Name;
                var actorDisplayName = actor.LookupValue("Display Name");
                if (actorName == conversationActor || actorName == conversationConversant ||
                    topic.Contains(actorName) ||
                    (!string.IsNullOrEmpty(actorDisplayName) && topic.Contains(actorDisplayName)))
                {
                    prompt = AddAssetInfo(prompt, actor, DialogueSystemFields.Description);
                    prompt = AddAssetInfo(prompt, actor, DialogueSystemFields.Knowledge);
                    prompt = AddAssetInfo(prompt, actor, DialogueSystemFields.Goals);
                }
            }
            return prompt;
        }

        public static string AddAssetInfo(string prompt, Asset asset, string fieldTitle)
        {
            var fieldValue = string.Empty;
            if (Application.isPlaying)
            {
                if (asset is Actor) fieldValue = DialogueLua.GetActorField(asset.Name, fieldTitle).asString;
                if (asset is Location) fieldValue = DialogueLua.GetLocationField(asset.Name, fieldTitle).asString;
            }
            if (string.IsNullOrEmpty(fieldValue))
            {
                fieldValue = asset.LookupValue(fieldTitle);
            }
            if (string.IsNullOrEmpty(fieldValue))
            {
                return prompt;
            }
            else
            {
                var endsWithPunctuation = fieldValue.TrimEnd().EndsWith(".");
                return fieldValue + (endsWithPunctuation ? " " : ". ");
            }
        }

        public static int GetActorID(DialogueDatabase database, string actorName, int defaultID)
        {
            foreach (var actor in database.actors)
            {
                if (actorName == actor.Name) return actor.id;
            }
            return defaultID;
        }

        /// <summary>
        /// Returns string containing descriptions of all Locations in database.
        /// </summary>
        public static string GetLocationDescriptions(DialogueDatabase database)
        {
            var prompt = string.Empty;
            foreach (var location in database.locations)
            {
                prompt = AddAssetInfo(prompt, location, DialogueSystemFields.Description);
            }
            return prompt;
        }

        /// <summary>
        /// Creates a conversation from OpenAI-returned content, and adds it to a database.
        /// </summary>
        public static Conversation CreateConversation(DialogueDatabase database, Template template,
            string conversationTitle, string actorName, string conversantName, string fullConversationText,
            int forceConversationID = -1)
        {
            if (DialogueDebug.logInfo) UnityEngine.Debug.Log($"Dialogue System: OpenAI generated conversation:\n{fullConversationText}");

            var conversationID = (forceConversationID != -1) ? forceConversationID : template.GetNextConversationID(database);
            var conversation = template.CreateConversation(conversationID, conversationTitle);
            database.conversations.Add(conversation);

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

            var prevNode = startNode;

            // Add lines:
            var lines = fullConversationText.Split('\n');
            foreach (var line in lines)
            {
                var text = line.Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;
                string speakerName = AITextUtility.ExtractSpeaker(ref text, database);

                //var pos = s.IndexOf(": ");
                //var speakerName = (pos >= 0) ? s.Substring(0, pos) : "NPC";
                //if (database.GetActor(speakerName) == null)
                //{
                //    // If no actor has this name, the colon is probably part of the text.
                //    pos = -1;
                //    speakerName = "NPC";
                //}
                //var text = AITextUtility.RemoveSurroundingQuotes((pos >= 0) ? s.Substring(pos + 2): s);

                var speakerID = (speakerName == actorName) ? actorID
                    : (speakerName == conversantName) ? conversantID
                    : AIConversationUtility.GetActorID(database, speakerName, conversantID);
                var listenerID = (speakerName == actorName) ? conversantID : actorID;

                var node = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
                node.ActorID = speakerID;
                node.ConversantID = listenerID;
                node.DialogueText = text;
                conversation.dialogueEntries.Add(node);

                var link = new Link(conversation.id, prevNode.id, conversation.id, node.id);
                prevNode.outgoingLinks.Add(link);

                prevNode = node;
            }

            return conversation;
        }
    }

}

#endif
