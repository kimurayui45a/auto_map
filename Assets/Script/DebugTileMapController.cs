using UnityEngine;
using UnityEngine.Tilemaps;

public class DebugTileMapController : MonoBehaviour
{
    [Header("Assign debug Tilemap & Tile")]
    [SerializeField] private Tilemap debugTilemap;   // Debug用タイルマップ（別レイヤ）
    [SerializeField] private TileBase borderTile;    // 境界線に置くタイル

    [Header("Map settings (TileMapControllerと揃える)")]
    public static readonly int MapWidth = 50;
    public static readonly int MapHeight = 30;

    [Header("Partition settings")]
    [SerializeField] private int divX = 3;           // 横の区画数
    [SerializeField] private int divY = 3;           // 縦の区画数
    [SerializeField] private int gap = 1;           // 区画間の境界幅(セル)
    [SerializeField] private bool startFromTopLeft = true; // 左上スタートで区画を置いたならtrue

    // 左右/上下に外周をあけたい場合ここで調整（不要なら0のまま）
    [SerializeField] private int leftPad = 0;
    [SerializeField] private int rightPad = 0;
    [SerializeField] private int bottomPad = 0;
    [SerializeField] private int topPad = 0;

    // 0=空, 1=境界
    private int[,] debugMap;

    // ボタンから呼ぶ
    [ContextMenu("Draw Borders (Debug)")]
    public void DrawBorders()
    {
        InitDebugMap();
        PaintBorders();
        SetDebugTiles();
    }

    private void InitDebugMap()
    {
        debugMap = new int[MapHeight, MapWidth]; // 全部0
        if (debugTilemap) debugTilemap.ClearAllTiles();
    }

    // 中心原点に合わせる変換（TileMapControllerと同じ計算）
    private Vector3Int ConvertPosition(int x, int y)
    {
        int cx = MapWidth / 2;
        int cy = MapHeight / 2;
        return new Vector3Int(x - cx, y - cy, 0);
    }

    private void PaintBorders()
    {
        // 仕様通り ((最大/3) - 1) で区画サイズを決定
        int sectionW = (MapWidth - leftPad - rightPad) / divX - 1;
        int sectionH = (MapHeight - bottomPad - topPad) / divY - 1;

        // 縦の境界（列）…各区画の右端のすぐ右=ギャップ列
        for (int ix = 0; ix < divX - 1; ix++)
        {
            int x0 = leftPad + ix * (sectionW + gap);
            int xGap = x0 + sectionW;                 // ギャップ列のX
            if (xGap < 0 || xGap >= MapWidth) continue;

            for (int y = 0; y < MapHeight; y++)
                debugMap[y, xGap] = 1;
        }

        // 横の境界（行）
        for (int iy = 0; iy < divY - 1; iy++)
        {
            int yGap;
            if (startFromTopLeft)
            {
                // 左上スタートで区画を描いた場合：上の区画の下に1セルのギャップ
                int y0 = MapHeight - topPad - sectionH - iy * (sectionH + gap);
                yGap = y0 - 1;
            }
            else
            {
                // 左下スタートの場合：下の区画の上に1セルのギャップ
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
        // 未設定なら何もしないで終了
        if (!debugTilemap || !borderTile) { Debug.LogWarning("未アサイン"); return; }

        for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
            {
                if (debugMap[y, x] == 1)
                    debugTilemap.SetTile(ConvertPosition(x, y), borderTile);
            }
    }
}
