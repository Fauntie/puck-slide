using UnityEngine;
using UnityEngine.SceneManagement;
using Puckslide.Networking;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject m_MainMenuRoot;
    [SerializeField] private GameObject m_LobbyRoot;
    [SerializeField] private GameObject m_LocalPlayRoot;

    public void OnLocalPlayClicked()
    {
        if (m_MainMenuRoot != null) m_MainMenuRoot.SetActive(false);
        if (m_LocalPlayRoot != null) m_LocalPlayRoot.SetActive(true);

        var nsm = FindObjectOfType<NetworkSessionManager>();
        if (nsm != null)
        {
            nsm.SetOfflineMode(true);
        }

        // You can either:
        // - Stay in this scene and just disable networking
        // - Or call SceneManager.LoadScene("GameSceneLocal");
    }

    public void OnOnlinePlayClicked()
    {
        if (m_MainMenuRoot != null) m_MainMenuRoot.SetActive(false);
        if (m_LobbyRoot != null) m_LobbyRoot.SetActive(true);

        // Optionally initialize Steam here:
        if (SteamworksBootstrap.Instance != null && !SteamworksBootstrap.Instance.Initialized)
        {
            SteamworksBootstrap.Instance.Initialize();
        }
    }

    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
