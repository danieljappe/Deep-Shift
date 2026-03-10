using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepShift.ORION
{
    /// <summary>
    /// Manages ORION's voice dialogue queue.
    /// Priority 0 = tips, 1 = advisories, 2 = critical (always interrupts).
    /// </summary>
    public class ORIONDialogueManager : MonoBehaviour
    {
        public static ORIONDialogueManager Instance { get; private set; }

        [SerializeField] private float _cooldownSeconds = 3f;

        // Three queues indexed by priority (0=tips, 1=advisories, 2=critical)
        private readonly Queue<string>[] _queues = new Queue<string>[3]
        {
            new Queue<string>(),
            new Queue<string>(),
            new Queue<string>(),
        };

        private Coroutine _playbackRoutine;
        private bool _isBusy; // true while playing or cooling down

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Enqueue(string line, int priority)
        {
            priority = Mathf.Clamp(priority, 0, 2);
            _queues[priority].Enqueue(line);

            if (priority == 2 && _isBusy)
            {
                Interrupt();
                return;
            }

            if (!_isBusy)
                _playbackRoutine = StartCoroutine(ProcessQueue());
        }

        // Convenience wrappers
        public void EnqueueTip(string line)      => Enqueue(line, 0);
        public void EnqueueAdvisory(string line) => Enqueue(line, 1);
        public void EnqueueCritical(string line) => Enqueue(line, 2);

        // ── Internal ──────────────────────────────────────────────────────────

        private void Interrupt()
        {
            if (_playbackRoutine != null) StopCoroutine(_playbackRoutine);
            _playbackRoutine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            _isBusy = true;

            while (HasPendingLines())
            {
                string line = DequeueHighestPriority(out int priority);
                PlayLine(line, priority);
                yield return new WaitForSeconds(_cooldownSeconds);
            }

            _isBusy = false;
        }

        private void PlayLine(string line, int priority)
        {
            string label = priority switch { 2 => "CRITICAL", 1 => "ADVISORY", _ => "TIP" };
            Debug.Log($"[ORION | {label}] {line}");

            // TODO: ElevenLabs integration
            // _ = ElevenLabsClient.StreamAudio(voiceId: ORIONVoiceId, text: line, onComplete: OnAudioComplete);
        }

        private string DequeueHighestPriority(out int priority)
        {
            for (int p = 2; p >= 0; p--)
            {
                if (_queues[p].Count > 0)
                {
                    priority = p;
                    return _queues[p].Dequeue();
                }
            }

            priority = 0;
            return string.Empty;
        }

        private bool HasPendingLines()
        {
            foreach (var q in _queues)
                if (q.Count > 0) return true;
            return false;
        }
    }
}
