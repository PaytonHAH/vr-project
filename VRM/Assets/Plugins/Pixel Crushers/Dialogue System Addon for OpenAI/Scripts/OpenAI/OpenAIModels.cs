// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    public enum ModelType { Completion, Chat, Edit, Transcription }

    public class Model
    {
        public string Name { get; set; }
        public ModelType ModelType { get; private set; }
        public int MaxTokens { get; private set; }

        public Model(string name, ModelType modelType, int maxTokens)
        {
            Name = name;
            ModelType = modelType;
            MaxTokens = maxTokens;
        }

        /// <summary>
        /// Our affordable and intelligent small model for fast, lightweight tasks.
        /// </summary>
        public static Model GPT4o_mini { get; } = new Model("gpt-4o-mini", ModelType.Chat, 128000);

        /// <summary>
        /// The latest GPT-4 model with improved instruction following, JSON mode, reproducible outputs, parallel function calling, and more. Returns a maximum of 4,096 output tokens.
        /// </summary>
        public static Model GPT4o { get; } = new Model("gpt-4o", ModelType.Chat, 128000);

        /// <summary>
        /// The latest GPT-4 model with improved instruction following, JSON mode, reproducible outputs, parallel function calling, and more. Returns a maximum of 4,096 output tokens.
        /// </summary>
        public static Model GPT4_Turbo { get; } = new Model("gpt-4-turbo", ModelType.Chat, 128000);

        /// <summary>
        /// More capable than any GPT-3.5 model, able to do more complex tasks, and optimized for chat.
        /// </summary>
        public static Model GPT4 { get; } = new Model("gpt-4", ModelType.Chat, 8192);

        /// <summary>
        /// Same capabilities as the base gpt-4 mode but with 4x the context length. Will be updated with our latest model iteration.
        /// </summary>
        public static Model GPT4_32K { get; } = new Model("gpt-4-32k", ModelType.Chat, 32768);

        /// <summary>
        /// Most capable GPT-3.5 model and optimized for chat at 1/10th the cost of text-davinci-003. Will be updated with our latest model iteration.
        /// </summary>
        public static Model GPT3_5_Turbo { get; } = new Model("gpt-3.5-turbo", ModelType.Chat, 4096);

        /// <summary>
        /// Like GPT-3.5 Turbo but with 4x the context.
        /// </summary>
        public static Model GPT3_5_Turbo_16K { get; } = new Model("gpt-3.5-turbo-16k", ModelType.Chat, 16384);

        /// <summary>
        /// Can do any language task with better quality, longer output, and consistent instruction-following than the curie, babbage, or ada models. Also supports inserting completions within text.
        /// </summary>
        public static Model Davinci_003 { get; } = new Model("text-davinci-003", ModelType.Completion, 4097);

        /// <summary>
        /// Used for Edits.
        /// </summary>
        public static Model Davinci_Edit_001 { get; } = new Model("text-davinci-edit-001", ModelType.Edit, 4097);

        /// <summary>
        /// Very capable, faster and lower cost than Davinci.
        /// </summary>
        public static Model Curie { get; } = new Model("text-curie-001", ModelType.Completion, 2049);

        /// <summary>
        /// Capable of straightforward tasks, very fast, and lower cost.
        /// </summary>
        public static Model Babbage { get; } = new Model("text-babbage-001", ModelType.Completion, 2049);

        /// <summary>
        /// Capable of very simple tasks, usually the fastest model in the GPT-3 series, and lowest cost.
        /// </summary>
        public static Model Ada { get; } = new Model("text-ada-001", ModelType.Completion, 2049);

        /// <summary>
        /// Model for speech to text transcription.
        /// </summary>
        public static Model Whisper_1 { get; } = new Model("whisper-1", ModelType.Transcription, 65536);
    }

}

#endif
