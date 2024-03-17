using TMPro;
using UnityEngine;

namespace ProcDungeon.UI
{
    public class DebugText : Singleton<DebugText>
    {

        TextMeshProUGUI TextUI;
        void Start()
        {
            TextUI = GetComponentInChildren<TextMeshProUGUI>();
        }

        public string Text
        {
            get { return TextUI.text; }
            set { TextUI.text = value; }
        }
    }
}
