using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using Random = System.Random;

namespace Xandimmersion.Geppetto
{
    public class ParametersController : MonoBehaviour
    {
        [Header("Rig Inputs")]
        // For more explanation on the variables go see the ohters files on IdleAnimationController file
        //[SerializeField,Tooltip("Head Transform")] private Transform HeadRig;
        //[SerializeField,Tooltip("Bone on Armature/CC_Base_BoneRoot/CC_Base_Spine02")] private Transform Spine2Rig;
        //[SerializeField,Tooltip("Bone on Armature/CC_Base_BoneRoot/CC_Base_Spine02/CC_Base_NeckTwist02/CC_Base_Head")] private Transform BaseHeadRig; 
        //[SerializeField,Tooltip("Bone on Armature/CC_Base_BoneRoot/CC_Base_Spine02/CC_Base_NeckTwist02/CC_Base_Head/CC_Base_FacialBone/CC_Base_L_Eye")] private Transform LeftEyeRig;
        //[SerializeField,Tooltip("Bone on Armature/CC_Base_BoneRoot/CC_Base_Spine02/CC_Base_NeckTwist02/CC_Base_Head/CC_Base_FacialBone/CC_Base_R_Eye")] private Transform RightEyeRig; 
        
        [Header("BlendShape objects list Inputs")]
        [Space(10),SerializeField,Tooltip("All objects containing blendShape to change")] private List<SkinnedMeshRenderer> body_parts_SK;
        [HideInInspector,Tooltip("Character will Look at this object")] private Transform LookAtTarget;
        [SerializeField,Tooltip("Max amplitude of blendShapes (default 100)")] private int maxRangeBlendShapeValue = 30;
        [SerializeField,Tooltip("Min amplitude of blendShapes (default 0)")] private int minRangeBlendShapeValue = 0;

        // Transition Function
        public enum TransitionFunction
        {
            None,
            Lerp,
            SmoothStep,
            SmoothDamp,
            MoveTowards,
            BezierCurves,
            EaseInOut,
            ExponentialInterpolation,
            StepInterpolation,
            CatmullRomInterpolation,
            DecelerationInterpolation,
            ElasticInterpolation,
            EaseInOutQuad,
            EaseInOutCubic,
            EaseInOutCirc,
            EaseOut,
            EaseOutCirc,
            EaseIn,
            //CubicHermiteInterpolation,
            //PowerInterpolation
        }

        // The emotional states
        public enum EmotionalState
        {
            Neutral,
            Happy,
            Angry,
            Nervous,
            Surprised,
            Disgusted,
            Sad,
            Fear,
            Confusion
        }

        
        [Space(10),Header("LipSync Settings")]
        [SerializeField,Tooltip("Do lipSync animation if true, false otherwise")] private bool doLipSync = false;
        // For more explanation on the variables go see the ohters files on GeppettoControllerARKit file
        [SerializeField,Tooltip("File .txt containing the blendShapes for phoneme reconstruction (.csv possible)")] private TextAsset VisemeFile;
        [SerializeField,Tooltip("Voice name")] private string voice = "Ada_tp";
        [HideInInspector,Tooltip("Max amplitude of blendShapes (default 100)")] private int max_ampl = 30;
        [HideInInspector,Tooltip("Min amplitude of blendShapes (default 0)")] private int min_ampl = 0;
        [SerializeField,Tooltip("Speed speach"),Range(0f,1f)] private float speed = 0.85f;
        [SerializeField,Tooltip("Boost LipSync blendShapes amplitude"), Range(0f,2f)] public float lipSyncAmplitude_boost = 1.0f;
        [SerializeField,TextAreaAttribute,Tooltip("Text spoken by character on play mode")] private string text;
        [SerializeField,Tooltip("Transition/Interpolate function possible")] private TransitionFunction transitionFunction;
     
        [SerializeField] private float audioStartTime;
        [SerializeField] private GameObject audioPlayed;

        [Space(10),Header("Blink Settings")]
        [SerializeField,Tooltip("Authorize character to blink")] private bool blink = false;
        [SerializeField] private List<string> blinkBlendNames;
        [SerializeField,Tooltip("List Mesh linked to blink state")] private SkinnedMeshRenderer blinkMesh;
        [SerializeField] private float blinkInterval = 5f;
        [SerializeField] private float blinkRandomVariation = 2f;
        [SerializeField,Tooltip("Speed blink")] private float blinkSpeed = 0.003f;

        private Animator animator;
        public Animator Animator
        {
            get { return animator; }
            set { animator = value; }
        }

        private bool isTalking;
        public bool IsTalking
        {
            get { return isTalking; }
            set { isTalking = value; }
        }

        [HideInInspector]public GeppettoControllerARKit _geppettoControllerARKitScript;

        void Start()
        {            
            // Get this.Animator
            animator = GetComponent<Animator>();

            
            if (body_parts_SK.Count < 0)
            {
                Debug.LogError("No renderer part found to animate");
            }
            else
            {
                foreach (SkinnedMeshRenderer part in body_parts_SK)
                {
                    if (part.sharedMesh.blendShapeCount < 0)
                    {
                        Debug.LogError("No blendShapes found on renderer "+part);
                    }
                }
            }

            if (doLipSync)
            {
                if (VisemeFile == null)
                {
                    Debug.LogError("No viseme file found");
                }
                else
                {
                    _geppettoControllerARKitScript = GetComponent<GeppettoControllerARKit>();
                    if (_geppettoControllerARKitScript == null)
                    {
                        _geppettoControllerARKitScript = gameObject.AddComponent<GeppettoControllerARKit>();
                    }

                    //_geppettoControllerARKitScript.BaseHeadRig = BaseHeadRig;
                    _geppettoControllerARKitScript.body_parts_SK = body_parts_SK;
                    _geppettoControllerARKitScript.VisemeFile = VisemeFile;
                    _geppettoControllerARKitScript.max_ampl = max_ampl;
                    _geppettoControllerARKitScript.min_ampl = min_ampl;
                    _geppettoControllerARKitScript.speed = speed;
                    _geppettoControllerARKitScript.text = text;
                    _geppettoControllerARKitScript.audioStartTime = audioStartTime;
                    _geppettoControllerARKitScript.audioPlayed = audioPlayed;
                    _geppettoControllerARKitScript.blink = blink;
                    _geppettoControllerARKitScript.blinkBlendNames = blinkBlendNames;
                    _geppettoControllerARKitScript.blinkMesh = blinkMesh;
                    _geppettoControllerARKitScript.blinkInterval = blinkInterval;
                    _geppettoControllerARKitScript.blinkRandomVariation = blinkRandomVariation;
                    _geppettoControllerARKitScript.blinkSpeed = blinkSpeed;
                    _geppettoControllerARKitScript.lipSyncAmplitude_boost = lipSyncAmplitude_boost;
                    _geppettoControllerARKitScript.transitionFunction = transitionFunction;
                    _geppettoControllerARKitScript.voice = voice;
                    _geppettoControllerARKitScript.maxRangeBlendShapeValue = maxRangeBlendShapeValue;
                    _geppettoControllerARKitScript.minRangeBlendShapeValue = minRangeBlendShapeValue;
                }
            }
            
               

            transitionFunction = TransitionFunction.ElasticInterpolation;

            
        }

        void Update()
        {
            
            if (_geppettoControllerARKitScript != null)
            {
                isTalking = _geppettoControllerARKitScript.IsTalking;

                _geppettoControllerARKitScript.blinkInterval = blinkInterval;
                _geppettoControllerARKitScript.blinkRandomVariation = blinkRandomVariation;
                _geppettoControllerARKitScript.blinkSpeed = blinkSpeed;
                _geppettoControllerARKitScript.blink = blink;
                _geppettoControllerARKitScript.speed = speed;
                _geppettoControllerARKitScript.text = text;
                _geppettoControllerARKitScript.speed = speed; 
                _geppettoControllerARKitScript.lipSyncAmplitude_boost = lipSyncAmplitude_boost;
                _geppettoControllerARKitScript.transitionFunction = transitionFunction;
            }

            
        }

        
     
        void OnApplicationQuit()
        {
                        
            // Remove the script components from the GameObject
            if (_geppettoControllerARKitScript != null)
            {
                Destroy(_geppettoControllerARKitScript);
            }

        }   
    }
}