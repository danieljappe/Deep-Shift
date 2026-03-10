using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepShift.Core
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string _surfaceCampScene = "SurfaceCamp";

        private void Awake()
        {
            SceneManager.LoadScene(_surfaceCampScene, LoadSceneMode.Additive);
        }
    }
}
