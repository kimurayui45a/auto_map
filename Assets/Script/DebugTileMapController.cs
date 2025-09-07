using UnityEngine;
using UnityEngine.Tilemaps;

public class DebugTileMapController : MonoBehaviour
{
    [Header("Assign debug Tilemap & Tile")]
    [SerializeField] private Tilemap debugTilemap;   // Debug�p�^�C���}�b�v�i�ʃ��C���j
    [SerializeField] private TileBase borderTile;    // ���E���ɒu���^�C��

    [Header("Map settings (TileMapController�Ƒ�����)")]
    public static readonly int MapWidth = 50;
    public static readonly int MapHeight = 30;

    [Header("Partition settings")]
    [SerializeField] private int divX = 3;           // ���̋�搔
    [SerializeField] private int divY = 3;           // �c�̋�搔
    [SerializeField] private int gap = 1;           // ���Ԃ̋��E��(�Z��)
    [SerializeField] private bool startFromTopLeft = true; // ����X�^�[�g�ŋ���u�����Ȃ�true

    // ���E/�㉺�ɊO�������������ꍇ�����Œ����i�s�v�Ȃ�0�̂܂܁j
    [SerializeField] private int leftPad = 0;
    [SerializeField] private int rightPad = 0;
    [SerializeField] private int bottomPad = 0;
    [SerializeField] private int topPad = 0;

    // 0=��, 1=���E
    private int[,] debugMap;

    // �{�^������Ă�
    [ContextMenu("Draw Borders (Debug)")]
    public void DrawBorders()
    {
        InitDebugMap();
        PaintBorders();
        SetDebugTiles();
    }

    private void InitDebugMap()
    {
        debugMap = new int[MapHeight, MapWidth]; // �S��0
        if (debugTilemap) debugTilemap.ClearAllTiles();
    }

    // ���S���_�ɍ��킹��ϊ��iTileMapController�Ɠ����v�Z�j
    private Vector3Int ConvertPosition(int x, int y)
    {
        int cx = MapWidth / 2;
        int cy = MapHeight / 2;
        return new Vector3Int(x - cx, y - cy, 0);
    }

    private void PaintBorders()
    {
        // �d�l�ʂ� ((�ő�/3) - 1) �ŋ��T�C�Y������
        int sectionW = (MapWidth - leftPad - rightPad) / divX - 1;
        int sectionH = (MapHeight - bottomPad - topPad) / divY - 1;

        // �c�̋��E�i��j�c�e���̉E�[�̂����E=�M���b�v��
        for (int ix = 0; ix < divX - 1; ix++)
        {
            int x0 = leftPad + ix * (sectionW + gap);
            int xGap = x0 + sectionW;                 // �M���b�v���X
            if (xGap < 0 || xGap >= MapWidth) continue;

            for (int y = 0; y < MapHeight; y++)
                debugMap[y, xGap] = 1;
        }

        // ���̋��E�i�s�j
        for (int iy = 0; iy < divY - 1; iy++)
        {
            int yGap;
            if (startFromTopLeft)
            {
                // ����X�^�[�g�ŋ���`�����ꍇ�F��̋��̉���1�Z���̃M���b�v
                int y0 = MapHeight - topPad - sectionH - iy * (sectionH + gap);
                yGap = y0 - 1;
            }
            else
            {
                // �����X�^�[�g�̏ꍇ�F���̋��̏��1�Z���̃M���b�v
                int y0 = bottomPad + iy * (sectionH + gap);
                yGap = y0 + sectionH;
            }

            if (yGap < 0 || yGap >= MapHeight) continue;

            for (int x = 0; x < MapWidth; x++)
                debugMap[yGap, x] = 1;
        }
    }

    private void SetDebugTiles()
    {
        // ���ݒ�Ȃ牽�����Ȃ��ŏI��
        if (!debugTilemap || !borderTile) { Debug.LogWarning("���A�T�C��"); return; }

        for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
            {
                if (debugMap[y, x] == 1)
                    debugTilemap.SetTile(ConvertPosition(x, y), borderTile);
            }
    }
}
