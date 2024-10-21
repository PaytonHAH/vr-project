// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    [Serializable]
    public class FineTunedModelInfo
    {
        public List<string> models = new List<string>();
        public int lastIndex = 0;
    }

}

#endif