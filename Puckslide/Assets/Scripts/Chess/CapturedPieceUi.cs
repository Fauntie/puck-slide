using UnityEngine;
using UnityEngine.UI;

public class CapturedPieceUi : MonoBehaviour
{
    [SerializeField]
    private Image m_Image;
    [SerializeField]
    private Sprite[] m_Sprites;
    
    
    public void SetupCapturedUiPiece(ChessPiece chessPiece)
    {
        int index = (int)chessPiece;

        if(index >= 0 && index < m_Sprites.Length)
        {
            m_Image.sprite = m_Sprites[index];
        }
        else
        {
            Debug.LogError($"Invalid sprite index for chessPiece: {chessPiece}");
        }
    }
}
