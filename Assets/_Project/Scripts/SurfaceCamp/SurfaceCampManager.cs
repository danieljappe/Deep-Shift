using UnityEngine;
using DeepShift.Mining;

namespace DeepShift.SurfaceCamp
{
    /// <summary>
    /// Manages the SurfaceCamp scene. Teleports the player to a designer-placed spawn
    /// point when the scene loads. The spawn point is a Transform reference you position
    /// freely in the Inspector — just create an empty GameObject, name it "PlayerSpawn",
    /// and drag it here.
    ///
    /// The player GameObject in the SurfaceCamp scene uses the same <see cref="PlayerController"/>
    /// as the Mine scene but with no MineGrid assigned — it moves freely without tile collision.
    ///
    /// Attach to a persistent GameObject in SurfaceCamp (e.g. "SurfaceCampManager").
    /// </summary>
    public class SurfaceCampManager : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("Position the player here when the camp scene loads. Create an empty GameObject and place it at your desired start position.")]
        [SerializeField] private Transform _playerSpawnPoint;

        private void Start()
        {
            if (_playerSpawnPoint == null) return;

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                player.TeleportTo(_playerSpawnPoint.position);
        }
    }
}
