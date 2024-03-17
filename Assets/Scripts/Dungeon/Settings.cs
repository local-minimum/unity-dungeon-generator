using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon
{
    public static class PlayerSettings
    {

        static readonly string SettingsRoot = "ProcDungeon";
        static readonly string MovementsRoot = $"{SettingsRoot}.Movement";
        static readonly string UIRoot = $"{SettingsRoot}.UI";

        public class BoolSetting
        {
            public readonly string Key;
            private bool DefaultValue;

            public BoolSetting(string key, bool defaultValue = false)
            {
                Key = key;
                DefaultValue = defaultValue;
            }

            public bool Value
            {
                get
                {
                    return PlayerPrefs.GetInt(Key, DefaultValue ? 1 : 0) == 1;
                }

                set
                {
                    PlayerPrefs.SetInt(Key, value ? 1 : 0);
                }
            }

            public void RestoreDefault()
            {
                PlayerPrefs.DeleteKey(Key);
            }
        }

        public static readonly BoolSetting InstantMovement = new BoolSetting($"{MovementsRoot}.InstantMovemnt", false);
        public static readonly BoolSetting ShowMinimap = new BoolSetting($"{UIRoot}.MinimapVisible", true);
        public static readonly BoolSetting ShowMovementKeys = new BoolSetting($"{UIRoot}.MovementKeys", true);

        public static void RestoreAllSettings()
        {
            InstantMovement.RestoreDefault();
            ShowMinimap.RestoreDefault();
            ShowMovementKeys.RestoreDefault();
        }
    }
}
