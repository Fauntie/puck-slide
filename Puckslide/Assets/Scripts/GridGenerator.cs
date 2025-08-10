using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [SerializeField] private GameObject m_BlackTilePrefab;
    [SerializeField] private GameObject m_WhiteTilePrefab;
    [SerializeField] private int m_GridSize = 8;
    [SerializeField] private float m_TileSize = 0.383f;

    private void Awake()
    {
        BoardFlipper.SetBoard(transform, m_GridSize, m_TileSize);
    }

    private void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < m_GridSize; x++)
        {
            for (int y = 0; y < m_GridSize; y++)
            {
                GameObject tilePrefab = (x + y) % 2 == 0 ? m_BlackTilePrefab : m_WhiteTilePrefab;

                Vector2 tilePosition = new Vector2(x * m_TileSize, y * m_TileSize);
                GameObject tile = Instantiate(tilePrefab, transform);
                tile.transform.localPosition = tilePosition;
                tile.transform.localScale = new Vector3(m_TileSize, m_TileSize, 1);
            }
        }
    }
}
