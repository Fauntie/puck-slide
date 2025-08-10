using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PromotionPanel : MonoBehaviour
{
    public static PromotionPanel Instance;

    [SerializeField]
    private GameObject m_Panel;

    [SerializeField]
    private GameObject m_PiecePrefab;

    private Piece m_PawnToReplace;
    private Tile m_PawnTile;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        m_Panel.SetActive(false);
    }

    public void ShowPanel(Piece pawn, Tile tile)
    {
        m_PawnToReplace = pawn;
        m_PawnTile = tile;
        m_Panel.SetActive(true);
    }

    private void HidePanel()
    {
        m_Panel.SetActive(false);
        m_PawnToReplace = null;
        m_PawnTile = null;
    }

    
    public void OnPromotionButtonClicked(int pieceIndex)
    {
        if (m_PawnToReplace != null && m_PawnTile != null)
        {
            Destroy(m_PawnToReplace.gameObject);
            m_PawnTile.ClearTile();

            ChessPiece newPieceType = ConvertIndexToEnum(pieceIndex);
            GameObject newPieceObj = Instantiate(m_PiecePrefab, m_PawnTile.transform.position, Quaternion.identity);

            Piece pieceScript = newPieceObj.GetComponent<Piece>();
            if (pieceScript != null)
            {
                pieceScript.SetupPiece(newPieceType);
                m_PawnTile.SetPiece(pieceScript);
                pieceScript.SetTile(m_PawnTile);
            }
            else
            {
                Debug.LogError("Your m_PiecePrefab is missing a Piece script!");
            }
        }

        HidePanel();
    }

    private ChessPiece ConvertIndexToEnum(int index)
    {
        switch (index)
        {
            case 0: return ChessPiece.W_Bishop;
            case 1: return ChessPiece.W_Knight;
            case 2: return ChessPiece.W_Rook;
            case 3: return ChessPiece.W_Queen;
            case 4: return ChessPiece.B_Bishop;
            case 5: return ChessPiece.B_Knight;
            case 6: return ChessPiece.B_Rook;
            case 7: return ChessPiece.B_Queen;
        }
        return ChessPiece.W_Queen;
    }
}
