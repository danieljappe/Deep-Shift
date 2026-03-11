using UnityEngine;
using DeepShift.Mining;

namespace DeepShift.UI
{
    /// <summary>
    /// Renders a simple on-screen ore count using Unity's immediate-mode GUI.
    /// Attach to any active GameObject in the Mine scene and assign
    /// <see cref="_inventory"/> in the Inspector.
    /// No Canvas setup required — uses <c>OnGUI</c> for prototype display.
    /// </summary>
    public class OreHUD : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _inventory;

        private GUIStyle _style;

        private void Start()
        {
            _style = new GUIStyle
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
            };
            _style.normal.textColor = new Color(1f, 0.65f, 0.1f); // VEKTRA orange
        }

        private void OnGUI()
        {
            if (_inventory == null) return;

            int count = _inventory.Count;
            GUI.Label(new Rect(20f, 20f, 260f, 40f),
                      $"Ore Carried: {count}", _style);
        }
    }
}
