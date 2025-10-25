using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Piece Setup Config", fileName = "PieceSetupConfig")]
public class PieceSetupConfig : ScriptableObject
{
    [SerializeField]
    private PieceSetupData[] m_DefaultSetup;

    public PieceSetupData[] CreateSetup()
    {
        if (m_DefaultSetup == null)
        {
            return System.Array.Empty<PieceSetupData>();
        }

        return m_DefaultSetup
            .Where(data => data != null)
            .Select(data => data.Clone())
            .ToArray();
    }
}
