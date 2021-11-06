using System;
namespace UnityEditor
{
#if UNITY_EDITOR
    // Unity will supply this class if we are compiling inside the Unity Editor
#else
    public class Debug
    {
        public static void Log(object obj)
        {
            Console.WriteLine(obj.ToString());
        }
    }
#endif
}
