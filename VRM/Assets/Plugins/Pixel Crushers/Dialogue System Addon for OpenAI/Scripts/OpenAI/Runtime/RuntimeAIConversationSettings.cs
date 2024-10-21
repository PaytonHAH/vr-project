// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    public enum ImageSizes { Size256x256, Size512x512, Size1024x1024 }

    // Add to dialogue UI.
    public class RuntimeAIConversationSettings : MonoBehaviour
    {

        public static RuntimeAIConversationSettings Instance { get; private set; }

        [Header("OpenAI Settings")]
        [SerializeField] private string apiKey;
        [SerializeField] private TextModelName textModelName = TextModelName.GPT3_5_Turbo;
        [Tooltip("Fine-Tuned Model is only applicable if Text Model Name is set to Fine-Tuned.")]
        [SerializeField] private string fineTunedModelName;
        [Tooltip("Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. We generally recommend altering this or top_p but not both.")]
        [SerializeField] private float temperature = 0.4f;
        [Tooltip("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend altering this or temperature but not both.")]
        [SerializeField][Range(0, 1)] private float top_p = 1f;
        [Tooltip("The total length of input tokens and generated tokens is limited by the model's context length.")]
        [SerializeField] private int maxTokens = 4097;
        [Tooltip("Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")]
        [SerializeField][Range(-2, 2)] private float frequencyPenalty = 0;
        [Tooltip("Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")]
        [SerializeField][Range(-2, 2)] private float presencePenalty = 0;
        [Tooltip("Use OpenAI for text to speech voice generation.")]
        [SerializeField] private bool useOpenAIVoiceGeneration = true;

        [Header("ElevenLabs Settings")]
        [Tooltip("If you want to generate text to speech, set your ElevenLabs API key here.")]
        [SerializeField] private string elevenLabsApiKey;
        [SerializeField] private ElevenLabs.ElevenLabsAPI.Models elevenLabsModel;

        [Header("UI Elements")]
        [Tooltip("Runtime conversations show this icon while waiting for OpenAI responses.")]
        [SerializeField] private GameObject waitingIcon;
        [Tooltip("Input field used for freeform text input conversations.")]
        [SerializeField] private StandardUIInputField chatInputField;
        [Tooltip("This button ends freeform text input conversations.")]
        [SerializeField] private Button goodbyeButton;
        [Tooltip("Shows generated images when playing CYOA (Choose Your Own Adventure) conversations.")]
        [SerializeField] private Image image;
        [SerializeField] private ImageSizes imageSize = ImageSizes.Size256x256;
        [Tooltip("If you want to allow speech input, this button starts recording user's speech.")]
        [SerializeField] private Button recordButton;
        [Tooltip("This button stops recording and submits it to OpenAI for text transcription.")]
        [SerializeField] private Button submitRecordingButton;
        [Tooltip("Optional dropdown for microphone input selection.")]
        [SerializeField] private UIDropdownField microphoneDevicesDropdown;
        [Tooltip("If recording audio, record up to this many seconds.")]
        [SerializeField] private int maxRecordingLength = 10;
        [Tooltip("If recording audio, record at this frequency.")]
        [SerializeField] private int recordingFrequency = 44100;

        public GameObject WaitingIcon => waitingIcon;
        public StandardUIInputField ChatInputField => chatInputField;
        public Button GoodbyeButton => goodbyeButton;
        public Image Image => image;
        public Button RecordButton => recordButton;
        public Button SubmitRecordingButton => submitRecordingButton;
        public UIDropdownField MicrophoneDevicesDropdown => microphoneDevicesDropdown;
        public int MaxRecordingLength => maxRecordingLength;
        public int RecordingFrequency => recordingFrequency;

        public string APIKey { get => apiKey; set => apiKey = value; }
        public Model Model => GetModel();
        public bool IsChatModel => Model.ModelType == ModelType.Chat;
        public float Temperature { get => temperature; set => temperature = value; }
        public float TopP { get => top_p; set => top_p = value; }
        public int MaxTokens { get => maxTokens; set => maxTokens = value; }
        public float FrequencyPenalty { get => frequencyPenalty; set => frequencyPenalty = value; }
        public float PresencePenalty { get => presencePenalty; set => presencePenalty = value; }
        public bool UseOpenAIVoiceGeneration { get => useOpenAIVoiceGeneration; set => useOpenAIVoiceGeneration = value; }
        public string ElevenLabsApiKey { get => elevenLabsApiKey; set => elevenLabsApiKey = value; }
        public ElevenLabs.ElevenLabsAPI.Models ElevenLabsModel { get => elevenLabsModel; set => elevenLabsModel = value; } 
        public string ElevenLabsModelId => ElevenLabs.ElevenLabsAPI.GetModelId(elevenLabsModel);

        public IVoiceService VoiceService { get; set; } = null;

        public string ImageSizeString
        {
            get
            {
                switch (imageSize)
                {
                    default:
                    case ImageSizes.Size256x256: return "256x256";
                    case ImageSizes.Size512x512: return "512x512";
                    case ImageSizes.Size1024x1024: return "1024x1024";
                }
            }
        }

        public int ImageSizeValue
        {
            get
            {
                switch (imageSize)
                {
                    default:
                    case ImageSizes.Size256x256: return 256;
                    case ImageSizes.Size512x512: return 512;
                    case ImageSizes.Size1024x1024: return 1024;
                }
            }
        }

        private Model fineTunedModel = null;

        protected virtual Model GetModel()
        {
            if (textModelName == TextModelName.FineTune)
            { 
                if (fineTunedModel == null)
                {
                    fineTunedModel = new Model(fineTunedModelName, ModelType.Chat, MaxTokens);
                }
                return fineTunedModel;
            }
            else
            {
                return OpenAI.NameToModel(textModelName);
            }
        }

        protected virtual void Awake()
        {
            Instance = this;
            HideExtraUIElements();
        }

        protected virtual void Start()
        {
            var dialogueUI = GetComponent<StandardDialogueUI>();
            if (dialogueUI == null) return;
            if (dialogueUI.conversationUIElements.mainPanel != null)
            {
                dialogueUI.conversationUIElements.mainPanel.onClose.AddListener(HideExtraUIElements);
            }
        }

        protected virtual void HideExtraUIElements()
        {
            if (waitingIcon != null) waitingIcon.SetActive(false);
            if (goodbyeButton != null) goodbyeButton.gameObject.SetActive(false);
        }

    }
}

#endif

