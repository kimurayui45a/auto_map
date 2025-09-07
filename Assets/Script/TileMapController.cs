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

    // 区画分割と境界幅（1～3の値で設定）
    [Header("区画数の設定")]
    [Tooltip("横方向の区画数の最小と最大（1～3の範囲）")]
    [SerializeField] Vector2Int divXRange = new Vector2Int(1, 3);
    [Tooltip("縦方向の区画数の最小と最大（1～3の範囲）")]
    [SerializeField] Vector2Int divYRange = new Vector2Int(1, 3);

    // 各部屋の最小サイズ(W,H)
    [Tooltip("部屋の最小サイズ（幅, 高さ）")]
    [SerializeField] Vector2Int roomMinSize = new Vector2Int(3, 3);

    // 現在の分割数（毎回ランダム決定）
    int DivX, DivY;

    // 区画間の境界線セル幅
    const int Gap = 1;

    // 外周
    const int LeftPad = 1;  // 左
    const int RightPad = 0; // 右

    // 区画のサイズ
    int sectionW;
    int sectionH;

    // 各区画の位置を決めるパラメータ
    int x0;
    int y0;
    int x1;
    int y1;

    // 部屋のサイズを決めるパラメータ
    int roomX0;
    int roomY0;
    int roomX1;
    int roomY1;

    // 二次元配列（長方形配列）を参照する変数
    int[,] map;

    // ===== ランダム設定 =====
    [Header("ランダム設定")]
    [Tooltip("各区画に部屋を作る確率（1で必ず作成）")]
    [SerializeField, Range(0f, 1f)]
    float roomSpawnRate = 1.0f;

    [Tooltip("通路を作る確率（1で必ず作成）、両側の部屋が同意した場合のみ接続")]
    [SerializeField, Range(0f, 1f)]
    float corridorSpawnRate = 1.0f;

    // ===== どの方向にも通路ができなかった場合の設定 =====
    [Tooltip("この部屋がどこかと最低1本は繋がるように保証する設定")]
    [SerializeField]
    bool ensureAtLeastOneExitPerRoom = true;

    // ===== 保存処理 =====
    // 各区画に確定した“部屋の矩形”を保存（通路生成でも参照）
    bool[,] hasRoom;      // 部屋を作ったか
    int[,] roomX0A, roomX1A, roomY0A, roomY1A;  // [iy, ix] ごとの部屋矩形

    void Start()
    {
        // 初回全面クリア
        InitMap();

        // 30×50 の全セルに 1 を入れる
        FillAll(1);

        // 全面白にする
        SetTile(map, tilemap);
    }

    // ボタンから呼ぶ
    // 3×3 区画を ((最大/3) - 1) のサイズで白(1)にする
    public void OnClickDivide()
    {
        // 今回の分割数を決める
        RandomizeDivisions();

        // 初回全面クリア
        InitMap();

        // 区画ごとに“内側”へ部屋を（白）を敷く
        //PaintRooms();
        PaintRoomsRandom();

        // 出口を作り、境界まで 2（黒） をで敷く
        PaintCorridors();

        // タイルの配置を実行する
        SetTile(map, tilemap);
    }

    // タイルをリセットする関数
    void InitMap()
    {
        // 高さ30 × 幅50 の 二次元配列を作成
        // 中身は全て0である
        map = new int[MapHeight, MapWidth];

        // シーン上の Tilemap から既存のタイルを全消去
        tilemap.ClearAllTiles();

        // ランダム用の状態配列を初期化
        hasRoom = new bool[DivY, DivX];
        roomX0A = new int[DivY, DivX];
        roomX1A = new int[DivY, DivX];
        roomY0A = new int[DivY, DivX];
        roomY1A = new int[DivY, DivX];
    }

    // 2次元配列 map の全セルを引数 v の値で埋める関数
    void FillAll(int v)
    {
        // map.GetLength(d)：配列の d 次元目の要素数を返すメソッド（System.Array）
        int h = map.GetLength(0), w = map.GetLength(1);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                map[y, x] = v; // 各配列にvの値が入る
    }

    // 中心をマップサイズ基準とするための関数
    Vector3Int ConvertPosition(int x, int y)
    {
        int cx = MapWidth / 2;
        int cy = MapHeight / 2;
        return new Vector3Int(x - cx, y - cy, 0);
    }

    // 各配置の中身の数値を確認し、配置するタイルの色を決める関数
    void SetTile(int[,] map, Tilemap tilemap)
    {
        int h = map.GetLength(0), w = map.GetLength(1);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (map[y, x] == 1)
                {
                    tilemap.SetTile(ConvertPosition(x, y), whiteTile);
                }
                else if (map[y, x] == 2)
                {
                    tilemap.SetTile(ConvertPosition(x, y), blackTile);
                }
            }
        }
    }

    // 区画サイズを計算する関数
    void SectionSize()
    {
        sectionW = (MapWidth - (DivX - 1) * Gap - LeftPad - RightPad) / DivX; // 幅は厳密式
        sectionH = (MapHeight / DivY) - 1;
    }

    // 区画の設定をする関数
    void RoomPosition(int iy, int ix) 
    {

        // 各区画のスタート位置を決める
        // 各区画の左下始点（Gapを入れて等間隔に配置）
        // この区画の左下X：前区画の幅＋境界1セルぶん右へずらす
        x0 = LeftPad + ix * (sectionW + Gap);
        y0 = MapHeight - sectionH - iy * (sectionH + Gap);

        // マップ外に出ないようにクランプしつつ塗る
        // クランプ：値をある範囲からはみ出さないように“挟み込む／切り詰めること
        x1 = Mathf.Min(x0 + sectionW, MapWidth);
        y1 = Mathf.Min(y0 + sectionH, MapHeight);

    }

    // 分割数を決める
    void RandomizeDivisions()
    {
        // Range(int,int) は上限が排他的なので +1 する
        DivX = UnityEngine.Random.Range(divXRange.x, divXRange.y + 1);
        DivY = UnityEngine.Random.Range(divYRange.x, divYRange.y + 1);

        // 念のためクランプ（Inspectorで不正値が入った場合に備えて）
        DivX = Mathf.Clamp(DivX, 1, 3);
        DivY = Mathf.Clamp(DivY, 1, 3);
    }

    // 区画ごとに部屋サイズと位置をランダムに決めて白で塗る
    // 決めた矩形は room* 配列に保存して、後段の通路生成で使う
    void PaintRoomsRandom()
    {
        // 区画サイズを計算
        SectionSize();

        // 縦方向の区画インデックスを0..2で回す
        for (int iy = 0; iy < DivY; iy++)
        {
            // 横方向の区画インデックスを0..2で回す
            for (int ix = 0; ix < DivX; ix++)
            {
                // 各区画のスタート位置を決める
                RoomPosition(iy, ix);

                // 区画サイズ
                int availW = x1 - x0;
                int availH = y1 - y0;

                // 最低でも左右上下に1セルの通路 + 最小部屋サイズを満たせるか
                // 部屋の最小サイズに 左右上下の通路1セルずつ（合計2セル） を足した大きさが置けないなら、その区画には部屋を作れないのでスキップ
                if (availW < roomMinSize.x + 2 || availH < roomMinSize.y + 2)
                {
                    hasRoom[iy, ix] = false;
                    continue;
                }

                // その区画に部屋を作るか（確率）
                if (UnityEngine.Random.value > roomSpawnRate)
                {
                    hasRoom[iy, ix] = false;
                    continue;
                }

                // ランダムな部屋サイズ
                // int の Range は上限排他なので (max+1) に注意
                int roomW = UnityEngine.Random.Range(roomMinSize.x, availW - 1); // ～ availW-2 まで
                int roomH = UnityEngine.Random.Range(roomMinSize.y, availH - 1); // ～ availH-2 まで

                // ランダムな部屋位置（通路=1セルを左右上下に必ず確保）
                int freeW = availW - roomW; // 通路幅の合計（左右）
                int freeH = availH - roomH; // 通路幅の合計（上下）

                int leftMargin = UnityEngine.Random.Range(1, freeW); // 右も1以上確保される
                int bottomMargin = UnityEngine.Random.Range(1, freeH); // 上も1以上確保される

                int rightMargin = freeW - leftMargin;
                int topMargin = freeH - bottomMargin;

                roomX0 = x0 + leftMargin;
                roomX1 = x1 - rightMargin;
                roomY0 = y0 + bottomMargin;
                roomY1 = y1 - topMargin;

                // 保存
                hasRoom[iy, ix] = true;
                roomX0A[iy, ix] = roomX0; roomX1A[iy, ix] = roomX1;
                roomY0A[iy, ix] = roomY0; roomY1A[iy, ix] = roomY1;

                // 白で塗る
                for (int y = roomY0; y < roomY1; y++)
                    for (int x = roomX0; x < roomX1; x++)
                        map[y, x] = 1;
            }
        }
    }

    // 通路生成（黒タイル）、両側が同意した境界のみ接続
    void PaintCorridors()
    {
        // まずは各境界に「片側の出口提案」を記録するだけ（描画しない）
        // 垂直境界（左右の区画間）：行=iy, 列=境界index(0..DivX-2)
        int[,] vBoundaryX = new int[DivY, DivX - 1];
        int[,] vDoorYLeft = new int[DivY, DivX - 1];   // 左側区画の出口Y
        int[,] vDoorYRight = new int[DivY, DivX - 1];   // 右側区画の出口Y
        bool[,] vHasLeft = new bool[DivY, DivX - 1];
        bool[,] vHasRight = new bool[DivY, DivX - 1];

        // 水平境界（上下の区画間）：行=境界index(0..DivY-2), 列=ix
        int[,] hBoundaryY = new int[DivY - 1, DivX];
        int[,] hDoorXLower = new int[DivY - 1, DivX];   // 下側区画（現在の iy）から上への出口X
        int[,] hDoorXUpper = new int[DivY - 1, DivX];   // 上側区画（iy-1）から下への出口X
        bool[,] hHasLower = new bool[DivY - 1, DivX];
        bool[,] hHasUpper = new bool[DivY - 1, DivX];

        // 区画サイズを計算
        SectionSize();

        for (int iy = 0; iy < DivY; iy++)
        {
            for (int ix = 0; ix < DivX; ix++)
            {
                if (!hasRoom[iy, ix]) continue;

                // 各区画のスタート位置を決める
                RoomPosition(iy, ix);

                // この部屋の矩形
                int rX0 = roomX0A[iy, ix], rX1 = roomX1A[iy, ix];
                int rY0 = roomY0A[iy, ix], rY1 = roomY1A[iy, ix];

                bool hasLeft = ix > 0;
                bool hasRight = ix < DivX - 1;
                bool hasUp = iy > 0;
                bool hasDown = iy < DivY - 1;

                int xGapLeft = x0 - Gap; // 左との境界列
                int xGapRight = x1;       // 右との境界列
                int yGapUp = y1;       // 上との境界行
                int yGapDown = y0 - Gap; // 下との境界行

                // --- 右隣への提案（左側の提案として記録）---
                if (hasRight && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int yDoor = UnityEngine.Random.Range(rY0, rY1);
                    int bx = ix; // この垂直境界のインデックス
                    vBoundaryX[iy, bx] = xGapRight;
                    vDoorYLeft[iy, bx] = yDoor;
                    vHasLeft[iy, bx] = true;
                }
                // --- 左隣への提案（右側の提案として記録）---
                if (hasLeft && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int yDoor = UnityEngine.Random.Range(rY0, rY1);
                    int bx = ix - 1; // 左側境界
                    vBoundaryX[iy, bx] = xGapLeft;
                    vDoorYRight[iy, bx] = yDoor;
                    vHasRight[iy, bx] = true;
                }
                // --- 上隣への提案（下側の提案として記録）---
                if (hasUp && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int xDoor = UnityEngine.Random.Range(rX0, rX1);
                    int by = iy - 1; // 上側境界
                    hBoundaryY[by, ix] = yGapUp;
                    hDoorXLower[by, ix] = xDoor;
                    hHasLower[by, ix] = true;
                }
                // --- 下隣への提案（上側の提案として記録）---
                if (hasDown && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int xDoor = UnityEngine.Random.Range(rX0, rX1);
                    int by = iy; // 下側境界
                    hBoundaryY[by, ix] = yGapDown;
                    hDoorXUpper[by, ix] = xDoor;
                    hHasUpper[by, ix] = true;
                }
            }
        }

        // 各部屋が最低1本は他部屋と接続されるよう保証（両側同意を強制）
        for (int iy = 0; iy < DivY; iy++)
        {
            for (int ix = 0; ix < DivX; ix++)
            {
                if (!hasRoom[iy, ix]) continue;

                // すでにどこかの境界で“両側同意”が成立していればスキップ
                bool alreadyConnected =
                    (ix > 0 && vHasLeft[iy, ix - 1] && vHasRight[iy, ix - 1]) ||
                    (ix < DivX - 1 && vHasLeft[iy, ix] && vHasRight[iy, ix]) ||
                    (iy > 0 && hHasLower[iy - 1, ix] && hHasUpper[iy - 1, ix]) ||
                    (iy < DivY - 1 && hHasLower[iy, ix] && hHasUpper[iy, ix]);

                if (alreadyConnected) continue;

                // 隣に“部屋がある”方向だけ候補にする
                var candidates = new List<System.Action>();

                // 区画矩形（境界座標を出すために計算）
                int x0 = LeftPad + ix * (sectionW + Gap);
                int y0 = MapHeight - sectionH - iy * (sectionH + Gap);
                int x1 = Mathf.Min(x0 + sectionW, MapWidth);
                int y1 = Mathf.Min(y0 + sectionH, MapHeight);

                // 右（境界 index = ix、X = x1）
                if (ix < DivX - 1 && hasRoom[iy, ix + 1])
                {
                    candidates.Add(() =>
                    {
                        int bx = ix;
                        int Bx = x1; // 右境界のX
                        vBoundaryX[iy, bx] = Bx;
                        // 左側部屋の出口Y / 右側部屋の出口Y をそれぞれランダム決定
                        vDoorYLeft[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix], roomY1A[iy, ix]);
                        vDoorYRight[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix + 1], roomY1A[iy, ix + 1]);
                        vHasLeft[iy, bx] = true;
                        vHasRight[iy, bx] = true;
                    });
                }

                // 左（境界 index = ix-1、X = 左境界 = x0 - Gap）
                if (ix > 0 && hasRoom[iy, ix - 1])
                {
                    candidates.Add(() =>
                    {
                        int bx = ix - 1;
                        int Bx = x0 - Gap; // 左境界のX
                        vBoundaryX[iy, bx] = Bx;
                        vDoorYLeft[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix - 1], roomY1A[iy, ix - 1]); // 左側部屋
                        vDoorYRight[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix], roomY1A[iy, ix]);     // 右側部屋（=現在）
                        vHasLeft[iy, bx] = true;
                        vHasRight[iy, bx] = true;
                    });
                }

                // 上（境界 index = iy-1、Y = y1）
                if (iy > 0 && hasRoom[iy - 1, ix])
                {
                    candidates.Add(() =>
                    {
                        int by = iy - 1;
                        int By = y1; // 上境界のY
                        hBoundaryY[by, ix] = By;
                        hDoorXUpper[by, ix] = UnityEngine.Random.Range(roomX0A[iy - 1, ix], roomX1A[iy - 1, ix]); // 上側部屋
                        hDoorXLower[by, ix] = UnityEngine.Random.Range(roomX0A[iy, ix], roomX1A[iy, ix]);     // 下側部屋（=現在）
                        hHasUpper[by, ix] = true;
                        hHasLower[by, ix] = true;
                    });
                }

                // 下（境界 index = iy、Y = y0 - Gap）
                if (iy < DivY - 1 && hasRoom[iy + 1, ix])
                {
                    candidates.Add(() =>
                    {
                        int by = iy;
                        int By = y0 - Gap; // 下境界のY
                        hBoundaryY[by, ix] = By;
                        hDoorXLower[by, ix] = UnityEngine.Random.Range(roomX0A[iy + 1, ix], roomX1A[iy + 1, ix]); // 下側部屋
                        hDoorXUpper[by, ix] = UnityEngine.Random.Range(roomX0A[iy, ix], roomX1A[iy, ix]);     // 上側部屋（=現在）
                        hHasLower[by, ix] = true;
                        hHasUpper[by, ix] = true;
                    });
                }

                // 隣接部屋が一つも無ければ何もしない
                if (candidates.Count == 0) continue;

                // 候補からランダムに1本だけ強制的に“両側同意”にする
                candidates[UnityEngine.Random.Range(0, candidates.Count)]();
            }
        }

        // 両側が提案した境界だけ、実際に描画する（スポーク＋境界線）
        // 垂直境界：X固定
        for (int iy = 0; iy < DivY; iy++)
        {
            for (int bx = 0; bx < DivX - 1; bx++)
            {
                if (!(vHasLeft[iy, bx] && vHasRight[iy, bx])) continue; // 片側だけならスキップ

                int B = vBoundaryX[iy, bx];
                int yL = vDoorYLeft[iy, bx];
                int yR = vDoorYRight[iy, bx];

                // 左側/右側の部屋矩形
                int rX1L = roomX1A[iy, bx];       // 左部屋の右壁（内側終端）
                int rX0R = roomX0A[iy, bx + 1];   // 右部屋の左壁（内側始端）

                // スポーク（境界“手前”まで）
                for (int x = rX1L; x < B; x++) map[yL, x] = 2;      // 左→境界手前
                for (int x = rX0R - 1; x > B; x--) map[yR, x] = 2;  // 右→境界手前

                // 境界上を結ぶ（Y方向）
                int y0 = Mathf.Min(yL, yR), y1 = Mathf.Max(yL, yR);
                for (int y = y0; y <= y1; y++) map[y, B] = 2;
            }
        }

        // 水平境界：Y固定
        for (int by = 0; by < DivY - 1; by++)
        {
            for (int ix = 0; ix < DivX; ix++)
            {
                if (!(hHasLower[by, ix] && hHasUpper[by, ix])) continue; // 片側だけならスキップ

                int B = hBoundaryY[by, ix];
                int xD = hDoorXLower[by, ix]; // 下側（現在 iy）の出口
                int xU = hDoorXUpper[by, ix]; // 上側（iy-1）の出口

                // 下側/上側の部屋矩形
                int rY1Lower = roomY1A[by + 1, ix]; // 下側部屋の上壁（内側終端）
                int rY0Upper = roomY0A[by, ix];     // 上側部屋の下壁（内側始端）

                // スポーク（境界“手前”まで）
                for (int y = rY1Lower; y < B; y++) map[y, xD] = 2;   // 下→境界手前
                for (int y = rY0Upper - 1; y > B; y--) map[y, xU] = 2; // 上→境界手前

                // 境界上を結ぶ（X方向）
                int x0 = Mathf.Min(xD, xU), x1 = Mathf.Max(xD, xU);
                for (int x = x0; x <= x1; x++) map[B, x] = 2;
            }
        }
    }



}
