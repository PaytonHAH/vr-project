#if !UNITY_WEBGL
using System;
using System.IO;
using System.Threading.Tasks;
using OpenAi.Api.V1;
using OpenAi.Unity.V1;
using OpenAI.Integrations.VoiceRecorder;
using TMPro;
using UnityEngine;
using OpenAI.DemoScript;

namespace OpenAI.Integrations.ElevenLabs
{
    [RequireComponent(typeof(AudioSource))]
    public class VoiceRecorder : MonoBehaviour
    {
        public TMP_InputField input; // Displays the transcription text response of the audio sent to the Whisper API
        public OpenAiCompleterV1 completer; // Holds the OpenAI API key settings
        private AudioSource audioSource;     // The first AudioSource (on this GameObject for playback)
        public GameObject micMonitorObject;  // Assign the GameObject with the second AudioSource (used for mic monitoring)
        private AudioSource micMonitor;      // The second AudioSource (on the separate GameObject for mic monitoring)

        public int recordingLength = 10; // Length of recording in seconds
        public float recordingThreshold = 0.8f; // Mic volume threshold for triggering recording
        public float recordingTimeThreshold = 2.0f; // Time threshold for stopping recording after mic volume drops below threshold
        public float sensitivity = 100.0f; // Used to adjust the sensitivity of the mic volume
        public float loudness = 0.0f; // Current volume level of the microphone
        private bool isRecording = false; // Used to make sure we don't start recording multiple times
        private bool processingResponse = false; // Used to make sure we don't start processing multiple responses
        private float timeBelowThreshold = 0.0f; // Used to keep track of how long the mic volume has been below the threshold

        void Start()
        {
            MonitorMic();  // Start microphone monitoring
        }

        void Update()
        {
            // Get the current volume level
            ELSpeaker ELSpeaker = GetComponent<ELSpeaker>();
            if (!ELSpeaker.responsePlaying)
            {
                loudness = GetAveragedVolume() * sensitivity;
            }

            // Check if mic volume is above threshold and start recording if it is
            if (!isRecording && !processingResponse && loudness > recordingThreshold)
            {
                isRecording = true;
                StartRecording();
            }
            // Check if mic volume is below threshold and stop recording if it is for a certain amount of time
            else if (isRecording && loudness < recordingThreshold)
            {
                timeBelowThreshold += Time.deltaTime;
                if (timeBelowThreshold >= recordingTimeThreshold)
                {
                    Debug.Log("Mic recording has stopped.");
                    StopRecording();
                    isRecording = false;
                    processingResponse = true;
                    timeBelowThreshold = 0.0f;
                    loudness = 0.0f;
                }
            }
            // Reset time below threshold if mic volume goes back above threshold
            else if (isRecording && loudness > recordingThreshold)
            {
                timeBelowThreshold = 0.0f;
            }
        }

        // Listen to the microphone but do not save the recording
        void MonitorMic()
        {
            audioSource = GetComponent<AudioSource>(); // Get the AudioSource on this GameObject
            audioSource.volume = 0;  // Mute the first AudioSource to prevent mic monitoring through speakers

            // Get the AudioSource from the separate GameObject for mic monitoring
            if (micMonitorObject != null)
            {
                micMonitor = micMonitorObject.GetComponent<AudioSource>();  // Get the second AudioSource from the separate GameObject
                if (micMonitor != null)
                {
                    micMonitor.volume = 1;  // Set the volume of the second AudioSource to monitor the mic
                    micMonitor.clip = Microphone.Start(null, true, recordingLength, 16000);  // Start monitoring the mic
                    micMonitor.loop = true;

                    while (!(Microphone.GetPosition(null) > 0)) { }  // Wait until the mic starts
                    micMonitor.Play();  // Play the mic monitoring
                }
                else
                {
                    Debug.LogError("The assigned GameObject does not have an AudioSource component.");
                }
            }
            else
            {
                Debug.LogError("No GameObject assigned for the mic monitor.");
            }
        }

        // Gets the current volume level of the microphone input (used to determine loudness)
        float GetAveragedVolume()
        {
            float[] data = new float[1024];
            int micPosition = Microphone.GetPosition(null) - (1024 + 1); // Get the position 1024 samples ago
            if (micPosition < 0)
                return 0; // Return 0 if the position is negative
            micMonitor.clip.GetData(data, micPosition); // Use the micMonitor to get the mic data
            float a = 0;
            foreach (float s in data)
            {
                a += Mathf.Abs(s);
            }
            return a / 1024;
        }

        // Start recording, but don't save the recording to disk
        public void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone found to record audio clip sample with.");
                return;
            }
            string mic = Microphone.devices[0];
            Debug.Log("Mic audio crossed threshold, starting recording...");
            audioSource.clip = Microphone.Start(mic, false, recordingLength, 16000);
            while (!(Microphone.GetPosition(null) > 0)) { }
            audioSource.Play();  // Play the microphone recording in real-time, but do not save it
        }

        // Stop recording, process the audio in memory, but don't save it to disk
        public async void StopRecording()
        {
            Microphone.End(null);
            audioSource.Stop();
            Debug.Log("Processing the audio recording...");
            await TranscriptRecording();  // Send the audio data to Whisper without saving it
            processingResponse = false;
            Debug.Log("Response processing complete, playing AI response...");
        }

        public async Task TranscriptRecording()
        {
            var transcript = await SendTranscriptRequest(
                audioSource.clip,  // Send the audio clip (in memory) to Whisper for transcription
                "This is the prompt for the Whisper API"  // Example prompt for Whisper
            );
            if (transcript.IsSuccess)
            {
                input.text = transcript.Result.text;  // Display the transcribed text
                OpenAIDemo openAIDemo = GetComponent<OpenAIDemo>();
                await openAIDemo.SendOpenAIRequest();  // Send the transcribed text to OpenAI
                ELSpeaker ELSpeaker = GetComponent<ELSpeaker>();
                ELSpeaker.SpeakSentenceFromInput();  // Use Eleven Labs to generate the voice from the OpenAI response
                Debug.Log("Playing response audio...");
                audioSource.volume = 1;  // Play the generated voice

                // Wait until Eleven Labs response finishes playing
                while (ELSpeaker.responsePlaying)
                {
                    await Task.Yield();
                }

                // After Eleven Labs audio finishes, start listening to the microphone again
                Debug.Log("AI response finished, resuming microphone monitoring...");
                MonitorMic();  // Restart microphone monitoring
            }
            else
            {
                input.text = $"ERROR: StatusCode: {transcript.HttpResponse.responseCode} - {transcript.HttpResponse.error}";
            }
        }

        // Send the audio clip directly to Whisper API without saving it
        public async Task<ApiResult<TranscriptionV1>> SendTranscriptRequest(AudioClip clip, string prompt)
        {
            SOAuthArgsV1 auth = completer.Auth;
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());
            Debug.Log("Sending audio recording to the Whisper API...");

            // Convert the audio clip to a byte array (in memory, not saved on disk)
            string filepath;
            byte[] audioFile = WavUtility.FromAudioClip(audioSource.clip, out filepath, false);  // Do not save to file

            ApiResult<TranscriptionV1> comp = await api.Audio.Transcriptions.CreateTranscriptionAsync(
                new TranscriptionRequestV1()
                {
                    model = "whisper-1",
                    prompt = prompt,
                    response_format = "json",
                    audioFile = audioFile
                }
            );

            return comp;
        }
    }
}
#endif
