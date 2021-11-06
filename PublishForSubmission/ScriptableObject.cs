using System;
namespace UnityEngine
{
#if UNITY_EDITOR
    // Unity will supply this class if we are compiling inside the Unity Editor
#else
    public class ScriptableObject
    {
        public ScriptableObject()
        {
        }
    }
#endif
}
