using System.Collections;
using UnityEngine;

namespace KingdomWar.Tools
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner instance;

        private static CoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    var obj = new GameObject("CoroutineRunner");
                    instance = obj.AddComponent<CoroutineRunner>();
                    if (Application.isPlaying)
                        DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        public static void RunCoroutine(IEnumerator coroutine)
        {
            Instance.RunCoroutineInstance(coroutine);
        }

        private void RunCoroutineInstance(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }
}
