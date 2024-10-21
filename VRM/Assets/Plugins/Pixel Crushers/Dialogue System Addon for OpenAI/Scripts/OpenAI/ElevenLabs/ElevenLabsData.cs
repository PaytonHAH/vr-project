// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.OpenAIAddon.ElevenLabs
{

   

    [Serializable]
    public class VoiceData
    {
        public string voice_id;
        public string name;
        public List<string> samples;
        public string category;
        public VoiceFineTuningData fine_tuning;
        //public Dictionary<string, string> labels;
        //public object description; //"description": null,
        public string preview_url;
        public List<string> available_for_tiers;
        public VoiceSettings settings;
    }

    [Serializable]
    public class VoiceFineTuningData
    {
        //public object model_id; //"model_id": null,
        public bool is_allowed_to_fine_tune;
        public bool fine_tuning_requested;
        public string finetuning_state;
        //public object verification_attempts; //"verification_attempts": null,
        //public object verification_failures; //"verification_failures": [],
        public int verification_attempts_count;
        //public object slice_ids;  //"slice_ids": null
    }

    [Serializable]
    public class VoiceSettings
    {
        public float stability;
        public float similarity_boost;

        public VoiceSettings() { }

        public VoiceSettings(float stability, float similarity_boost)
        {
            this.stability = stability;
            this.similarity_boost = similarity_boost;
        }
    }

    [Serializable]
    public class TextToSpeechRequest
    {
        public string model_id;
        public string text;
        public VoiceSettings voice_settings;

        public TextToSpeechRequest(string text, VoiceSettings voice_settings)
        {
            this.model_id = ElevenLabsAPI.GetDefaultModelId();
            this.text = text;
            this.voice_settings = voice_settings;
        }

        public TextToSpeechRequest(string model_id, string text, VoiceSettings voice_settings)
        {
            this.model_id = model_id;
            this.text = text;
            this.voice_settings = voice_settings;
        }
    }

}

#endif
