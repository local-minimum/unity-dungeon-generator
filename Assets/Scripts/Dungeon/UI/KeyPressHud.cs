using ProcDungeon.World;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace ProcDungeon.UI
{
    public class KeyPressHud : Singleton<KeyPressHud>
    {
        [SerializeField]
        GameObject RotateCCW;

        [SerializeField]
        GameObject MoveForward;

        [SerializeField]
        GameObject RotateCW;

        [SerializeField]
        GameObject StrafeLeft;

        [SerializeField]
        GameObject MoveBackward;

        [SerializeField]
        GameObject StrafeRight;

        [SerializeField]
        Color DefaultColor;
        [SerializeField]
        Color ActiveColor;
        [SerializeField]
        Color PressedColor;

        [SerializeField, Range(0, 1)]
        float easeToDefaultTime = 0.1f;

        private struct Ease
        {
            public readonly GameObject Target;
            public readonly Color StartColor;
            float StartTime;
            float Duration;

            public Ease(GameObject target, Color startColor, float duration)
            {
                Target = target;
                StartColor = startColor;
                StartTime = Time.timeSinceLevelLoad;
                Duration = duration;
            }

            public float CalculateProgress() => Mathf.Clamp01((Time.timeSinceLevelLoad - StartTime) / Duration);
            
        }

        private GameObject GetByMovement(PlayerController.Movement movement)
        {
            switch (movement)
            {
                case PlayerController.Movement.Forward:
                    return MoveForward;
                case PlayerController.Movement.Backward:
                    return MoveBackward;
                case PlayerController.Movement.StrafeLeft:
                    return StrafeLeft;
                case PlayerController.Movement.StrafeRight:
                    return StrafeRight;
                case PlayerController.Movement.TurnCCW:
                    return RotateCCW;
                case PlayerController.Movement.TurnCW:
                    return RotateCW;
                   
                default:
                    return null; 
            }
        }

        List<PlayerController.Movement> pressed = new List<PlayerController.Movement>();
        List<Ease> eases = new List<Ease>();

        void ApplyEffect(GameObject go, Color color)
        {
            foreach (Image image in go.GetComponentsInChildren<Image>())
            {
                image.color = color;
            }

            foreach (TextMeshProUGUI text in go.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.color = color;
            }
        }

        private void SyncPressed()
        {
            for (int i = 0, n = pressed.Count; i < n; i++)
            {
                var movement = pressed[i];
                var go = GetByMovement(movement);
                if (go == null) continue;

                ApplyEffect(go, i == n - 1 ? ActiveColor : PressedColor);
            }
        }

        public void Press(PlayerController.Movement movement)
        {
            var go = GetByMovement(movement);
            if (go == null) return;

            pressed.Add(movement);

            eases = eases.Where(e => e.Target != go).ToList();

            SyncPressed();
            
        }

        public void Release(PlayerController.Movement movement)
        {
            var go = GetByMovement(movement);
            if (go == null) return;

            eases.Add(new Ease(go, pressed.Last() == movement ? ActiveColor : PressedColor, easeToDefaultTime));

            pressed.Remove(movement);

            SyncPressed();
        }

        private void Start()
        {
            ApplyEffect(RotateCCW, DefaultColor);
            ApplyEffect(MoveForward, DefaultColor);
            ApplyEffect(RotateCW, DefaultColor);
            ApplyEffect(StrafeLeft, DefaultColor);
            ApplyEffect(MoveBackward, DefaultColor);
            ApplyEffect(StrafeRight, DefaultColor);

            Visible = PlayerSettings.ShowMovementKeys.Value;
        }

        private void Update()
        {
            if (eases.Count == 0) return;

            var nextEases = new List<Ease>();

            foreach (var ease in eases)
            {
                var progress = ease.CalculateProgress();

                ApplyEffect(ease.Target, Color.Lerp(ease.StartColor, DefaultColor, progress));

                if (progress < 1)
                {
                    nextEases.Add(ease);
                }
            }

            eases = nextEases;
        }

        public bool Visible
        {
            set
            {
                for (int i = 0, n = transform.childCount; i<n; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(value);
                }
            }
        }

        
    }
}
