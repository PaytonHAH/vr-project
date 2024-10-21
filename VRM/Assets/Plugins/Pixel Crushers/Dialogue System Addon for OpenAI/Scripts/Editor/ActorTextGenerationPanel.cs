// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Base panel type for panels that generate text for actors.
    /// </summary>
    public abstract class ActorTextGenerationPanel : TextGenerationPanel
    {

        protected int actorIndex = 0;
        protected int conversantIndex = 1;
        protected List<int> extraActorIndices = new List<int>();
        protected string[] actorNames;
        protected bool includeLocations = false;

        private static GUIContent ExtraActorsLabel = new GUIContent("Extra Actors", "(Optional) Extra actors to involve in the conversation.");
        private static GUIContent IncludeLocationsLabel = new GUIContent("Include Locations", "Include Description fields of dialogue database's Locations in prompt.");

        public ActorTextGenerationPanel(string apiKey, DialogueDatabase database, 
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
            actorNames = (database != null)
                ? database.actors.Select(actor => actor.Name).ToArray()
                : new string[0];
        }

        protected void DrawActorAndConversant()
        {
            actorIndex = EditorGUILayout.Popup("Actor", actorIndex, actorNames);
            conversantIndex = EditorGUILayout.Popup("Conversant", conversantIndex, actorNames);
        }

        protected void DrawExtraActors()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(ExtraActorsLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", EditorStyles.miniButton))
            {
                extraActorIndices.Add(-1);
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < extraActorIndices.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                extraActorIndices[i] = EditorGUILayout.Popup("Also Involve", extraActorIndices[i], actorNames);
                if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    extraActorIndices.RemoveAt(i);
                    GUIUtility.ExitGUI();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        protected void DrawIncludeLocations()
        {
            includeLocations = EditorGUILayout.Toggle(IncludeLocationsLabel, includeLocations);
        }

        protected string GetActorDescriptions(string conversationActor, string conversationConversant, string topic)
        {
            return AIConversationUtility.GetActorDescriptions(database, conversationActor, conversationConversant, topic);
        }

        protected int GetActorID(string actorName, int defaultID)
        {
            return AIConversationUtility.GetActorID(database, actorName, defaultID);
        }

        protected string GetLocationDescriptions()
        {
            return includeLocations ? AIConversationUtility.GetLocationDescriptions(database) : string.Empty;
        }

    }
}

#endif
