using System;

public enum LobbyState
{
    Idle,
    Hosting,
    Joining,
    Ready,
    Starting
}

public class LobbyStateMachine
{
    private readonly NetTransport m_Transport;

    public LobbyStateMachine(NetTransport transport)
    {
        m_Transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public LobbyState State { get; private set; } = LobbyState.Idle;
    public string PlayerName { get; private set; } = string.Empty;
    public string SessionCode { get; private set; } = string.Empty;
    public bool IsHost => m_Transport.IsHosting;
    public NetTransport Transport => m_Transport;

    public void Host(string playerName, string sessionCode)
    {
        EnsureIdle();
        PlayerName = ValidateName(playerName);
        SessionCode = ValidateSessionCode(sessionCode);

        int port = LobbySessionCodeUtility.GetPort(SessionCode);
        m_Transport.Host(port);
        State = LobbyState.Hosting;
    }

    public void Join(string playerName, string sessionCode)
    {
        EnsureIdle();
        PlayerName = ValidateName(playerName);
        SessionCode = ValidateSessionCode(sessionCode);

        int port = LobbySessionCodeUtility.GetPort(SessionCode);
        m_Transport.Connect("localhost", port);
        State = LobbyState.Joining;
    }

    public void MarkReady()
    {
        if (State != LobbyState.Hosting && State != LobbyState.Joining)
        {
            throw new InvalidOperationException("Ready can only be set while hosting or joining.");
        }

        State = LobbyState.Ready;
    }

    public void BeginStart()
    {
        if (State != LobbyState.Ready)
        {
            throw new InvalidOperationException("Start can only be triggered from the Ready state.");
        }

        State = LobbyState.Starting;
    }

    public void Reset()
    {
        State = LobbyState.Idle;
        PlayerName = string.Empty;
        SessionCode = string.Empty;
    }

    private static string ValidateName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            throw new ArgumentException("Player name is required.", nameof(playerName));
        }

        return playerName.Trim();
    }

    private static string ValidateSessionCode(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            throw new ArgumentException("Session code is required.", nameof(sessionCode));
        }

        return sessionCode.Trim();
    }

    private void EnsureIdle()
    {
        if (State != LobbyState.Idle)
        {
            throw new InvalidOperationException("Lobby is already active.");
        }
    }
}

public static class LobbySessionCodeUtility
{
    private const int k_BasePort = 7000;
    private const int k_PortRange = 1000;

    public static int GetPort(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            throw new ArgumentException("Session code is required.", nameof(sessionCode));
        }

        string trimmed = sessionCode.Trim();
        uint hash = unchecked((uint)trimmed.GetHashCode());
        return k_BasePort + (int)(hash % k_PortRange);
    }
}
