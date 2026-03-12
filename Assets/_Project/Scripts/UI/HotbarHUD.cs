using System;
using UnityEngine;
using DeepShift.Core;

namespace DeepShift.UI
{
    /// <summary>
    /// Draws the 2-slot weapon hotbar centred at the bottom of the screen.
    /// Slot 1 = Drill, Slot 2 = Bolt Pistol. Active slot is highlighted in VEKTRA orange.
    /// Also shows current ammo below the ranged weapon slot.
    /// </summary>
    public class HotbarHUD : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Int _onWeaponSlotChanged;
        [SerializeField] private GameEventSO_Int _onWeaponAmmoChanged;

        // ── Private state ──────────────────────────────────────────────────────

        private int _activeSlot  = 0;
        private int _currentAmmo = 0;

        // Inner listener class — avoids single-interface restriction on IGameEventListener<int>
        private class IntListener : IGameEventListener<int>
        {
            private readonly Action<int> _cb;
            public IntListener(Action<int> cb) => _cb = cb;
            public void OnEventRaised(int v) => _cb(v);
        }

        private IntListener _slotListener;
        private IntListener _ammoListener;
        private GUIStyle    _labelStyle;   // built lazily inside OnGUI (GUI.skin only valid there)

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _slotListener = new IntListener(v => _activeSlot  = v);
            _ammoListener = new IntListener(v => _currentAmmo = v);
        }

        private void OnEnable()
        {
            _onWeaponSlotChanged?.RegisterListener(_slotListener);
            _onWeaponAmmoChanged?.RegisterListener(_ammoListener);
        }

        private void OnDisable()
        {
            _onWeaponSlotChanged?.UnregisterListener(_slotListener);
            _onWeaponAmmoChanged?.UnregisterListener(_ammoListener);
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private static readonly Color ActiveColor   = new Color(1f, 0.65f, 0.1f);
        private static readonly Color InactiveColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color TextColor     = Color.white;

        private const float BoxW         = 180f;
        private const float BoxH         = 135f;
        private const float BoxGap       = 22f;
        private const float BottomMargin = 45f;
        private const float Pad          = 10f;

        private void OnGUI()
        {
            // Build style lazily — GUI.skin is only valid inside OnGUI
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 27,
                    fontStyle = FontStyle.Bold,
                };
                _labelStyle.normal.textColor = Color.white;
            }

            float totalWidth = BoxW * 2 + BoxGap;
            float startX     = (Screen.width - totalWidth) * 0.5f;
            float boxY       = Screen.height - BottomMargin - BoxH;

            float s1x = startX;
            float s2x = startX + BoxW + BoxGap;

            // Slot 1 — Drill
            GUI.color = (_activeSlot == 0) ? ActiveColor : InactiveColor;
            GUI.Box(new Rect(s1x, boxY, BoxW, BoxH), "");

            GUI.color = TextColor;
            GUI.Label(new Rect(s1x + Pad, boxY + Pad, BoxW - Pad * 2, BoxH - Pad * 2),
                      "1  DRILL", _labelStyle);

            // Slot 2 — Bolt Pistol
            GUI.color = (_activeSlot == 1) ? ActiveColor : InactiveColor;
            GUI.Box(new Rect(s2x, boxY, BoxW, BoxH), "");

            GUI.color = TextColor;
            GUI.Label(new Rect(s2x + Pad, boxY + Pad,      BoxW - Pad * 2, 45f),
                      "2  BOLT", _labelStyle);
            GUI.Label(new Rect(s2x + Pad, boxY + Pad + 45f, BoxW - Pad * 2, 45f),
                      $"AMMO: {_currentAmmo}", _labelStyle);

            GUI.color = Color.white;
        }
    }
}
