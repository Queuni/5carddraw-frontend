using System.Collections;
using UnityEngine;

public class SplashScene : MonoBehaviour
{
    public float displayDuration = 1.7f;

    private void Awake()
    {

#if UNITY_STANDALONE_WIN
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(1280, 720, false);
        Screen.fullScreen = false;
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
        AppBootstrap.EnsureManagers();
        ResourceManager.LoadAll();

        StartCoroutine(SplashSequence());
    }

    private IEnumerator SplashSequence()
    {
        yield return new WaitForSeconds(displayDuration);

        Utils.LoadScene("LoginScene");
    }
}
