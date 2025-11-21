using UnityEngine;

namespace InsertAffiliate
{
    /// <summary>
    /// Singleton MonoBehaviour to run coroutines for the Insert Affiliate SDK
    /// </summary>
    public class InsertAffiliateCoroutineRunner : MonoBehaviour
    {
        private static InsertAffiliateCoroutineRunner instance;

        public static InsertAffiliateCoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("InsertAffiliateCoroutineRunner");
                    instance = go.AddComponent<InsertAffiliateCoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
