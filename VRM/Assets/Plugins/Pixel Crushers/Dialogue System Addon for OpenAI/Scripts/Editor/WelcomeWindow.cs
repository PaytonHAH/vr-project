// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Dialogue System Addon for OpenAI welcome window.
    /// </summary>
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {

        private const string ShowOnStartEditorPrefsKey = "PixelCrushers.DialogueSystemOpenAIAddon.ShowWelcomeOnStart";
        private const string SettingsPrefsKey = "PixelCrushers.DialogueSystemOpenAIAddon.Settings";
        private const string USE_OPENAI = "USE_OPENAI";
        private const string USE_OVERTONE = "USE_OVERTONE";
        private const string USE_DEEPVOICE = "USE_DEEPVOICE";

        private static WelcomeWindow instance;

        private static GUIContent MainWindowButtonLabel = new GUIContent("OpenAI Addon Window", "Open main OpenAI Addon window.");
        private static GUIContent AssistantWindowButtonLabel = new GUIContent("Assistant Window", "Open AI Assistant window.");
        private static GUIContent OvertoneLabel = new GUIContent("Overtone", "Enable support for Least Squares' Overtone asset. You must also import the 'Overtone Support' unitypackage from the Third Party Support folder.");
        private static GUIContent DeepVoiceLabel = new GUIContent("DeepVoice", "Enable support for AiKodex's DeepVoice asset. You must also import the 'DeepVoice Support' unitypackage from the Third Party Support folder.");
        private static GUIContent OverrideBaseUrlLabel = new GUIContent("Override Base URL", "Use a different URL than api.openai.com.");
        private static GUIContent BaseUrlLabel = new GUIContent("Base URL", "Use this URL instead of api.openai.com.");

        private static bool showOnStartPrefs
        {
            get { return EditorPrefs.GetBool(ShowOnStartEditorPrefsKey, true); }
            set { EditorPrefs.SetBool(ShowOnStartEditorPrefsKey, value); }
        }

        [MenuItem("Tools/Pixel Crushers/Dialogue System/Addon for OpenAI/Welcome Window", false, -2)]
        public static void Open()
        {
            instance = GetWindow<WelcomeWindow>(false, "Welcome");
            instance.minSize = new Vector2(350, 200);
            instance.showOnStart = true; // Can't check EditorPrefs when constructing window: showOnStartPrefs;
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            RegisterWindowCheck();
        }

        private static void RegisterWindowCheck()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update -= CheckShowWelcomeWindow;
                EditorApplication.update += CheckShowWelcomeWindow;
            }
        }

        private static void CheckShowWelcomeWindow()
        {
            EditorApplication.update -= CheckShowWelcomeWindow;
            if (showOnStartPrefs)
            {
                Open();
            }
        }

        private bool showOnStart = true;
        private string openAIKey;
        private string elevenLabsKey;
#if USE_OPENAI
        private ElevenLabs.ElevenLabsAPI.Models elevenLabsModel;
#endif
        private AddonSettings settings;
        private GUIStyle heading;
        private GUIStyle labelWordWrapped;
        private GUIStyle labelHyperlink;
        private GUIStyle labelSuccess;
        private Vector2 scrollPosition = Vector2.zero;

        private void OnEnable()
        {
#if USE_OPENAI
            openAIKey = EditorPrefs.GetString(DialogueSystemOpenAIWindow.OpenAIKey);
            elevenLabsKey = EditorPrefs.GetString(DialogueSystemOpenAIWindow.ElevenLabsKey);
            elevenLabsModel = (ElevenLabs.ElevenLabsAPI.Models)EditorPrefs.GetInt(DialogueSystemOpenAIWindow.ElevenLabsModel, 0);
#endif
            LoadSettings();
        }

        private void OnDisable()
        {
            instance = null;
            SaveSettings();
        }

        private void LoadSettings()
        {
            if (EditorPrefs.HasKey(SettingsPrefsKey))
            {
                settings = JsonUtility.FromJson<AddonSettings>(EditorPrefs.GetString(SettingsPrefsKey));
            }
            if (settings == null) settings = new AddonSettings();
#if USE_OPENAI
            if (!string.IsNullOrEmpty(settings.baseURL)) OpenAI.BaseURL = settings.baseURL;
#endif
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(SettingsPrefsKey, JsonUtility.ToJson(settings));
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            CheckGUIStyles();
            DrawBanner();
            DrawInfoText();
            DrawDefineSection();
            DrawKeySection();
            DrawOpenButton();
            DrawElevenLabsSection();
            DrawIntegrationsSection();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField(string.Empty, GUILayout.Height(EditorGUIUtility.singleLineHeight + 8f));
            DrawFooter();
        }

        private void CheckGUIStyles()
        {
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label);
                heading.fontStyle = FontStyle.Bold;
                heading.fontSize = 16;
                heading.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
            if (labelWordWrapped == null)
            {
                labelWordWrapped = new GUIStyle(GUI.skin.label);
                labelWordWrapped.wordWrap = true;
            }
            if (labelHyperlink == null)
            {
                labelHyperlink = new GUIStyle(GUI.skin.label);
                labelHyperlink.normal.textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue;
            }
            if (labelSuccess == null)
            {
                labelSuccess = new GUIStyle(GUI.skin.label);
                labelSuccess.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 1, 0) : new Color(0, 0.7f, 0);
            }
        }

        private void DrawBanner()
        {
            EditorGUILayout.LabelField("Dialogue System Addon for OpenAI", heading);
            EditorGUILayout.Space();
        }

        private void DrawInfoText()
        {
            EditorGUILayout.LabelField("Welcome to the Dialogue System Addon for OpenAI!", labelWordWrapped);
            EditorGUILayout.LabelField("This addon is a third-party OpenAI API client. It is not affiliated with OpenAI Inc. " +
                "An OpenAI account is required. The addon will use your OpenAI account's API key. " +
                "You are responsible for any usage charges that OpenAI applies to your API key.", labelWordWrapped);
            if (GUILayout.Button("OpenAI Pricing", labelHyperlink))
            {
                Application.OpenURL("https://openai.com/api/pricing/");
            }
        }

        private void DrawDefineSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enable Addon for OpenAI", EditorStyles.boldLabel);
            if (MoreEditorUtility.DoesScriptingDefineSymbolExist(USE_OPENAI))
            {
                EditorGUILayout.LabelField("✓ Enabled. (USE_OPENAI Scripting Define Symbol)", labelSuccess);
            }
            else
            {
                EditorGUILayout.LabelField("Click the button below to add the Scripting Define Symbol USE_OPENAI, " +
                    "which will enable the addon:", labelWordWrapped);
                if (GUILayout.Button("Enable Addon"))
                {
                    MoreEditorUtility.TryAddScriptingDefineSymbols(USE_OPENAI);
                    EditorUtility.DisplayDialog("Enable Addon", "Setting Scripting Define Symbol USE_OPENAI to " +
                        "enable the Dialogue System Addon for OpenAI.\n\n" +
                        "If you've imported the Dialogue System's assembly definition files, " +
                        "you'll need to import the addon's AddonAssemblyDefinitions unitypackage " +
                        "and possibly update them to account for any other asmdef references you've added.", "OK");
                    EditorTools.ReimportScripts();
                    Repaint();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawKeySection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configure OpenAI Access", EditorStyles.boldLabel);
#if !USE_OPENAI
            EditorGUILayout.LabelField("Enable Addon first.", labelWordWrapped);
#else
            if (!OpenAI.IsApiKeyValid(openAIKey))
            {
                EditorGUILayout.LabelField("If you don't have an OpenAI API key, click here to create one:", labelWordWrapped);
            }
            else
            {
                EditorGUILayout.LabelField("✓ OpenAI Key Accepted.", labelSuccess);
            }
            if (GUILayout.Button("Create New OpenAI API Key"))
            {
                Application.OpenURL("https://platform.openai.com/account/api-keys");
            }

            EditorGUILayout.BeginHorizontal();
            openAIKey = EditorGUILayout.TextField("Open API Key", openAIKey);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(openAIKey) || !openAIKey.StartsWith("sk-") || settings.overrideBaseURL);
            var connectButtonWidth = GUI.skin.button.CalcSize(new GUIContent("Connect")).x;
            if (GUILayout.Button("Connect", GUILayout.Width(connectButtonWidth)))
            {
                EditorPrefs.SetString(DialogueSystemOpenAIWindow.OpenAIKey, openAIKey);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(openAIKey));
            EditorGUILayout.EndHorizontal();

            settings.overrideBaseURL = EditorGUILayout.Toggle(OverrideBaseUrlLabel, settings.overrideBaseURL);
            if (settings.overrideBaseURL)
            {
                EditorGUI.BeginChangeCheck();
                settings.baseURL = EditorGUILayout.TextField(BaseUrlLabel, settings.baseURL);
                if (EditorGUI.EndChangeCheck())
                {
                    OpenAI.BaseURL = settings.baseURL;
                }
            }
#endif
        }

        private void DrawOpenButton()
        {
            EditorGUILayout.Space();
#if USE_OPENAI
            EditorGUI.BeginDisabledGroup(!(MoreEditorUtility.DoesScriptingDefineSymbolExist(USE_OPENAI)));
#else
            EditorGUI.BeginDisabledGroup(true);
#endif
            if (GUILayout.Button(MainWindowButtonLabel))
            {
#if USE_OPENAI
                DialogueSystemOpenAIWindow.OpenMain();
#endif
            }
            if (GUILayout.Button(AssistantWindowButtonLabel))
            {
#if USE_OPENAI
                AssistantWindow.Open();
#endif
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawElevenLabsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configure ElevenLabs Access (Optional)", EditorStyles.boldLabel);
#if !USE_OPENAI
            EditorGUILayout.LabelField("Enable Addon first.", labelWordWrapped);
#else
            if (!ElevenLabs.ElevenLabsAPI.IsApiKeyValid(elevenLabsKey))
            {
                EditorGUILayout.LabelField("If you want to use ElevenLabs text to speech and don't have an ElevenLabs API key, click here to create one:", labelWordWrapped);
            }
            else
            {
                EditorGUILayout.LabelField("✓ ElevenLabs Key Accepted.", labelSuccess);
            }
            if (GUILayout.Button("Create New ElevenLabs API Key"))
            {
                Application.OpenURL("https://docs.elevenlabs.io/authentication/01-xi-api-key");
            }

            EditorGUI.BeginChangeCheck();
            elevenLabsKey = EditorGUILayout.TextField("ElevenLabs API Key", elevenLabsKey);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(DialogueSystemOpenAIWindow.ElevenLabsKey, elevenLabsKey);
            }
            if (ElevenLabs.ElevenLabsAPI.IsApiKeyValid(elevenLabsKey))
            {
                EditorGUI.BeginChangeCheck();
                elevenLabsModel = (ElevenLabs.ElevenLabsAPI.Models)EditorGUILayout.EnumPopup("Model", elevenLabsModel);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(DialogueSystemOpenAIWindow.ElevenLabsModel, (int)elevenLabsModel);
                }
            }
#endif
        }

        private void DrawIntegrationsSection()
        {
#if USE_OPENAI
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Other Integrations", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
#if USE_OVERTONE
            EditorGUI.BeginChangeCheck();
            var toggle = EditorGUILayout.Toggle(OvertoneLabel, true);
            if (EditorGUI.EndChangeCheck() && !toggle)
            {
                MoreEditorUtility.TryRemoveScriptingDefineSymbols(USE_OVERTONE);
                EditorUtility.DisplayDialog("Disabling Overtone Integration", "Removing Scripting Define Symbol USE_OVERTONE.", "OK");
                EditorTools.ReimportScripts();
                Repaint();
                GUIUtility.ExitGUI();
            }
#else
            EditorGUI.BeginChangeCheck();
            var toggle = EditorGUILayout.Toggle(OvertoneLabel, false);
            if (EditorGUI.EndChangeCheck() && toggle)
            {
                MoreEditorUtility.TryAddScriptingDefineSymbols(USE_OVERTONE);
                EditorUtility.DisplayDialog("Enable Overtone Integration", "Setting Scripting Define Symbol USE_OVERTONE to " +
                    "enable the Overtone integration.", "OK");
                EditorTools.ReimportScripts();
                Repaint();
                GUIUtility.ExitGUI();
            }
#endif
#if USE_DEEPVOICE
            EditorGUI.BeginChangeCheck();
            toggle = EditorGUILayout.Toggle(DeepVoiceLabel, true);
            if (EditorGUI.EndChangeCheck() && !toggle)
            {
                MoreEditorUtility.TryRemoveScriptingDefineSymbols(USE_DEEPVOICE);
                EditorUtility.DisplayDialog("Disabling DeepVoice Integration", "Removing Scripting Define Symbol USE_DEEPVOICE.", "OK");
                EditorTools.ReimportScripts();
                Repaint();
                GUIUtility.ExitGUI();
            }
#else
            EditorGUI.BeginChangeCheck();
            toggle = EditorGUILayout.Toggle(DeepVoiceLabel, false);
            if (EditorGUI.EndChangeCheck() && toggle)
            {
                MoreEditorUtility.TryAddScriptingDefineSymbols(USE_DEEPVOICE);
                EditorUtility.DisplayDialog("Enable DeepVoice Integration", "Setting Scripting Define Symbol USE_DEEPVOICE to " +
                    "enable the DeepVoice integration.", "OK");
                EditorTools.ReimportScripts();
                Repaint();
                GUIUtility.ExitGUI();
            }
#endif
#endif
        }

        private void DrawFooter()
        {
            if (string.IsNullOrEmpty(openAIKey) || !openAIKey.StartsWith("sk-"))
            {
                EditorGUI.EndDisabledGroup();
            }
            var newShowOnStart = EditorGUI.ToggleLeft(new Rect(5, position.height - 5 - EditorGUIUtility.singleLineHeight, position.width - (70 + 150), EditorGUIUtility.singleLineHeight), "Show at start", showOnStart);
            if (newShowOnStart != showOnStart)
            {
                showOnStart = newShowOnStart;
                showOnStartPrefs = newShowOnStart;
            }
            if (GUI.Button(new Rect(position.width - 80, position.height - 5 - EditorGUIUtility.singleLineHeight, 70, EditorGUIUtility.singleLineHeight), new GUIContent("Support", "Contact the developer for support")))
            {
                Application.OpenURL("http://www.pixelcrushers.com/support-form/");
            }
        }

    }

}
