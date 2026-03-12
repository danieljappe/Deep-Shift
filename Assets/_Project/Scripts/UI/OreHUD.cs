using System.Collections.Generic;
using UnityEngine;
using DeepShift.Mining;

namespace DeepShift.UI
{
    /// <summary>
    /// Displays the player's carried ore in the top-left corner, grouped by ore type.
    /// Each type is shown on its own line as "Name  ×  count".
    /// </summary>
    public class OreHUD : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;

        private GUIStyle _headerStyle;
        private GUIStyle _itemStyle;

        private const float StartX    = 45f;
        private const float StartY    = 45f;
        private const float LineHeight = 54f;

        private void Start()
        {
            _headerStyle = new GUIStyle
            {
                fontSize  = 36,
                fontStyle = FontStyle.Bold,
            };
            _headerStyle.normal.textColor = new Color(1f, 0.65f, 0.1f); // VEKTRA orange

            _itemStyle = new GUIStyle
            {
                fontSize  = 33,
                fontStyle = FontStyle.Normal,
            };
            _itemStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f); // light grey
        }

        private void OnGUI()
        {
            if (_inventory == null || _headerStyle == null) return;

            // Group ore by display name
            var counts = new Dictionary<string, int>();
            var colors = new Dictionary<string, Color>();
            foreach (var ore in _inventory.CarriedOre)
            {
                if (ore == null) continue;
                if (!counts.ContainsKey(ore.displayName))
                {
                    counts[ore.displayName] = 0;
                    colors[ore.displayName] = ore.tileColor;
                }
                counts[ore.displayName]++;
            }

            float y = StartY;

            GUI.Label(new Rect(StartX, y, 450f, LineHeight), "CARGO", _headerStyle);
            y += LineHeight;

            if (counts.Count == 0)
            {
                _itemStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(StartX, y, 450f, LineHeight), "— empty —", _itemStyle);
                return;
            }

            foreach (var kvp in counts)
            {
                _itemStyle.normal.textColor = colors[kvp.Key];
                GUI.Label(new Rect(StartX, y, 450f, LineHeight),
                          $"{kvp.Key}  ×  {kvp.Value}", _itemStyle);
                y += LineHeight;
            }
        }
    }
}
