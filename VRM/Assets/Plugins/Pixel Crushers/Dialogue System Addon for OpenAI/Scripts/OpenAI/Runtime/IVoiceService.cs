// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Interface that provides text to speech service.
    /// </summary>
    public interface IVoiceService
    {

        /// <summary>
        /// Name of the service, used in logging.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Sequencer command to generate text to speech.
        /// </summary>
        public string SequencerCommand { get; }

        /// <summary>
        /// Generate text to speech audio clip and send it in the callback.
        /// </summary>
        /// <param name="voiceName">Voice name to use.</param>
        /// <param name="voiceID">Voice ID to use. Service may or may not use this.</param>
        /// <param name="text">Text to convert to audio speech.</param>
        /// <param name="callback">Pass resulting audio clip to this. May be called with null on failure.</param>
        public void GenerateTextToSpeech(string voiceName, string voiceID,
            string text, Action<AudioClip> callback);

    }
}

#endif
