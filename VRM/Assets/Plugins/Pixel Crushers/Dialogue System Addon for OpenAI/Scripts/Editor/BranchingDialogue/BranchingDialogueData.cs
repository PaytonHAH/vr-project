// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    public class BranchingDialogueData
    {
        public string context;
        public List<Node> nodes;
    }

    public class Node
    {
        public int entryID;
        public string title;
        public string actorName;
        public List<DialogueEntry> pathToNode;
        public List<string> text;
        public List<bool> accept;
    }

}

#endif
