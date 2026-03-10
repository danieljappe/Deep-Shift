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

        public void SaveToJson()
        {
            // TODO: encrypt / obfuscate before shipping
            // SaveData data = new SaveData
            // {
            //     oreCredits      = _oreCredits,
            //     debtTokens      = _debtTokens,
            //     vektraRep       = _vektraRep,
            //     blackMarketChips = _blackMarketChips,
            // };
            // File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log("[EconomyManager] SaveToJson — stub");
        }

        public void LoadFromJson()
        {
            // TODO: decrypt / validate before applying
            // if (!File.Exists(SavePath)) return;
            // SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            // _oreCredits       = data.oreCredits;
            // _debtTokens       = data.debtTokens;
            // _vektraRep        = data.vektraRep;
            // _blackMarketChips = data.blackMarketChips;
            Debug.Log("[EconomyManager] LoadFromJson — stub");
        }
    }
}
