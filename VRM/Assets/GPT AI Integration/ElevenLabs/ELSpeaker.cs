using System.Collections;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using OpenAI.Integrations.ElevenLabs.Configuration; // Assuming ELAuthArgsV1 is in this namespace

public class ELSpeaker : MonoBehaviour
{
    public string multilingualV2VoiceID = "YOUR_MULTILINGUAL_V2_VOICE_ID"; // Replace with the actual Voice ID for Eleven Labs
    public bool responsePlaying = false;
    private const string urlTemplate = "https://api.elevenlabs.io/v1/text-to-speech/{{voice_id}}/stream";
    public AudioSource audioSource;  // AudioSource for playing the Eleven Labs response
    public TMP_Text textInput;

    [SerializeField, Tooltip("Check this file and make sure your API key on it is set.")]
    public ELAuthArgsV1 ELAuth;  // Reference to the ELAuthArgsV1 script for API key handling

    // JSON body template for the Eleven Labs API request
    private const string jsonBodyTemplate =
        "{\"text\": \"{{text}}\", \"model_id\": \"eleven_multilingual_v2\", \"voice_settings\": {\"stability\": {{stability}}, \"similarity_boost\": {{similarity_boost}}, \"style\": {{style}}, \"use_speaker_boost\": {{use_speaker_boost}}}}";

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Call this function to trigger speech generation and play it from the input field
    public void SpeakSentenceFromInput()
    {
        responsePlaying = true;
        StartCoroutine(SpeakSentence(textInput.text));
    }

    // Coroutine to send a request to Eleven Labs and play the audio response
    private IEnumerator SpeakSentence(string input)
    {
        if (string.IsNullOrEmpty(multilingualV2VoiceID))
        {
            Debug.LogError("Multilingual V2 Voice ID is not set!");
            yield break;
        }

        string url = urlTemplate.Replace("{{voice_id}}", multilingualV2VoiceID);
        Debug.Log("Using URL: " + url);

        // Get the API key from ELAuthArgsV1
        string apiKey = ELAuth.PrivateApiKey;

        // Set stability, similarity boost, and style settings
        string jsonBody = jsonBodyTemplate
            .Replace("{{text}}", input)
            .Replace("{{stability}}", 0.5f.ToString(CultureInfo.InvariantCulture))  // Stability set to 50%
            .Replace("{{similarity_boost}}", 0.8f.ToString(CultureInfo.InvariantCulture))  // Similarity boost set to 80%
            .Replace("{{style}}", 0.3f.ToString(CultureInfo.InvariantCulture))  // Style set to 30%
            .Replace("{{use_speaker_boost}}", "true");  // Use speaker boost

        Debug.Log("JSON body is: " + jsonBody);

        // Create the web request
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        using (UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("xi-api-key", apiKey);  // Get your Eleven Labs API key from ELAuthArgsV1

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error during Eleven Labs API call: {www.error}");
                responsePlaying = false;
                yield break;
            }

            // Get the audio data from the response
            byte[] audioData = www.downloadHandler.data;
            if (audioData.Length == 0)
            {
                Debug.LogError("No audio data received.");
                responsePlaying = false;
                yield break;
            }

            // Save audio data as a temporary file (if needed)
            string tempFilePath = Path.Combine(Application.persistentDataPath, "ElevenLabsResponse.mp3");
            File.WriteAllBytes(tempFilePath, audioData);
            Debug.Log("Audio file saved at: " + tempFilePath);

            // Load the MP3 file as an AudioClip and play it
            StartCoroutine(LoadAndPlayAudioClip(tempFilePath));
        }
    }

    // Coroutine to load the MP3 file as an AudioClip and play it
    private IEnumerator LoadAndPlayAudioClip(string filePath)
    {
        string url = "file://" + filePath;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error loading AudioClip: {www.error}");
                responsePlaying = false;
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            audioSource.loop = false;  // Ensure the audio does not loop
            audioSource.Play();

            Debug.Log("Audio clip is now playing.");

            // Wait until the audio clip finishes playing
            while (audioSource.isPlaying)
            {
                yield return null;  // Wait for the next frame
            }

            // Audio playback is finished
            responsePlaying = false;
            Debug.Log("Audio clip has finished playing.");
        }
    }
}
