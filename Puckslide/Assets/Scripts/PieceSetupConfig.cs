using UnityEngine;

[CreateAssetMenu(menuName = "Game/Piece Setup Config", fileName = "PieceSetupConfig")]
public class PieceSetupConfig : ScriptableObject
{
    [SerializeField]
    private PieceSetupData[] m_DefaultSetup;

    public PieceSetupData[] DefaultSetup => m_DefaultSetup;
}
