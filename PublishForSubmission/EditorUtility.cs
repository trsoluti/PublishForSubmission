using System;
namespace UnityEditor
{
#if UNITY_EDITOR
    // Unity will supply this class if we are compiling inside the Unity Editor
#else
    public class EditorUtility
    {
        public static void DisplayProgressBar(string title, string info, float progress)
        {
            Console.WriteLine($"{title} / {info} ({progress})");
        }
        public static void ClearProgressBar()
        {
            // no-op outside the editor
        }
    }
#endif
}
