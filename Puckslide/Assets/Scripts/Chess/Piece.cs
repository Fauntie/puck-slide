using UnityEngine;

public enum ChessPiece
{
    B_Bishop, B_King, B_Knight, B_Pawn, B_Queen, B_Rook,
    W_Bishop, W_King, W_Knight, W_Pawn, W_Queen, W_Rook
}

public class Piece : MonoBehaviour
{

    [SerializeField]
    private Sprite[] m_Sprites;

    [SerializeField]
    private SpriteRenderer m_SpriteRenderer;

    private Tile m_CurrentTile;
    private ChessPiece m_ChessPiece;
    
    public Tile GetCurrentTile() => m_CurrentTile;
    public void SetTile(Tile tile) => m_CurrentTile = tile;
    
    public ChessPiece GetChessPiece() => m_ChessPiece;

    public void SetupPiece(ChessPiece chessPiece)
    {
        m_ChessPiece = chessPiece;
        int index = (int)chessPiece;

        if(index >= 0 && index < m_Sprites.Length)
        {
            m_SpriteRenderer.sprite = m_Sprites[index];
        }
        else
        {
            Debug.LogError($"Invalid sprite index for chessPiece: {chessPiece}");
        }
    }

    public bool IsWhite()
    {
        return (int)m_ChessPiece >= 6;
    }

    public bool IsPawn()
    {
        return m_ChessPiece is ChessPiece.B_Pawn or ChessPiece.W_Pawn;
    }
}