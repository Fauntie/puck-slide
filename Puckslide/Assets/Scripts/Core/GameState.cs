using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;

[DataContract]
public struct Position : IEquatable<Position>
{
    [DataMember]
    public int X { get; set; }
    [DataMember]
    public int Y { get; set; }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(Position other) => X == other.X && Y == other.Y;
    public override bool Equals(object obj) => obj is Position other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }
}

[DataContract]
public struct Move
{
    [DataMember]
    public Position From { get; set; }
    [DataMember]
    public Position To { get; set; }

    public Move(Position from, Position to)
    {
        From = from;
        To = to;
    }
}

[DataContract]
public class GameState
{
    public static GameState Instance { get; set; } = new GameState();

    [DataMember]
    private Dictionary<Position, ChessPiece> m_Board = new Dictionary<Position, ChessPiece>();
    [DataMember]
    private List<ChessPiece> m_CapturedWhite = new List<ChessPiece>();
    [DataMember]
    private List<ChessPiece> m_CapturedBlack = new List<ChessPiece>();
    [DataMember]
    private bool m_WhiteTurn = true;

    public bool IsWhiteTurn => m_WhiteTurn;

    public IReadOnlyList<ChessPiece> CapturedWhite => m_CapturedWhite;
    public IReadOnlyList<ChessPiece> CapturedBlack => m_CapturedBlack;

    public Dictionary<Position, ChessPiece> GetLayout()
    {
        return new Dictionary<Position, ChessPiece>(m_Board);
    }

    public void SetPiece(Position pos, ChessPiece piece)
    {
        m_Board[pos] = piece;
    }

    public void Clear()
    {
        m_Board.Clear();
        m_CapturedWhite.Clear();
        m_CapturedBlack.Clear();
        m_WhiteTurn = true;
    }

    public void ApplyMove(Move m)
    {
        if (!m_Board.TryGetValue(m.From, out var piece))
        {
            return;
        }

        m_Board.Remove(m.From);

        if (m_Board.TryGetValue(m.To, out var captured))
        {
            if (IsWhitePiece(captured))
                m_CapturedBlack.Add(captured);
            else
                m_CapturedWhite.Add(captured);
        }

        m_Board[m.To] = piece;
        m_WhiteTurn = !m_WhiteTurn;
    }

    private bool IsWhitePiece(ChessPiece piece) => (int)piece >= 6;

    public string ToJson()
    {
        var serializer = new DataContractJsonSerializer(typeof(GameState));
        using (var ms = new MemoryStream())
        {
            serializer.WriteObject(ms, this);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }

    public static GameState FromJson(string json)
    {
        var serializer = new DataContractJsonSerializer(typeof(GameState));
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        {
            return (GameState)serializer.ReadObject(ms);
        }
    }

    public byte[] ToBinary()
    {
        using (var ms = new MemoryStream())
        {
            var bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011
            bf.Serialize(ms, this);
#pragma warning restore SYSLIB0011
            return ms.ToArray();
        }
    }

    public static GameState FromBinary(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        {
            var bf = new BinaryFormatter();
#pragma warning disable SYSLIB0011
            return (GameState)bf.Deserialize(ms);
#pragma warning restore SYSLIB0011
        }
    }

    public BoardLayoutMessage ToBoardLayoutMessage()
    {
        var entries = new List<BoardEntry>();
        foreach (var kvp in m_Board)
        {
            entries.Add(new BoardEntry { Pos = kvp.Key, Piece = kvp.Value });
        }

        return new BoardLayoutMessage
        {
            Board = entries,
            CapturedWhite = new List<ChessPiece>(m_CapturedWhite),
            CapturedBlack = new List<ChessPiece>(m_CapturedBlack),
            WhiteTurn = m_WhiteTurn
        };
    }

    public void ApplyBoardLayoutMessage(BoardLayoutMessage message)
    {
        m_Board = new Dictionary<Position, ChessPiece>();
        if (message.Board != null)
        {
            foreach (var entry in message.Board)
            {
                m_Board[entry.Pos] = entry.Piece;
            }
        }
        m_CapturedWhite = message.CapturedWhite != null
            ? new List<ChessPiece>(message.CapturedWhite)
            : new List<ChessPiece>();
        m_CapturedBlack = message.CapturedBlack != null
            ? new List<ChessPiece>(message.CapturedBlack)
            : new List<ChessPiece>();
        m_WhiteTurn = message.WhiteTurn;
    }

    public static MoveMessage ToMoveMessage(Move move)
    {
        return new MoveMessage { From = move.From, To = move.To };
    }

    public static Move FromMoveMessage(MoveMessage message)
    {
        return new Move(message.From, message.To);
    }
}
