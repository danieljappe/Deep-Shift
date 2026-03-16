// Bootstrap scene entry point.
//
// The Bootstrap scene (build index 0) contains one persistent GameObject with
// the following components — all use DontDestroyOnLoad and survive every scene load:
//
//   • SceneController  — drives all scene transitions (ShiftStarted → Mine, ShiftComplete → SurfaceCamp)
//   • GameManager      — owns GameState enum and ChangeState()
//   • EconomyManager   — owns all currency values and JSON save/load
//
// SceneController.Start() immediately loads SurfaceCamp, replacing the Bootstrap scene.
// No MonoBehaviour is needed here — this file is documentation only.
