public readonly struct PuckStoppedEvent
{
    public PuckController Puck { get; }
    public bool HasReachedBoard { get; }
    public bool IsWhitePiece { get; }

    public PuckStoppedEvent(PuckController puck, bool hasReachedBoard, bool isWhitePiece)
    {
        Puck = puck;
        HasReachedBoard = hasReachedBoard;
        IsWhitePiece = isWhitePiece;
    }
}
