using Puckslide.Networking;
using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [Header("UI Root References")]
    [SerializeField] private GameObject m_LobbyUIRoot;
    [SerializeField] private GameObject m_GameUIRoot;

    [Header("Optional")]
    [SerializeField] private GameObject m_SetupPhaseRoot;
    [SerializeField] private GameObject m_PuckPhaseRoot;

    private void OnEnable()
    {
        NetworkEvents.OnGameStart.AddListener(OnGameStart);
    }

    private void OnDisable()
    {
        NetworkEvents.OnGameStart.RemoveListener(OnGameStart);
    }

    private void Start()
    {
        if (m_LobbyUIRoot != null)
        {
            m_LobbyUIRoot.SetActive(true);
        }

        if (m_GameUIRoot != null)
        {
            m_GameUIRoot.SetActive(false);
        }
    }

    private void OnGameStart(GameStartMessage _)
    {
        if (m_LobbyUIRoot != null)
        {
            m_LobbyUIRoot.SetActive(false);
        }

        if (m_GameUIRoot != null)
        {
            m_GameUIRoot.SetActive(true);
        }

        if (m_SetupPhaseRoot != null)
        {
            m_SetupPhaseRoot.SetActive(true);
        }

        if (m_PuckPhaseRoot != null)
        {
            m_PuckPhaseRoot.SetActive(false);
        }
    }
}
