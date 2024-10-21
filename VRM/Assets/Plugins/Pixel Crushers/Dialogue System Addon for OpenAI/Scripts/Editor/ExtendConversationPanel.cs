// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Panel to extend a conversation from a specified dialogue entry.
    /// This feature is planned for a future release.
    /// </summary>
    public class ExtendConversationPanel : ActorTextGenerationPanel
    {

        public ExtendConversationPanel(string apiKey, DialogueDatabase database,
            Asset asset, DialogueEntry entry, Field field)
            : base(apiKey, database, asset, entry, field)
        {
        }

    }
}

#endif
