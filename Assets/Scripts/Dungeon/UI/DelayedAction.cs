using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProcDungeon.UI
{
    public class DelayedAction : Singleton<DelayedAction>
    {
        [SerializeField]
        TMPro.TextMeshProUGUI textGUI;

        [SerializeField]
        Image ProgressImage;

        [SerializeField]
        GameObject DisplayRoot;

        System.Action onWaitDone;
        float waitStart;
        float waitDuration;

        public void ShowMessage(string message, System.Action onWaitDone, float waitDuration = 1)
        {
            waitStart = Time.timeSinceLevelLoad;
            this.waitDuration = Mathf.Max(waitDuration, 0.0001f);
            textGUI.text = message;
            this.onWaitDone = onWaitDone;
            ProgressImage.fillAmount = 0;
            DisplayRoot.SetActive(true);
        }

        public void CancelMessage(string message)
        {
            if (message == textGUI.text)
            {
                ClearMessage();
            }
        }

        void ClearMessage()
        {
            onWaitDone = null;
            DisplayRoot.SetActive(false);
        }

        private void Start()
        {
            if (onWaitDone == null) ClearMessage();
        }

        private void Update()
        {
            if (onWaitDone == null) { return; }

            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - waitStart) / waitDuration);

            if (progress == 1)
            {
                onWaitDone();
                ClearMessage();
            }
            else
            {
                ProgressImage.fillAmount = progress;
            }
        }
    }
}