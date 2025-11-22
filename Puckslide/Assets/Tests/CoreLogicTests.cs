using NUnit.Framework;

public class PieceSetupStateTests
{
    [Test]
    public void IncreaseCount_StopsAtPerColorLimit()
    {
        var startingSetup = new[]
        {
            new PieceSetupData
            {
                Type = ChessPieceType.Pawn,
                WhiteCount = PieceSetupState.MaxPiecesPerColor,
                BlackCount = 0,
                Sticky = false
            }
        };

        var state = new PieceSetupState(startingSetup);

        bool result = state.IncreaseCount(ChessPieceType.Pawn, true);

        Assert.IsFalse(result, "White count should not exceed the per-color cap.");
        Assert.AreEqual(PieceSetupState.MaxPiecesPerColor, state.GetCount(ChessPieceType.Pawn, true));
    }

    [Test]
    public void ToggleSticky_UpdatesFlag()
    {
        var state = new PieceSetupState(PieceSetupState.CreateDefaultSetup());

        bool changed = state.ToggleSticky(ChessPieceType.Knight, true);

        Assert.IsTrue(changed);
        Assert.IsTrue(state.GetSticky(ChessPieceType.Knight));
    }
}

public class EvtTests
{
    [Test]
    public void AddListenerWithReplay_ReceivesLastValue()
    {
        var evt = new Evt<int>();
        evt.Invoke(5);

        int received = 0;
        evt.AddListener(value => received = value, true);

        Assert.AreEqual(5, received);
    }
}
