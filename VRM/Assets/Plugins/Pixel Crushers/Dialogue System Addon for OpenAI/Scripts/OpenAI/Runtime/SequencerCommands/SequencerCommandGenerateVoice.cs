#if USE_OPENAI

using UnityEngine;
using PixelCrushers.DialogueSystem.OpenAIAddon;
using PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    using OpenAI = PixelCrushers.DialogueSystem.OpenAIAddon.OpenAI;

    /// <summary>
    /// Sequencer command: GenerateVoice()
    /// Generates and plays voice audio through OpenAI or ElevenLabs.
    /// If unable to generate audio, delays for the value of {{end}}.
    /// </summary>
    public class SequencerCommandGenerateVoice : SequencerCommand
    {
        private AudioClip audioClip = null;

        protected virtual void Awake()
        {
            // Get the speaker and dialogue text.
            var speakerInfo = DialogueManager.currentConversationState.subtitle.speakerInfo;
            var dialogueText = DialogueManager.currentConversationState.subtitle.formattedText.text;
            var actor = DialogueManager.masterDatabase.GetActor(speakerInfo.id);
            var voiceName = actor != null ? actor.LookupValue(DialogueSystemFields.Voice) : null;
            var voiceID = actor != null ? actor.LookupValue(DialogueSystemFields.VoiceID) : null;

            // Set a fallback in case voice generation fails.
            InvokeStopAfterDelay();

            // Validate settings.
            if (RuntimeAIConversationSettings.Instance == null)
            {
                Debug.LogWarning("Dialogue System: No Runtime AI Conversation Settings component in scene. Not playing audio.");
                return;
            }

            // Validate voice ID and name.
            if (string.IsNullOrEmpty(voiceName) || string.IsNullOrEmpty(voiceID))
            {
                Debug.LogWarning($"Dialogue System: No voice has been selected for {speakerInfo.nameInDatabase}. Not playing audio.");
                return;
            }

            // Handle OpenAI TTS if voiceID is "OpenAI".
            if (voiceID == "OpenAI")
            {
                CancelInvoke();
                var openAIVoice = System.Enum.Parse<Voices>(voiceName);
                OpenAI.SubmitVoiceGenerationAsync(RuntimeAIConversationSettings.Instance.APIKey,
                    TTSModel.TTSModel1HD, openAIVoice,
                    VoiceOutputFormat.MP3, 1, dialogueText, OnReceivedOpenAITextToSpeech);
            }
            // Handle ElevenLabs TTS if voiceID is not "OpenAI".
            else if (!string.IsNullOrEmpty(RuntimeAIConversationSettings.Instance.ElevenLabsApiKey))
            {
                CancelInvoke();
                ElevenLabsAPI.GetTextToSpeech(
                    RuntimeAIConversationSettings.Instance.ElevenLabsApiKey,
                    RuntimeAIConversationSettings.Instance.ElevenLabsModelId,
                    voiceName, voiceID,
                    0.5f, 0.8f, 0.3f, // Stability, Similarity Boost, and Style (0.3f for style)
                    dialogueText,
                    OnReceivedTextToSpeech // Ensure the callback is passed here
                );
            }
            else
            {
                Debug.LogWarning("Dialogue System: ElevenLabs API key is not set on Runtime AI Conversation Settings component. Not playing audio.");
            }
        }

        /// <summary>
        /// Stops the sequencer after the delay.
        /// </summary>
        protected virtual void InvokeStopAfterDelay()
        {
            Invoke(nameof(Stop), ConversationView.GetDefaultSubtitleDurationInSeconds(DialogueManager.currentConversationState.subtitle.formattedText.text));
        }

        /// <summary>
        /// Callback for OpenAI TTS.
        /// </summary>
        protected virtual void OnReceivedOpenAITextToSpeech(AudioClip audioClip, byte[] bytes)
        {
            OnReceivedTextToSpeech(audioClip);
        }

        /// <summary>
        /// Callback for ElevenLabs TTS.
        /// </summary>
        protected virtual void OnReceivedTextToSpeech(AudioClip audioClip)
        {
            this.audioClip = audioClip;
            if (audioClip == null)
            {
                Debug.LogWarning("Dialogue System: ElevenLabs did not return a valid audio clip. Not playing audio.");
                InvokeStopAfterDelay();
            }
            else
            {
                PlayAudio(audioClip);
            }
        }

        /// <summary>
        /// Plays the generated audio.
        /// </summary>
        protected virtual void PlayAudio(AudioClip audioClip)
        {
            var speaker = DialogueManager.currentConversationState.subtitle.speakerInfo.transform;
            var audioSource = SequencerTools.GetAudioSource(speaker);
            if (audioSource == null)
            {
                Debug.LogWarning($"Dialogue System: Unable to get or create an AudioSource on {speaker}. Not playing audio.");
                InvokeStopAfterDelay();
            }
            else
            {
                audioSource.clip = audioClip;
                audioSource.Play();
                Invoke(nameof(Stop), audioClip.length);
            }
        }

        /// <summary>
        /// Cleanup to destroy the audio clip.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (audioClip != null) Destroy(audioClip);
            audioClip = null;
        }

    }
}

#endif
