using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using TMPro;
using System.Net;
using UnityEngine.Events;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.Serialization;

namespace Xandimmersion.Geppetto
{
    public class GeppettoEditor : EditorWindow
    {

        public class Response
        {
            public string tts { get; set; }
            public List<List<object>> phonemes_list { get; set; }

        }

        //What is shown
        public Dictionary<string, string[]> Options = new Dictionary<string, string[]>
        {
            //{ "English (US)", new string[] { "Ada (woman)", "Alioth (young man)", "Baldur (man)", "David (man)", "Dulhan (man)", "Oriane (young woman)", "Socrates (old man)", "Tom (man)", "Zenaya (woman)", "Mary", "Linda", "Patricia", "Barbara", "Susan", "Paul", "Michael", "William", "Thomas", } },
            //{ "French", new string[] { "Auriane (woman)", "Philippe (man)", "Capucine", "Alix", "Arnaud", } },

            { "English (US)", new string[] { "Ada (premium voice)", "Alioth (premium voice)", "Baldur (premium voice)", "David (premium voice)", "Dulhan (premium voice)", "Oriane (premium voice)", "Socrates (premium voice)", "Tom (premium voice)", "Zenaya (premium voice)", "Mary", "Linda", "Patricia", "Barbara", "Susan", "Paul", "Michael", "William", "Thomas", } },
            { "French", new string[] { "Auriane (premium voice)", "Philippe (premium voice)", "Capucine", "Alix", "Arnaud", } },

            { "Arabic",new string[] { "Farah", } },
            { "Chinese (Mandarin)",new string[] { "Daiyu", } },
            { "Danish",new string[] { "Emma", "Oscar", } },
            { "Dutch",new string[] { "Anke","Adriaan", } },
            { "English (Australian)",new string[] { "Mia","Grace","Jack", } },
            { "English (British)",new string[] { "Charlotte","Sophia","Elijah", } },
            { "English (Indian)",new string[] { "Advika","Onkar", } },
            { "English (New Zealand)",new string[] { "Ruby", } },
            { "English (South African)",new string[] { "Elna", } },
            //{ "English (US)",new string[] { "Mary","Linda","Patricia","Barbara","Susan","Paul","Michael","William","Thomas", } },
            { "English (Welsh)",new string[] { "Aeron", } },
            { "French (Canadian)",new string[] { "Stephanie","Celine", } },
            { "German",new string[] { "Maria","Theresa","Felix", } },
            { "Hindi",new string[] { "Chhaya", } },
            { "Icelandic",new string[] { "Anna","Sigriour", } },
            { "Italian",new string[] { "Gabriella","Bella","Lorenzo", } },
            { "Japanese",new string[] { "Rika","Tanaka", } },
            { "Korean",new string[] { "Ji-Ho", } },
            { "Norwegian",new string[] { "Camilla", } },
            { "Polish",new string[] { "Katarzyna","Malgorzata","Piotr","Jan", } },
            { "Portuguese (Brazilian)",new string[] { "Tabata","Juliana","Pedro", } },
            { "Portuguese (European)",new string[] { "Pati","Adriano", } },
            { "Romanian",new string[] { "Alexandra", } },
            { "Russian",new string[] { "Inessa","Viktor", } },
            { "Spanish (European)",new string[] { "Francisca","Margarita","Mateo", } },
            { "Spanish (Mexican)",new string[] { "Leticia", } },
            { "Spanish (US)",new string[] { "Josefina","Rosa","Miguel", } },
            { "Swedish",new string[] { "Eva", } },
            { "Turkish",new string[] { "Mesut", } },
            { "Welsh",new string[] { "Angharad", } }
        };

        public string[] LanguesRT = new string[]
        {
            "Arabic", "Chinese (Mandarin)", "Danish", "Dutch", "English (Australian)", "English (British)", "English (Indian)", "English (New Zealand)", "English (South African)", "English (US)", "English (Welsh)", "French", "French (Canadian)", "German", "Hindi", "Icelandic", "Italian", "Japanese", "Korean", "Norwegian", "Polish", "Portuguese (Brazilian)", "Portuguese (European)", "Romanian", "Russian", "Spanish (European)", "Spanish (Mexican)", "Spanish (US)", "Swedish", "Turkish", "Welsh",
        };

        //What is send to the Post request
        public List<List<string>> APIOptions = new List<List<string>>
        {
            //new List<string> { "Ada", "Alioth", "Baldur", "David", "Dulhan", "Oriane", "Socrates", "Tom", "Zenaya","Mary", "Linda", "Patricia", "Barbara", "Susan", "Paul", "Michael", "William", "Thomas",},
            //new List<string> { "Auriane", "Philippe", "Capucine", "Alix", "Arnaud",},
            //new List<string> { "Farah",},

            new List<string> { "Farah",} ,
            new List<string>{ "Daiyu",} ,
            new List<string>{ "Emma", "Oscar",},
            new List<string>{ "Anke","Adriaan", },
            new List<string>{ "Mia","Grace","Jack",},
            new List<string>{ "Charlotte","Sophia","Elijah",},
            new List<string>{ "Advika","Onkar",},
            new List<string>{ "Ruby",},
            new List<string>{ "Elna",},
            new List<string> { "Ada", "Alioth", "Baldur", "David", "Dulhan", "Oriane", "Socrates", "Tom", "Zenaya","Mary", "Linda", "Patricia", "Barbara", "Susan", "Paul", "Michael", "William", "Thomas",},
            new List<string>{ "Aeron",},
            new List<string> { "Auriane", "Philippe", "Capucine", "Alix", "Arnaud",},
            new List<string>{ "Stephanie","Celine",},
            new List<string> { "Maria","Theresa","Felix",},
            new List<string>{ "Chhaya",},
            new List<string>{ "Anna","Sigriour",},
            new List<string>{ "Gabriella","Bella","Lorenzo",},
            new List<string>{ "Rika","Tanaka",},
            new List<string>{ "Ji-Ho",},
            new List<string>{ "Camilla",},
            new List<string>{ "Katarzyna","Malgorzata","Piotr","Jan",},
            new List<string>{ "Tabata","Juliana","Pedro",},
            new List<string> { "Pati","Adriano",},
            new List<string>{ "Alexandra",},
            new List<string>{ "Inessa","Viktor",},
            new List<string>{ "Francisca","Margarita","Mateo",},
            new List<string>{ "Leticia",},
            new List<string> { "Josefina","Rosa","Miguel",},
            new List<string>{ "Eva",},
            new List<string>{ "Mesut", },
            new List<string> { "Angharad",}

        };

        public Response response;
        
        //Value for API POST Request
        private string apiPath = "https://new-convinceme-api.xandimmersion.com/geppetto-get-phonemes/";
        public string text;
        private int max_ampl;
        private int min_ampl;
        private float timeDelay;
        private int silence_threshold;
        private int silence_time;
        public byte[] fileContent;
        public string[] Langues = new string[]
        {
            "en-US", "fr-FR", "de-DE", "es-ES", "it-IT", "pt-PT", "zh (cmn-Hans-CN)", "ja-JP", "sv-SE", "fi-FI", "no-NO", "da-DK",
        };
        public int langue;

        public AnimationUtility.TangentMode tangentMode = AnimationUtility.TangentMode.Free;
        public ParametersController.TransitionFunction transitionMode = ParametersController.TransitionFunction.None;
        
        public int transitionPrecision = 30;


        public UnityEngine.Object csvFile;
        public string clipName;
        public string savePath = "None";
        public Keyframe[] ks;
        public List<Keyframe> ksTest;
        public List<Keyframe> ksTestDown;
        public string[] bs_name;
        string bs_nameTest;
        AnimationClip clipTest;
        AnimationClip tempClip;
        public AnimationClip anim_obj;
        AnimationCurve curveTest;
        AnimationCurve curveTestDown;
        UnityWebRequest www;
        public string animationName;

        public bool useVoiceRecord;
        public bool useTextOnly;
        public bool useVoiceAndText;
        public bool doesAnimationExist;
        public bool doesAnimationNotExist;
        public string voiceRecordPath;

        AnimationClip newClip;
        AnimationClip anim;


        public int meshList = 1;

        public List<GameObject> blendshapes = new List<GameObject>();

        //Blink
        public bool isBlink;
        public bool isOneEye;
        public GameObject blinkMesh;
        string blinkBlendShapeName;
        string blinkBlendShapeName2;
        string blinkFullName;
        string blinkFullName2;
        public List<Keyframe> blinkKeyframes;
        public float blinkInterval = 2f;
        public float blinkDelta = 0.1f;


        //Tab
        private int selectedMainTab = 0;
        string[] mainTabs = { "Animation Clip"};

        Vector2 scrollPos = new Vector2();
        Vector2 scrollPos2 = Vector2.zero;
        Vector2 scrollPos3 = Vector2.zero;


        //RunTime
        public GameObject RTGameObject;
        public string template_live_script_path = "Assets/Geppetto/DoNotMoveOrChange/TemplateGeppettoLive.cs";
        public string save_path = null;
        private string text_error = null;
        public string prefab_name = null;
        public string script_name = null;

        public int Selected_O = 0;
        public int Selected_L = 9;

        List<(string, double, double)> myPhonemesList = new List<(string, double, double)>()
        {


        };


        Dictionary<string, List<(float, float)>> phonemDictionary = new Dictionary<string, List<(float, float)>>()
        {
            //{ "viseme_aa",new List<(float,float)> {(1,1),(1,2)}   },
            //{ "viseme_aa",new List<float> {1,2}   },
        };

        Dictionary<string, List<Keyframe>> keyframesDictionary = new Dictionary<string, List<Keyframe>>()
        {
            //{ "viseme_aa",new List<Keyframe> { new Keyframe(1, 20), new Keyframe(1, 10)}   },
            //{ "viseme_nn",new List<Keyframe> { new Keyframe(5, 20), new Keyframe(7, 10)}   },
        };

        //List<(string, double)> listTest = new List<(string, double)>()
        //{
        ////("HH", 0.058), ("AH", 0.16140000000000004), ("L", 0.2648000000000001), ("OW", 0.3682000000000001), ("AY", 0.47160000000000013), ("W", 0.5750000000000002), ("AA", 0.6784000000000002), ("N", 0.7818000000000003), ("T", 0.8852000000000003), ("T", 0.9886000000000004), ("UW", 1.0920000000000003), ("D", 1.1954000000000005), ("AE", 1.2988000000000004), ("N", 1.4022000000000006), ("S", 1.5056000000000005), ("W", 1.6090000000000007), ("IH", 1.7124000000000006), ("DH", 1.8158000000000005), ("Y", 1.9192000000000007), ("UW", 2.0226000000000006), ("PAUSE", 2.184)
        //("viseme_kk", 0.058), ("viseme_O", 0.16140000000000004), ("viseme_RR", 0.2648000000000001), ("viseme_O", 0.3682000000000001), ("viseme_E", 0.47160000000000013), ("viseme_U", 0.5750000000000002), ("viseme_U", 0.6784000000000002), ("viseme_nn", 0.7818000000000003), ("viseme_DD", 0.8852000000000003), ("viseme_DD", 0.9886000000000004), ("viseme_U", 1.0920000000000003), ("viseme_DD", 1.1954000000000005), ("viseme_aa", 1.2988000000000004), ("viseme_nn", 1.4022000000000006), ("viseme_SS", 1.5056000000000005), ("viseme_U", 1.6090000000000007), ("viseme_I", 1.7124000000000006), ("viseme_TH", 1.8158000000000005), ("viseme_I", 1.9192000000000007), ("viseme_U", 2.0226000000000006), ("PAUSE", 2.184)
        ////("viseme_kk", 0.058), ("viseme_O", 0.16140000000000004)
        //};

        private void Awake()
        {
            blendshapes.Add(null);
            max_ampl = 100;
            min_ampl = 0;
            doesAnimationExist = true;
            doesAnimationNotExist = false;
            useVoiceRecord = false;
            silence_threshold = -60;
            silence_time = 200;


        }

        public static AnimationCurve MergeAnimationCurves(AnimationCurve curve1, AnimationCurve curve2)
        {
            AnimationCurve newCurve = new AnimationCurve();

            foreach (Keyframe keyframe in curve1.keys)
            {
                newCurve.AddKey(keyframe);
            }

            foreach (Keyframe keyframe in curve2.keys)
            {
                newCurve.AddKey(keyframe);
            }

            return newCurve;
        }


        [MenuItem("Window/Geppetto", false, 1)]
        public static void ShowWindow()
        {
            GetWindow<GeppettoEditor>("Geppetto");
        }

        public Response CallGeppetoAPI()
        {
            www = null;
            WWWForm form = new WWWForm();
            //form.AddField("format", "viseme"); //Format for RPM avatar
            form.AddField("format", "default");
            form.AddField("min_ampl", min_ampl.ToString());
            form.AddField("max_ampl", max_ampl.ToString());
            form.AddField("silence_threshold", silence_threshold.ToString());
            form.AddField("silence_time", silence_time.ToString());
            if (selectedMainTab == 1)
            {
                form.AddField("langue", Langues[langue]);
            }
            
            // IF Audio
            if (useVoiceRecord)
            {
                //  If savepath.EndWith(".wav")            
                form.AddBinaryData("sound", fileContent, "filename", "audio/x-wav");

                //  If savepath.EndWith(".mp3")            
                //form.AddBinaryData("sound", fileContent, "filename", "audio/mp3");

                //If Audio AND Text
                if (useVoiceAndText)
                {
                    form.AddField("text", text);

                }
            }
            // IF only Text
            else
            {
                form.AddField("text", text);
                //form.AddField("voice", "Mackowsky");
            }

            using (www = UnityWebRequest.Post(apiPath, form))
            {

                www.SetRequestHeader("Authorization", "Api-Key DiFfIY8V.OtXo0aj6l1oPhceGRGG3ndWW7b8OUeVt"); //

                www.SendWebRequest();
                                
                while (!www.isDone)
                {
                    continue;
                }

                if (www.isHttpError)
                {
                    UnityEngine.Debug.LogError("Error While Sending: " + www.error);

                }
                else
                {

                    response = JsonConvert.DeserializeObject<Response>(www.downloadHandler.text);
                    
                    return response;
                }
            }
            return null;

        }

        private string getNextFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            int i = 0;
            //We loop until we create a filename that doesnt exist yet
            while (File.Exists(fileName))
            {
                if (i == 0)
                    fileName = fileName.Replace(extension, "(" + ++i + ")" + extension);
                else
                    fileName = fileName.Replace("(" + i + ")" + extension, "(" + ++i + ")" + extension);

            }

            return fileName;
        }


        static void lineChanger(string newText, string templateFileName, int line_to_edit, string newFileName)
        {
            string[] arrLine = File.ReadAllLines(templateFileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(newFileName, arrLine);
        }

        public static void CreateGeppettoLive(string templateFilePath, string newFilePath, string csvFilePath, string script_name, string voice)
        {
            UnityEngine.Debug.Log("CreateGeppettoLive");
            lineChanger($"    public class {script_name}GeppettoLive : MonoBehaviour", templateFilePath, 15, newFilePath);
            lineChanger($"        private string csvFilePath = \"{csvFilePath}\";", newFilePath, 30, newFilePath);
            lineChanger($"        private string voice = \"{voice}\";", newFilePath, 31, newFilePath);

            AssetDatabase.Refresh(); //Make known the script by Unity
            
        }

        public string ReadGlossary(string glossary, string phonem)
        {

            string[] data = glossary.Split(new string[] { ",", "\n" }, StringSplitOptions.None);

            for (int i = 0; i <= data.Length - 1; i += 2)
            {
                if (data[i] == phonem)
                {
                    //Debug.Log("Data = Phonem -> Data: " + data[i] + " Data +1: " + data[i + 1]);
                    return data[i + 1].Trim();
                }
                
            }
            return null;

        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            selectedMainTab = GUILayout.Toolbar(selectedMainTab, mainTabs, GUILayout.Width(600));

            if (selectedMainTab == 0) // 0 = Generation
            {
                GUILayout.Space(20);

                TabGeppettoGeneration();
            }
            

            EditorGUILayout.EndScrollView();

        }

        void TabGeppettoGeneration()
        {
            GUILayout.BeginVertical();
            scrollPos2 = GUILayout.BeginScrollView(scrollPos2);
            //clipName = EditorGUILayout.TextField("File Name", text, GUI.skin.textArea);
            GUILayout.Label("Create Animation Clip base on the Blendshape of your 3D Model. \n" +
                "You can find those Blendshapes under the SkinnedMeshRenderer component of your 3D model ");

            GUILayout.Space(20);


            //Button for New or Existing animation
            GUILayout.BeginHorizontal();
            useTextOnly = EditorGUILayout.ToggleLeft("Text only", !useVoiceRecord);
            useVoiceRecord = EditorGUILayout.ToggleLeft("Voice record", !useTextOnly);
            GUILayout.EndHorizontal();
            if (useVoiceRecord)
            {
                GUILayout.BeginHorizontal();
                //voiceRecordPath = EditorGUILayout.ObjectField("Audio for the lyp-sync", voiceRecordPath, typeof(AudioClip), true) as AudioClip;                      //MODIFICATION A FAIRE
                if (GUILayout.Button("Select audio file", GUILayout.Width(300)))
                {
                
                    voiceRecordPath = EditorUtility.OpenFilePanel("Audio for the lyp-sync", "", "wav");
                    fileContent = File.ReadAllBytes(voiceRecordPath);

                }

                GUILayout.Label(" " + voiceRecordPath);
                GUILayout.EndHorizontal();
                langue = EditorGUILayout.Popup("Language of the record", langue, Langues, GUILayout.Width(300)); // GUILayout.ExpandWidth(true)
                useVoiceAndText = EditorGUILayout.ToggleLeft("Use the script of the voice record for more accuracy", useVoiceAndText); 
                if (useVoiceAndText)
                {
                    text = EditorGUILayout.TextField("Script of your voice record", text, GUI.skin.textArea, GUILayout.Height(100));
                    GUILayout.Space(10f);
                }
                else
                {
                    GUILayout.Space(112f);
                }

            }

            else
            {
                //Text to Animation
                voiceRecordPath = null;
                text = EditorGUILayout.TextField("Text for the lyp-sinc", text, GUI.skin.textArea, GUILayout.Height(100));
                GUILayout.Space(71f);

            }






            //Glossary
            csvFile = EditorGUILayout.ObjectField("Phoneme Glossary", csvFile, typeof(TextAsset), true);
            
            //Mesh with Blendshapes
            if (meshList > 0)
            {
                for (int i = 0; i < meshList; i++)
                {
                    GUILayout.BeginHorizontal();
                    //blendshapes[i] = EditorGUILayout.ObjectField("Mesh to get blendshapes", blendshapes[i], typeof(GameObject), true) as GameObject;
                    blendshapes[i] = EditorGUILayout.ObjectField("Mesh with blendshapes", blendshapes[i], typeof(GameObject), true) as GameObject;

                    if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        if (meshList > 1)
                        {
                            meshList--;
                            blendshapes.RemoveAt(i);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            
            
            
            //Button to add mesh
            if (GUILayout.Button("Add mesh"))
            {
                meshList++;
                blendshapes.Add(null);
            }
            


            isBlink = EditorGUILayout.ToggleLeft("Add blink to animation", isBlink);

            if (isBlink)
            {
                GUILayout.BeginHorizontal();
                blinkMesh = EditorGUILayout.ObjectField("Blink Mesh", blinkMesh, typeof(GameObject), true) as GameObject;
                GUILayout.Space(10f);
                isOneEye = EditorGUILayout.ToggleLeft("One BlendShape for both eyes", isOneEye);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (isOneEye)
                {
                    blinkBlendShapeName = EditorGUILayout.TextField(new GUIContent("BlendShape's name: "), blinkBlendShapeName, GUI.skin.textArea);
                }

                else 
                { 
                    blinkBlendShapeName = EditorGUILayout.TextField(new GUIContent("BlendShape's name Left: "), blinkBlendShapeName, GUI.skin.textArea);
                    blinkBlendShapeName2 = EditorGUILayout.TextField(new GUIContent("BlendShape's name Right: "), blinkBlendShapeName2, GUI.skin.textArea);
                }
                GUILayout.EndHorizontal();
                

            }
            else
            {
                GUILayout.Space(40f);
            }


            GUILayout.Space(10f);

            GUILayout.Label("Silence parameters in the audio, play with them for better results (-60 and 200 if unknown) : ");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Silence threshold");
            silence_threshold = EditorGUILayout.IntSlider(silence_threshold, -80, -10);
            GUILayout.Label("Silence duration");
            silence_time = EditorGUILayout.IntSlider(silence_time, 1000, 10);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            GUILayout.Label("Audio start Time (seconde)");
            timeDelay = EditorGUILayout.FloatField(timeDelay, GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.7f));

            GUILayout.Space(10f);


            GUILayout.Label("Range of Blendshape animation : ");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min");

            min_ampl = EditorGUILayout.IntSlider(min_ampl, 0, max_ampl);
            GUILayout.Label("Max");
            max_ampl = EditorGUILayout.IntSlider(max_ampl, min_ampl, 100);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            //Button for New or Existing animation
            GUILayout.BeginHorizontal();
            doesAnimationExist = EditorGUILayout.ToggleLeft("Add to existing Animation Clip", !doesAnimationNotExist);
            doesAnimationNotExist = EditorGUILayout.ToggleLeft("Create new Animation Clip", !doesAnimationExist);
            GUILayout.EndHorizontal();

            if (doesAnimationExist)
            {
                anim_obj = EditorGUILayout.ObjectField("Animation Clip", anim_obj, typeof(AnimationClip), true) as AnimationClip;
                GUILayout.Space(31f);

            }

            if (doesAnimationNotExist)
            {
                animationName = EditorGUILayout.TextField("New animation clip name", animationName, GUI.skin.textArea, GUILayout.MinWidth(352));

                // Choose saving path for anim clip
                GUILayout.BeginHorizontal();
                if (savePath == null || savePath == "None")
                {
                    if (GUILayout.Button("Saving Path", GUILayout.Width(200)))
                    {
                        savePath = EditorUtility.OpenFolderPanel("Select output folder", "", "");
                        //From absolute to relative project path
                        if (savePath.StartsWith(Application.dataPath))
                        {
                            savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            Debug.LogError("Saving Path must be in the current Unity project");
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Change Saving Path", GUILayout.Width(200)))
                    {
                        savePath = EditorUtility.OpenFolderPanel("Select output folder", "", "");
                        //From absolute to relative project path
                        if (savePath.StartsWith(Application.dataPath))
                        {
                            savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            Debug.LogError("Saving Path must be in the current Unity project");
                        }
                    }
                }
                
                
                GUILayout.Label(" " + savePath);
                GUILayout.EndHorizontal();
                GUILayout.Space(10f);
            }
            
            GUILayout.BeginHorizontal();
            // Curves type
            tangentMode =
                (AnimationUtility.TangentMode)EditorGUILayout.Popup("Curves type",(int)tangentMode, System.Enum.GetNames(typeof(AnimationUtility.TangentMode)), GUILayout.Width(300));
            
            GUILayout.Space(30);
            // Transition curve between keyframes
            transitionMode = (ParametersController.TransitionFunction)EditorGUILayout.Popup("Transition curve",
                (int)transitionMode, Enum.GetNames(typeof(ParametersController.TransitionFunction)),
                GUILayout.Width(350));
            
            GUILayout.Space(30);
            //Transition curve precision
            transitionPrecision = EditorGUILayout.IntSlider("Transition rate", transitionPrecision, 1, 60,
                GUILayout.Width(300));
                
            
            
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            // Button to Generate anim clip
            if (GUILayout.Button("Generate", GUILayout.MinHeight(40)))
            {
                //Get the phonem list from API
                response = CallGeppetoAPI();
                if (response == null)
                {
                    Debug.LogError("Response NULL");
                    return;
                }
                
                keyframesDictionary.Clear();
                phonemDictionary.Clear();

                for (int i = 0; i < response.phonemes_list.Count; i++)
                {
                    string phonem = (string)response.phonemes_list[i][0];
                    
                    //USEGLOSSARY
                    //Debug.Log("The phonem BEFORE:" + phonem + ":FIN");

                    if (phonem != "PAUSE")
                    {
                        phonem = ReadGlossary(csvFile.ToString(), phonem);                                                      
                    }
                    //Debug.Log("The phonem AFTER:" + phonem + ":FIN");

                    double time = (double)response.phonemes_list[i][1];
                    double amplitude = Convert.ToDouble(response.phonemes_list[i][2]);
                    //Debug.Log("phonem: " + response.phonemes_list[i][0] + " time: " + response.phonemes_list[i][1] + " amplitude: " + response.phonemes_list[i][2] + " type: " + response.phonemes_list[i][2].GetType());

                    float fTime = (float)time + timeDelay;
                    float fAmplitude = (float)amplitude;

                    //if new -> create
                    if (!keyframesDictionary.ContainsKey(phonem))
                    {
                        keyframesDictionary[phonem] = new List<Keyframe> { new Keyframe(timeDelay, 0) }; ;
                    }


                    //Add if contained
                    if (phonemDictionary.ContainsKey(phonem))
                    {
                        phonemDictionary[phonem].Add((fTime, fAmplitude));
                    }
                    //if new -> create
                    else
                    {
                        phonemDictionary[phonem] = new List<(float, float)> { (fTime, fAmplitude) };
                    }
                }

                //Add to animation
                if(doesAnimationExist)
                {
                    clipTest = anim_obj;
                    tempClip = new AnimationClip();
                    tempClip.name = "tempAnimationHolder";
                    tempClip.legacy = false;

                }
                //Create new animation
                else if (doesAnimationNotExist)
                {
                    newClip = new AnimationClip();
                    clipTest = newClip;
                    clipTest.name = animationName;
                }
                clipTest.legacy = false; //true if we use the old animation system

                // Loop through all lines in dictionnary we just created
                foreach (KeyValuePair<string, List<(float, float)>> toPrint in phonemDictionary)
                {
                    bs_nameTest = null;
                    ksTest = new List<Keyframe> { };      //Activation list of keyframe
                    ksTestDown = new List<Keyframe> { };  //Deactivation list

                    int i = 0;

                    bs_nameTest = "blendShape." + toPrint.Key; //path of the blendshape on the skinnedMeshRenderer

                    // Loop through all value of time for the current Key in loop
                    foreach (var value in toPrint.Value)
                    {
                        //Debug.Log("value.Item1" + value.Item1 + "value.Item2" + value.Item2);

                        ksTest.Add(new Keyframe(value.Item1, value.Item2));
                        ksTestDown.Add(new Keyframe(value.Item1, 0));


                        i++;
                    }


                    curveTest = new AnimationCurve(ksTest.ToArray());
                    curveTestDown = new AnimationCurve(ksTestDown.ToArray());


                    // Loop through all key in phonemdictionnary  ->  then create animation curve at UP for current key and at Down(0) for all others
                    foreach (KeyValuePair<string, List<(float, float)>> onlyKey in phonemDictionary)
                    {

                        // Current Key at Up
                        if (onlyKey.Key == toPrint.Key)
                        {
                            //Case when current Key  => use ksTest
                            if (onlyKey.Key != "PAUSE")
                            {

                                keyframesDictionary[onlyKey.Key].AddRange(ksTest);

                            }
                        }

                        // Other keys at Down(0)
                        else
                        {
                            //Case when not the current Key  =>  use ksTestDown
                            if (onlyKey.Key != "PAUSE")
                            {

                                //bs_nameTestDown = "blendShape." + onlyKey.Key;
                                keyframesDictionary[onlyKey.Key].AddRange(ksTestDown);
                            }
                        }
                    }
                }
                
                Debug.Log($"Framerate: {clipTest.frameRate}");

                int fullCount = 0;
                
                //Take all the Curve created and put it in the clip
                foreach (KeyValuePair<string, List<Keyframe>> allKeyframe in keyframesDictionary)
                {
                    if (allKeyframe.Key != "PAUSE")
                    {
                        bs_nameTest = "blendShape." + allKeyframe.Key;
                        //Debug.Log("BLENDSHAPE NAME: blendShape." + allKeyframe.Key);

                        if (transitionMode != ParametersController.TransitionFunction.None)
                        {
                            List<Keyframe> keysToAdd = new List<Keyframe>();
                            List<Keyframe> sortedKey = allKeyframe.Value;
                            sortedKey.Sort((a, b) => a.time.CompareTo(b.time));
                        
                            for (int i = 0; i < sortedKey.Count - 1; i++)
                            {
                                // Value change
                                if (Math.Abs(sortedKey[i].value - sortedKey[i + 1].value) > 0.01f)
                                {
                                    float duration = sortedKey[i + 1].time - sortedKey[i].time;
                                    float frameTime = duration / (transitionPrecision + 1);
                                    
                                    for (int frame = 1; frame <= transitionPrecision; frame++)
                                    {
                                        float currTime = frame * frameTime;
                                        float currValue = UtilityFunctions.BlendShapeTransitionFunction(
                                            transitionMode,
                                            sortedKey[i].value,
                                            sortedKey[i + 1].value,
                                            currTime/duration,
                                            max_ampl);
                                        if (sortedKey[i].time + currTime > sortedKey[i + 1].time)
                                        {
                                            Debug.LogError($"INVALID KEY!! ({sortedKey[i].time+currTime},{currValue}) > {sortedKey[i+1].time}");
                                        }
                                        else
                                        {
                                            keysToAdd.Add(new Keyframe(sortedKey[i].time + currTime, currValue));
                                        }
                                    }
                                }
                            }
                            Debug.Log($"Adding {keysToAdd.Count} keyframes, total is now {keysToAdd.Count + allKeyframe.Value.Count}");
                            allKeyframe.Value.AddRange(keysToAdd);
                        }
                        
                        for (int i = 0; i < meshList; i++) //TODO UTILITE DE CE TRUC ??
                        {
                            //Debug.Log("blendshapes[i].name" + blendshapes[i].name);
                            var curves = new AnimationCurve(allKeyframe.Value.ToArray());
                            

                            if (tangentMode != AnimationUtility.TangentMode.Free)
                            {
                                Debug.Log($"Setting curves to {tangentMode}");
                                for (int j = 0; j < curves.length; j++)
                                {
                                    AnimationUtility.SetKeyLeftTangentMode(curves, j, tangentMode);
                                    AnimationUtility.SetKeyRightTangentMode(curves, j, tangentMode);
                                }
                            }



                            fullCount += curves.length;
                            Debug.Log($"-- Adding {curves.length} keyframes to animation ({bs_nameTest} values added to {clipTest.name})");

                            if (doesAnimationExist)
                            {
                                tempClip.SetCurve(blendshapes[i].name, typeof(SkinnedMeshRenderer), bs_nameTest, curves);
                            }

                            else
                            {
                                clipTest.SetCurve(blendshapes[i].name, typeof(SkinnedMeshRenderer), bs_nameTest, curves);
                            }

                        }
                        

                    }

                }
                
                
                if (doesAnimationExist)
                {
                    UnityEditor.EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(anim_obj);          
                    UnityEditor.EditorCurveBinding[] bindingsTemp = AnimationUtility.GetCurveBindings(tempClip);            
                    Dictionary<(string, EditorCurveBinding), AnimationCurve > mergeCurves = new Dictionary<(string, EditorCurveBinding), AnimationCurve>();

                    // Loop through the animation curves for the two animation clips and merge them into the corresponding new Dictionary entry.
                    foreach (EditorCurveBinding binding in bindings)
                    {
                        AnimationCurve mergeCurve1 = AnimationUtility.GetEditorCurve(anim_obj, binding);

                        if (!mergeCurves.ContainsKey((binding.propertyName, binding)))
                        {
                            mergeCurves.Add((binding.propertyName, binding), new AnimationCurve());
                        }

                        mergeCurves[(binding.propertyName, binding)] = MergeAnimationCurves(mergeCurves[(binding.propertyName, binding)], mergeCurve1);

                    }

                    foreach (EditorCurveBinding bindingTemp in bindingsTemp)
                    {
                        AnimationCurve mergeCurve2 = AnimationUtility.GetEditorCurve(tempClip, bindingTemp);

                        if (!mergeCurves.ContainsKey((bindingTemp.propertyName, bindingTemp)))
                        {
                            mergeCurves.Add((bindingTemp.propertyName, bindingTemp), new AnimationCurve());
                        }

                        mergeCurves[(bindingTemp.propertyName, bindingTemp)] = MergeAnimationCurves(mergeCurves[(bindingTemp.propertyName, bindingTemp)], mergeCurve2);
                    }

                    foreach (KeyValuePair<(string, EditorCurveBinding), AnimationCurve> mergeCurve in mergeCurves)
                    {
                        AnimationUtility.SetEditorCurve(clipTest, mergeCurve.Key.Item2, mergeCurve.Value);

                    }


                }

                Debug.Log($"------ Added animation with {fullCount} keyframes");

                if (isBlink)
                {
                    //BLINK

                    //float blinkIterator = 0f;
                    blinkKeyframes = new List<Keyframe> { };      //Activation list of keyframe

                    // CHANGER ICI ----------------------------------------------------------------------------------------------------------------------------------------

                    float lastPhonemeTime = Convert.ToSingle(response.phonemes_list[response.phonemes_list.Count - 1][1]);

                    // CHANGER ICI ----------------------------------------------------------------------------------------------------------------------------------------


                    for (float blinkIterator = blinkInterval; blinkIterator < lastPhonemeTime; blinkIterator += blinkInterval )
                    {
                        blinkKeyframes.Add(new Keyframe(blinkIterator - blinkDelta, 0));
                        blinkKeyframes.Add(new Keyframe(blinkIterator, 100));
                        blinkKeyframes.Add(new Keyframe(blinkIterator + blinkDelta, 10));
                    }

                    if (isOneEye)
                    {
                        blinkFullName = "blendShape." + blinkBlendShapeName;
                        clipTest.SetCurve(blinkMesh.name, typeof(SkinnedMeshRenderer), blinkFullName, new AnimationCurve(blinkKeyframes.ToArray()));
                    }
                    else
                    {
                        blinkFullName = "blendShape." + blinkBlendShapeName;
                        blinkFullName2 = "blendShape." + blinkBlendShapeName2;
                        clipTest.SetCurve(blinkMesh.name, typeof(SkinnedMeshRenderer), blinkFullName, new AnimationCurve(blinkKeyframes.ToArray()));
                        clipTest.SetCurve(blinkMesh.name, typeof(SkinnedMeshRenderer), blinkFullName2, new AnimationCurve(blinkKeyframes.ToArray()));
                    }
                    //Debug.Log("blinkFullName: " + blinkFullName);

                }

                //Option : Create new asset selected -> CREATE NEW ASSET
                if (doesAnimationNotExist)
                {
                    Debug.Log("Creating asset");
                    //Debug.Log("Absolute: " + Application.dataPath);
                    //Debug.Log("Relative: " + "Assets" + savePath.Substring(Application.dataPath.Length));
                    
                    AssetDatabase.CreateAsset(clipTest, savePath + "/" + clipTest.name + ".anim");

                }
                else
                {
                    Debug.Log("Adding to Asset");
                }

                
                AssetDatabase.Refresh();
            }
        }
    }
}