using UnityEngine;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace Xandimmersion.Geppetto
{
    public static class UtilityFunctions
    {
        // Check if Blendshape present on body_parts_SK : if need to be more specialized -> changes add SkinnedMeshRenderer on signature function and erase the first loop
        public static bool CheckBlendShapesExistence(List<SkinnedMeshRenderer> body_parts, string blendShape, out string result)
        {
            result = blendShape;

            foreach (SkinnedMeshRenderer part in body_parts)
            {
                for (int i = 0; i < part.sharedMesh.blendShapeCount; i++)
                {
                    if (blendShape == part.sharedMesh.GetBlendShapeName(i))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Function to read the CSV file, extract the data into the blendshape dictionary
        public static Dictionary<string, Dictionary<string, float>> ReadBlendShapeFile(List<SkinnedMeshRenderer> body_parts, TextAsset file, bool scale, float minRangeBlendShapeValue, float maxRangeBlendShapeValue)
        {
            Dictionary<string, Dictionary<string, float>> blendShapesDictionary = new Dictionary<string, Dictionary<string, float>>();

            if (file != null)
            {
                string fileContent = file.text;

                using (StringReader reader = new StringReader(fileContent))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] values = line.Trim().Split(',');

                        if (values.Length > 2 || !string.IsNullOrEmpty(values[0]))
                        {
                            string key = values[0];
                            Dictionary<string, float> blendShapes = new Dictionary<string, float>();

                            for (int i = 1; i < values.Length - 1; i += 2)
                            {
                                if (CheckBlendShapesExistence(body_parts, values[i].Trim(), out string blendShape))
                                {
                                    if (!blendShapes.ContainsKey(blendShape))
                                    {
                                        float value = float.Parse(values[i + 1].Trim(), CultureInfo.InvariantCulture);
                                        if (scale) { value = ScaleValue(value, 0.0f, 100f, minRangeBlendShapeValue, maxRangeBlendShapeValue); }
                                        blendShapes.Add(blendShape, value);
                                    }
                                }
                                else
                                {
                                    Debug.Log("In " + values[0] + " viseme of glossary could not find " + values[i] + " blendshape on body_part_SK");
                                }
                            }

                            blendShapesDictionary.Add(key, blendShapes);
                        }
                    }
                }
            }

            return blendShapesDictionary;
        }

        // Function to reset blendshape putting their values at minRange
        public static void resetBlendShapesValues(List<SkinnedMeshRenderer> body_parts, float minRangeBlendShapeValue)
        {
            foreach (SkinnedMeshRenderer part in body_parts)
            {
                for (int i = 0; i < part.sharedMesh.blendShapeCount; i++)
                {
                    part.SetBlendShapeWeight(i, minRangeBlendShapeValue);
                }
            }
        }

        public static List<string> GetBlendShapesNameDictionaryWithSubstring(Dictionary<string, float> dict, string[] substrings)
        {
            List<string> tmp = new List<string>();

            foreach (KeyValuePair<string, float> pair in dict)
            {
                foreach (string substring in substrings)
                {
                    if (pair.Key.Contains(substring))
                    {
                        tmp.Add(pair.Key);
                    }

                }
            }

            return tmp;
        }

        public static string GetBlendShapeNameWithSubstring(Dictionary<string, float> dictionary, string substring)
        {
            foreach (string key in dictionary.Keys)
            {
                if (key.Contains(substring))
                {
                    return key;
                }
            }

            return substring;
        }

        public static Dictionary<string, float> GetBlendShapeWithSubstring(List<SkinnedMeshRenderer> body_parts, string[] substrings)
        {
            Dictionary<string, float> tmp = new Dictionary<string, float>();

            foreach (SkinnedMeshRenderer part in body_parts)
            {
                for (int i = 0; i < part.sharedMesh.blendShapeCount; i++)
                {
                    foreach (string substring in substrings)
                    {
                        string blendShapeName = part.sharedMesh.GetBlendShapeName(i);
                        if (blendShapeName.Contains(substring) && !tmp.ContainsKey(blendShapeName))
                        {
                            tmp.Add(blendShapeName, part.GetBlendShapeWeight(i));
                        }
                    }
                }
            }

            return tmp;
        }

        public static float ClampValue(float value, float min, float max)
        {
            if (value >= max || value >= 100f)
            {
                return max;
            }

            if (value <= min || value <= 0f)
            {
                return min;
            }

            return value;
        }

        public static string FindOppositeBlendShape(string blendShapeName)
        {
            if (blendShapeName.Contains("Left"))
            {
                return blendShapeName.Replace("Left", "Right");
            }
            else if (blendShapeName.Contains("Right"))
            {
                return blendShapeName.Replace("Right", "Left");
            }
            else
            {
                return blendShapeName;
            }
        }

        // Scale a value from the range [minFrom, maxFrom] to the range [minTo, maxTo]
        public static float ScaleValue(float value, float minFrom, float maxFrom, float minTo, float maxTo)
        {
            return (value - minFrom) * (maxTo - minTo) / (maxFrom - minFrom) + minTo;
        }

        // Method to calculate percentage of a number
        public static float CalculatePercentage(float number, float percentage)
        {
            float percentageDecimal = percentage / 100f;
            return number * percentageDecimal;
        }

        public static float BlendShapeTransitionFunction(Xandimmersion.Geppetto.ParametersController.TransitionFunction transitionFunction, float startValue, float endValue, float time, float maxRangeBlendShapeValue)
        {
            time = Mathf.Clamp01(time);
            switch (transitionFunction)
            {
                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.Lerp:
                    // Linear transition between start and end values depending on the mixing factor, t = time here
                    return Mathf.Lerp(startValue, endValue, time);

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.SmoothStep:
                    // Smooth interpolation that smoothes the transition between start and end values
                    return Mathf.SmoothStep(startValue, endValue, time);

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.SmoothDamp:
                    // Create a smooth transition from one value to another by gradually adjusting the speed of change.
                    float currentVelocity = 0f;
                    return Mathf.SmoothDamp(startValue, endValue, ref currentVelocity, time);

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.MoveTowards:
                    // Create a linear transition between two values with a determined constant speed maxdelta = time here
                    return Mathf.MoveTowards(startValue, endValue, time);

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.BezierCurves:
                    // Create more complex transitions by specifying control points that influence the shape of the curve.
                    float p0 = startValue; // starting point of the curve
                    float p1 = Mathf.Lerp(startValue, endValue, 0.33f); //first control point that affects the shape of the curve (between startValue to EndValue, before p2)
                    float p2 = Mathf.Lerp(startValue, endValue, 0.66f); //second control point that also affects the shape of the curve (Between p1 and endvalue, after p1)
                    float p3 = endValue; //ending point of the curve

                    // Mathf.Lerp is used to calculate explicit control point values at fixed positions along the curve
                    // It is possible to change 0.33f or 0.66f to create different shapes for the Bezier curve

                    float u = 1f - time;
                    float tt = time * time;
                    float uu = u * u;
                    float uuu = uu * u;
                    float ttt = tt * time;
                    return uuu * p0 + 3f * uu * time * p1 + 3f * u * tt * p2 + ttt * p3;

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.EaseInOut:
                    // Create smooth and gradual transitions between values. It provides a way to control the acceleration and deceleration of the interpolation
                    // interpolation applies a cosine curve to create the easing effect. By using the cosine function, the interpolation starts slowly, accelerates in the middle, and then slows down towards the end
                    time = 0.5f * (1f - Mathf.Cos(time * Mathf.PI));
                    // interpolation gradually accelerates during the first half and then gradually decelerates during the second half. This results in a smooth, symmetric easing effect with a consistent rate of change
                    //time = time < 0.5f ? 0.5f * Mathf.Pow(time * 2f, 2f) : 1f - 0.5f * Mathf.Pow(2f - time * 2f, 2f); 
                    return Mathf.Lerp(startValue, endValue, time);

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.ExponentialInterpolation:
                    // Allows you to control the rate of change and the curve shape by adjusting the exponent value used in the Interpolation equation
                    // Higher exponent values will result in faster transitions and steeper curves, while lower exponent values will create slower transitions and more gradual curves.
                    float exponent = Mathf.Lerp(1f, CalculatePercentage(maxRangeBlendShapeValue, 10f), time); // Adjust the second parameter for different levels of exponentiation
                    return Mathf.Lerp(startValue, endValue, Mathf.Pow(time, exponent));

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.StepInterpolation:
                    // Create an instantaneous transition between two values at a specific threshold
                    float threshold = 0.05f;
                    return time < threshold ? startValue : endValue;

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.CatmullRomInterpolation:
                    // Create Interpolation that provides a smooth and continuous curve through a set of control points
                    float s0 = startValue; // The control point preceding the interpolated segment
                    float s1 = startValue + (endValue - startValue) * 0.33f; // First endpoint of the interpolated segment
                    float s2 = startValue + (endValue - startValue) * 0.66f; // Second endpoint of the interpolated segment
                    float s3 = endValue; // The control point following the interpolated segment

                    float t21 = time * time;
                    float t31 = t21 * time;
                    float c0 = -0.5f * s0 + 1.5f * s1 - 1.5f * s2 + 0.5f * s3;
                    float c1 = s0 - 2.5f * s1 + 2f * s2 - 0.5f * s3;
                    float c2 = -0.5f * s0 + 0.5f * s2;
                    float c3 = s1;
                    return c0 * t31 + c1 * t21 + c2 * time + c3;

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.DecelerationInterpolation:
                    //Gradually slows down the transition between start and end values ​​as time elapses. It is often used to create natural deceleration effects.
                    return Mathf.Lerp(startValue, endValue, 1f - Mathf.Pow(1f - time, CalculatePercentage(maxRangeBlendShapeValue, 2f)));

                case Xandimmersion.Geppetto.ParametersController.TransitionFunction.ElasticInterpolation:
                    //Simulates stretching and bouncing behavior when transitioning between start and end values. It can be used to create bouncing or warping effects.
                    float amplitude = CalculatePercentage(maxRangeBlendShapeValue, 1.2f);
                    float frequency = CalculatePercentage(maxRangeBlendShapeValue, 0.4f);
                    return Mathf.Lerp(startValue, endValue, time) + Mathf.Sin(time * Mathf.PI * frequency) * amplitude;

                default:
                    Debug.LogWarning("Incorrect interpolation function. Found " + transitionFunction.ToString());
                    exponent = Mathf.Lerp(1f, CalculatePercentage(maxRangeBlendShapeValue, 10f), time); // Adjust the second parameter for different levels of exponentiation
                    return Mathf.Lerp(startValue, endValue, Mathf.Pow(time, exponent));
            }
        }

        public static void SavePlayerPrefs(string saveKey, string saveValue)
        {
            PlayerPrefs.SetString(saveKey, saveValue);
            PlayerPrefs.Save();
        }
    }
}