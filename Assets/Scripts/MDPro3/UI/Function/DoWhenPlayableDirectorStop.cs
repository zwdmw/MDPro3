using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace MDPro3.UI
{
    public class DoWhenPlayableDirectorStop : MonoBehaviour
    {
        public Action action;
        PlayableDirector director;
        void Start()
        {
            director = GetComponent<PlayableDirector>();
        }

        void Update()
        {
            if (director == null)
            {
                director = GetComponent<PlayableDirector>();
                if (director == null)
                {
                    CompleteOnce();
                    return;
                }
            }

            if (director.state != PlayState.Playing)
            {
                CompleteOnce();
            }
        }

        void CompleteOnce()
        {
            enabled = false;
            var callback = action;
            action = null;
            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }
    }
}
