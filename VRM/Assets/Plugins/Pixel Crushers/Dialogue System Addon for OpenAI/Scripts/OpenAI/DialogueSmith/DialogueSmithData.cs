//--- Dialogue Smith has discontinued their service.

//// Copyright (c) Pixel Crushers. All rights reserved.

//#if USE_OPENAI

//using System;
//using System.Collections.Generic;

//namespace PixelCrushers.DialogueSystem.OpenAIAddon.DialogueSmith
//{

//    [Serializable]
//    public class BranchingDialogueData
//    {
//        public UserMessage user_message;
//    }

//    [Serializable]
//    public class UserMessage
//    {
//        public List<CharacterData> characters;
//        public List<ConversationData> conversation;

//    }

//    [Serializable]
//    public class CharacterData
//    {
//        public int id;
//        public string name;
//        public string character_sheet;
//    }

//    [Serializable]
//    public class ConversationData
//    {
//        public string context;
//        public List<Node> nodes;
//    }

//    [Serializable]
//    public class Node
//    {
//        public int id;
//        public string title;
//        public int character;
//        public List<int> goto_next;
//        public List<string> text;

//        [NonSerialized] public List<bool> accept;
//    }

//}

//#endif
