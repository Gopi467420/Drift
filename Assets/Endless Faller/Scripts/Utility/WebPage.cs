using System.Runtime.InteropServices;
using UnityEngine;

// A browser can't close its own tab, so on the web Quit reloads the page instead,
// which starts the game fresh. Everywhere else it quits as normal.
public static class WebPage
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // In Assets/Plugins/WebGL/WebPage.jslib.
    [DllImport("__Internal")]
    private static extern void ReloadPage();
#endif

    public static void Quit()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ReloadPage();
#else
        Application.Quit();
#endif
    }
}
