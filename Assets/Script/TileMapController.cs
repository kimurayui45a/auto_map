using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapController : MonoBehaviour
{
    // タイルマップとタイル
    [SerializeField] Tilemap tilemap;
    [SerializeField] Tile whiteTile;
    [SerializeField] Tile blackTile;
    [SerializeField] Tile grayTile;

    // マップの最大サイズ、幅・高さ（タイルの最大数）
    static readonly int MapWidth = 50;
    static readonly int MapHeight = 30;

    void Start()
    {
        Func();
    }

    void Func()
    {
        // 中心（y = 0, x = 0）

        // 左下に表示、左下を(0, 0)
        tilemap.SetTile(ConvertPosition(0, 0), blackTile);
        // 中心に表示
        tilemap.SetTile(ConvertPosition(MapWidth / 2, MapHeight / 2), whiteTile);
        // 右上に表示、右上を(50, 30)
        tilemap.SetTile(ConvertPosition(MapWidth, MapHeight), whiteTile);
    }

    Vector3Int ConvertPosition(int x, int y)
    {
        int cx = MapWidth / 2;
        int cy = MapHeight / 2;
        return new Vector3Int(x - cx, y - cy, 0);
    }

    //void Update()
    //{

    //}
}
