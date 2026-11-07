using UnityEngine;
using UnityEngine.SceneManagement;

// The preparation scene is only an entry/return route to the shared modal.
public sealed class VesselSelectionScene : MonoBehaviour
{
    public static string PreviousScene = "BoosterSelection";
    void Start() { VesselSelectionScreen.Create(() => SceneManager.LoadScene(PreviousScene)); }
}
