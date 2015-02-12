using UnityEngine;
using System.Collections;
using NodeCanvas.Variables;
using System;

namespace scenes.collection
{
    public class Collectible : MonoBehaviour
    {

        private static int INSTANCE_COUNT = 0;

        public string Name;

        private int _instanceId;
        private bool activeInView;

        void Awake()
        {
            _instanceId = INSTANCE_COUNT++;
        }

        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            bool inViewFrustrum = IsVisible();

            if (inViewFrustrum != activeInView)
            {
                activeInView = inViewFrustrum;
            }
        }

        public bool IsVisible()
        {

            return false;
        }

        public string getCollectibleScreenName
        {
            get { return Name; }
        }

        public int instanceId
        {
            get { return _instanceId; }
        }
    }

    [Serializable]
    public class BBCollectible : BBVariable<Collectible> { }
}
