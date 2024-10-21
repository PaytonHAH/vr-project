using UnityEngine;

namespace OpenAI.Integrations.ElevenLabs.Configuration
{
    [CreateAssetMenu(fileName = "ELAuthArgs", menuName = "ElevenLabs/Configuration/ElevenLabs Auth configuration")]
    public class ELAuthArgsV1 : ScriptableObject
    {
        [SerializeField] // Ensure this field is serialized and shows in the Inspector
        public string PrivateApiKey;
    }
}
