using System;
using System.IO;
using UnityEngine;
using DeepShift.Core;

namespace DeepShift.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        // ── Event references ──────────────────────────────────────────────────
        [Header("Events")]
        [SerializeField] private GameEventSO_Int _onOreCreditsChanged;
        [SerializeField] private GameEventSO_Int _onDebtChanged;
        [SerializeField] private GameEventSO_Int _onVektraRepChanged;
        [SerializeField] private GameEventSO_Int _onBlackMarketChipsChanged;

        // ── Backing fields ────────────────────────────────────────────────────
        private int _oreCredits;
        private int _debtTokens;
        private int _vektraRep;
        private int _blackMarketChips;

        // ── Properties ────────────────────────────────────────────────────────
        public int OreCredits
        {
            get => _oreCredits;
            private set { _oreCredits = value; _onOreCreditsChanged?.Raise(value); }
        }

        public int DebtTokens
        {
            get => _debtTokens;
            private set { _debtTokens = value; _onDebtChanged?.Raise(value); }
        }

        public int VektraRep
        {
            get => _vektraRep;
            private set { _vektraRep = value; _onVektraRepChanged?.Raise(value); }
        }

        public int BlackMarketChips
        {
            get => _blackMarketChips;
            private set { _blackMarketChips = value; _onBlackMarketChipsChanged?.Raise(value); }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromJson();
        }

        // ── Public mutation API ───────────────────────────────────────────────
        public void AddOreCredits(int amount)       => OreCredits      += amount;
        public void SpendOreCredits(int amount)     => OreCredits      = Mathf.Max(0, OreCredits - amount);
        public void AddDebt(int amount)             => DebtTokens      += amount;
        public void ReduceDebt(int amount)          => DebtTokens      = Mathf.Max(0, DebtTokens - amount);
        public void ChangeVektraRep(int delta)      => VektraRep       += delta;
        public void AddBlackMarketChips(int amount) => BlackMarketChips += amount;
        public void SpendBlackMarketChips(int amount) =>
            BlackMarketChips = Mathf.Max(0, BlackMarketChips - amount);

        // ── Save / Load (JSON stubs) ──────────────────────────────────────────
        [Serializable]
        private struct SaveData
        {
            public int oreCredits;
            public int debtTokens;
            public int vektraRep;
            public int blackMarketChips;
        }

        private static string SavePath => Path.Combine(Application.persistentDataPath, "economy.json");

        /// <summary>
        /// Serialises all economy values to JSON at
        /// <c>Application.persistentDataPath/economy.json</c>.
        /// Called automatically after successful extraction and can be called explicitly
        /// at any other save point (e.g. scene transition to SurfaceCamp).
        /// TODO: encrypt / obfuscate before shipping.
        /// </summary>
        public void SaveToJson()
        {
            SaveData data = new SaveData
            {
                oreCredits       = _oreCredits,
                debtTokens       = _debtTokens,
                vektraRep        = _vektraRep,
                blackMarketChips = _blackMarketChips,
            };
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        }

        /// <summary>
        /// Loads economy values from JSON, if the save file exists.
        /// Called automatically in <see cref="Awake"/> so values are restored on launch.
        /// Raises all four currency-changed events so any HUD components initialise correctly.
        /// </summary>
        public void LoadFromJson()
        {
            if (!File.Exists(SavePath)) return;

            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

            // Assign backing fields directly then raise events once each
            _oreCredits       = data.oreCredits;
            _debtTokens       = data.debtTokens;
            _vektraRep        = data.vektraRep;
            _blackMarketChips = data.blackMarketChips;

            _onOreCreditsChanged?.Raise(_oreCredits);
            _onDebtChanged?.Raise(_debtTokens);
            _onVektraRepChanged?.Raise(_vektraRep);
            _onBlackMarketChipsChanged?.Raise(_blackMarketChips);
        }
    }
}
