// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{

    public static class EditorAudioUtility
    {

        /// <summary>
        /// Plays an audio clip in the editor.
        /// </summary>
        public static void PlayAudioClip(AudioClip clip)
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null,  new object[] { clip, 0, false } );
        }

    }
}

#endif
