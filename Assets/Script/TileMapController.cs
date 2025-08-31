using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapController : MonoBehaviour
{
    // �^�C���}�b�v�ƃ^�C��
    [SerializeField] Tilemap tilemap;
    [SerializeField] Tile whiteTile;
    [SerializeField] Tile blackTile;
    [SerializeField] Tile grayTile;

    // �}�b�v�̍ő�T�C�Y�A���E�����i�^�C���̍ő吔�j
    static readonly int MapWidth = 50;
    static readonly int MapHeight = 30;

    void Start()
    {
        Func();
    }

    void Func()
    {
        // ���S�iy = 0, x = 0�j

        // �����ɕ\���A������(0, 0)
        tilemap.SetTile(ConvertPosition(0, 0), blackTile);
        // ���S�ɕ\��
        tilemap.SetTile(ConvertPosition(MapWidth / 2, MapHeight / 2), whiteTile);
        // �E��ɕ\���A�E���(50, 30)
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
