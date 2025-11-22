using System;

[Serializable]
public class PieceSetupData
{
    public ChessPieceType Type;
    public int WhiteCount;
    public int BlackCount;
    public bool Sticky;

    public PieceSetupData Clone()
    {
        return new PieceSetupData
        {
            Type = Type,
            WhiteCount = WhiteCount,
            BlackCount = BlackCount,
            Sticky = Sticky
        };
    }
}
