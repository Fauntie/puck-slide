using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField]
    private InputField m_PlayerNameInput;
    [SerializeField]
    private InputField m_SessionCodeInput;
    [SerializeField]
    private Text m_StatusLabel;

    private LobbyStateMachine m_Lobby;

    private void Awake()
    {
        m_Lobby = new LobbyStateMachine(new LoopbackTransport());
        UpdateStatus();
    }

    public void OnHostClicked()
    {
        m_Lobby.Host(m_PlayerNameInput.text, m_SessionCodeInput.text);
        UpdateStatus();
    }

    public void OnJoinClicked()
    {
        m_Lobby.Join(m_PlayerNameInput.text, m_SessionCodeInput.text);
        UpdateStatus();
    }

    public void OnReadyClicked()
    {
        m_Lobby.MarkReady();
        UpdateStatus();
    }

    public void OnStartClicked()
    {
        m_Lobby.BeginStart();
        UpdateStatus();
    }

    public void OnResetClicked()
    {
        m_Lobby.Reset();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (m_StatusLabel == null)
        {
            return;
        }

        switch (m_Lobby.State)
        {
            case LobbyState.Idle:
                m_StatusLabel.text = "Enter a name and session to host or join.";
                break;
            case LobbyState.Hosting:
                m_StatusLabel.text = $"Hosting {m_Lobby.SessionCode} as {m_Lobby.PlayerName}.";
                break;
            case LobbyState.Joining:
                m_StatusLabel.text = $"Joining {m_Lobby.SessionCode} as {m_Lobby.PlayerName}.";
                break;
            case LobbyState.Ready:
                m_StatusLabel.text = "Ready to start.";
                break;
            case LobbyState.Starting:
                m_StatusLabel.text = "Starting match...";
                break;
        }
    }
}
