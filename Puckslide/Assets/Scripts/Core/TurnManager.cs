using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

[DataContract]
public class TurnManager
{
    public static TurnManager Instance { get; set; } = new TurnManager();

    [DataMember]
    private PlayerColor m_CurrentPlayer = PlayerColor.White;

    [IgnoreDataMember]
    private PuckController m_ActivePuck;

    [IgnoreDataMember]
    private IEventBus m_EventBus;

    private IEventBus Bus => m_EventBus ??= Object.FindObjectOfType<EventBusBootstrap>()?.Bus;

    public PlayerColor CurrentPlayer => m_CurrentPlayer;
    public bool IsWhiteTurn => m_CurrentPlayer == PlayerColor.White;
    public PuckController ActivePuck
    {
        get => m_ActivePuck;
        set => m_ActivePuck = value;
    }

    public void BeginTurn(PlayerColor c)
    {
        m_CurrentPlayer = c;
        m_ActivePuck = null;
        Bus?.Publish(EventBusEvents.TurnChanged, IsWhiteTurn);
    }

    public void EndTurn()
    {
        m_CurrentPlayer = m_CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        m_ActivePuck = null;
        Bus?.Publish(EventBusEvents.TurnChanged, IsWhiteTurn);
    }

    public string ToJson()
    {
        var serializer = new DataContractJsonSerializer(typeof(TurnManager));
        using (var ms = new MemoryStream())
        {
            serializer.WriteObject(ms, this);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }

    public static TurnManager FromJson(string json)
    {
        var serializer = new DataContractJsonSerializer(typeof(TurnManager));
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        {
            return (TurnManager)serializer.ReadObject(ms);
        }
    }
}

public enum PlayerColor
{
    White,
    Black
}
