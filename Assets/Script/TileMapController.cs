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

    // ��敪���Ƌ��E���i1�`3�̒l�Őݒ�j
    [Header("��搔�̐ݒ�")]
    [Tooltip("�������̋�搔�̍ŏ��ƍő�i1�`3�͈̔́j")]
    [SerializeField] Vector2Int divXRange = new Vector2Int(1, 3);
    [Tooltip("�c�����̋�搔�̍ŏ��ƍő�i1�`3�͈̔́j")]
    [SerializeField] Vector2Int divYRange = new Vector2Int(1, 3);

    // �e�����̍ŏ��T�C�Y(W,H)
    [Tooltip("�����̍ŏ��T�C�Y�i��, �����j")]
    [SerializeField] Vector2Int roomMinSize = new Vector2Int(3, 3);

    // ���݂̕������i���񃉃��_������j
    int DivX, DivY;

    // ���Ԃ̋��E���Z����
    const int Gap = 1;

    // �O��
    const int LeftPad = 1;  // ��
    const int RightPad = 0; // �E

    // ���̃T�C�Y
    int sectionW;
    int sectionH;

    // �e���̈ʒu�����߂�p�����[�^
    int x0;
    int y0;
    int x1;
    int y1;

    // �����̃T�C�Y�����߂�p�����[�^
    int roomX0;
    int roomY0;
    int roomX1;
    int roomY1;

    // �񎟌��z��i�����`�z��j���Q�Ƃ���ϐ�
    int[,] map;

    // ===== �����_���ݒ� =====
    [Header("�����_���ݒ�")]
    [Tooltip("�e���ɕ��������m���i1�ŕK���쐬�j")]
    [SerializeField, Range(0f, 1f)]
    float roomSpawnRate = 1.0f;

    [Tooltip("�ʘH�����m���i1�ŕK���쐬�j�A�����̕��������ӂ����ꍇ�̂ݐڑ�")]
    [SerializeField, Range(0f, 1f)]
    float corridorSpawnRate = 1.0f;

    // ===== �ǂ̕����ɂ��ʘH���ł��Ȃ������ꍇ�̐ݒ� =====
    [Tooltip("���̕������ǂ����ƍŒ�1�{�͌q����悤�ɕۏ؂���ݒ�")]
    [SerializeField]
    bool ensureAtLeastOneExitPerRoom = true;

    // ===== �ۑ����� =====
    // �e���Ɋm�肵���g�����̋�`�h��ۑ��i�ʘH�����ł��Q�Ɓj
    bool[,] hasRoom;      // �������������
    int[,] roomX0A, roomX1A, roomY0A, roomY1A;  // [iy, ix] ���Ƃ̕�����`

    void Start()
    {
        // ����S�ʃN���A
        InitMap();

        // 30�~50 �̑S�Z���� 1 ������
        FillAll(1);

        // �S�ʔ��ɂ���
        SetTile(map, tilemap);
    }

    // �{�^������Ă�
    // 3�~3 ���� ((�ő�/3) - 1) �̃T�C�Y�Ŕ�(1)�ɂ���
    public void OnClickDivide()
    {
        // ����̕����������߂�
        RandomizeDivisions();

        // ����S�ʃN���A
        InitMap();

        // ��悲�ƂɁg�����h�֕������i���j��~��
        //PaintRooms();
        PaintRoomsRandom();

        // �o�������A���E�܂� 2�i���j ���ŕ~��
        PaintCorridors();

        // �^�C���̔z�u�����s����
        SetTile(map, tilemap);
    }

    // �^�C�������Z�b�g����֐�
    void InitMap()
    {
        // ����30 �~ ��50 �� �񎟌��z����쐬
        // ���g�͑S��0�ł���
        map = new int[MapHeight, MapWidth];

        // �V�[����� Tilemap ��������̃^�C����S����
        tilemap.ClearAllTiles();

        // �����_���p�̏�Ԕz���������
        hasRoom = new bool[DivY, DivX];
        roomX0A = new int[DivY, DivX];
        roomX1A = new int[DivY, DivX];
        roomY0A = new int[DivY, DivX];
        roomY1A = new int[DivY, DivX];
    }

    // 2�����z�� map �̑S�Z�������� v �̒l�Ŗ��߂�֐�
    void FillAll(int v)
    {
        // map.GetLength(d)�F�z��� d �����ڂ̗v�f����Ԃ����\�b�h�iSystem.Array�j
        int h = map.GetLength(0), w = map.GetLength(1);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                map[y, x] = v; // �e�z���v�̒l������
    }

    // ���S���}�b�v�T�C�Y��Ƃ��邽�߂̊֐�
    Vector3Int ConvertPosition(int x, int y)
    {
        int cx = MapWidth / 2;
        int cy = MapHeight / 2;
        return new Vector3Int(x - cx, y - cy, 0);
    }

    // �e�z�u�̒��g�̐��l���m�F���A�z�u����^�C���̐F�����߂�֐�
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

    // ���T�C�Y���v�Z����֐�
    void SectionSize()
    {
        sectionW = (MapWidth - (DivX - 1) * Gap - LeftPad - RightPad) / DivX; // ���͌�����
        sectionH = (MapHeight / DivY) - 1;
    }

    // ���̐ݒ������֐�
    void RoomPosition(int iy, int ix) 
    {

        // �e���̃X�^�[�g�ʒu�����߂�
        // �e���̍����n�_�iGap�����ē��Ԋu�ɔz�u�j
        // ���̋��̍���X�F�O���̕��{���E1�Z���Ԃ�E�ւ��炷
        x0 = LeftPad + ix * (sectionW + Gap);
        y0 = MapHeight - sectionH - iy * (sectionH + Gap);

        // �}�b�v�O�ɏo�Ȃ��悤�ɃN�����v���h��
        // �N�����v�F�l������͈͂���͂ݏo���Ȃ��悤�Ɂg���ݍ��ށ^�؂�l�߂邱��
        x1 = Mathf.Min(x0 + sectionW, MapWidth);
        y1 = Mathf.Min(y0 + sectionH, MapHeight);

    }

    // �����������߂�
    void RandomizeDivisions()
    {
        // Range(int,int) �͏�����r���I�Ȃ̂� +1 ����
        DivX = UnityEngine.Random.Range(divXRange.x, divXRange.y + 1);
        DivY = UnityEngine.Random.Range(divYRange.x, divYRange.y + 1);

        // �O�̂��߃N�����v�iInspector�ŕs���l���������ꍇ�ɔ����āj
        DivX = Mathf.Clamp(DivX, 1, 3);
        DivY = Mathf.Clamp(DivY, 1, 3);
    }

    // ��悲�Ƃɕ����T�C�Y�ƈʒu�������_���Ɍ��߂Ĕ��œh��
    // ���߂���`�� room* �z��ɕۑ����āA��i�̒ʘH�����Ŏg��
    void PaintRoomsRandom()
    {
        // ���T�C�Y���v�Z
        SectionSize();

        // �c�����̋��C���f�b�N�X��0..2�ŉ�
        for (int iy = 0; iy < DivY; iy++)
        {
            // �������̋��C���f�b�N�X��0..2�ŉ�
            for (int ix = 0; ix < DivX; ix++)
            {
                // �e���̃X�^�[�g�ʒu�����߂�
                RoomPosition(iy, ix);

                // ���T�C�Y
                int availW = x1 - x0;
                int availH = y1 - y0;

                // �Œ�ł����E�㉺��1�Z���̒ʘH + �ŏ������T�C�Y�𖞂����邩
                // �����̍ŏ��T�C�Y�� ���E�㉺�̒ʘH1�Z�����i���v2�Z���j �𑫂����傫�����u���Ȃ��Ȃ�A���̋��ɂ͕��������Ȃ��̂ŃX�L�b�v
                if (availW < roomMinSize.x + 2 || availH < roomMinSize.y + 2)
                {
                    hasRoom[iy, ix] = false;
                    continue;
                }

                // ���̋��ɕ�������邩�i�m���j
                if (UnityEngine.Random.value > roomSpawnRate)
                {
                    hasRoom[iy, ix] = false;
                    continue;
                }

                // �����_���ȕ����T�C�Y
                // int �� Range �͏���r���Ȃ̂� (max+1) �ɒ���
                int roomW = UnityEngine.Random.Range(roomMinSize.x, availW - 1); // �` availW-2 �܂�
                int roomH = UnityEngine.Random.Range(roomMinSize.y, availH - 1); // �` availH-2 �܂�

                // �����_���ȕ����ʒu�i�ʘH=1�Z�������E�㉺�ɕK���m�ہj
                int freeW = availW - roomW; // �ʘH���̍��v�i���E�j
                int freeH = availH - roomH; // �ʘH���̍��v�i�㉺�j

                int leftMargin = UnityEngine.Random.Range(1, freeW); // �E��1�ȏ�m�ۂ����
                int bottomMargin = UnityEngine.Random.Range(1, freeH); // ���1�ȏ�m�ۂ����

                int rightMargin = freeW - leftMargin;
                int topMargin = freeH - bottomMargin;

                roomX0 = x0 + leftMargin;
                roomX1 = x1 - rightMargin;
                roomY0 = y0 + bottomMargin;
                roomY1 = y1 - topMargin;

                // �ۑ�
                hasRoom[iy, ix] = true;
                roomX0A[iy, ix] = roomX0; roomX1A[iy, ix] = roomX1;
                roomY0A[iy, ix] = roomY0; roomY1A[iy, ix] = roomY1;

                // ���œh��
                for (int y = roomY0; y < roomY1; y++)
                    for (int x = roomX0; x < roomX1; x++)
                        map[y, x] = 1;
            }
        }
    }

    // �ʘH�����i���^�C���j�A���������ӂ������E�̂ݐڑ�
    void PaintCorridors()
    {
        // �܂��͊e���E�Ɂu�Б��̏o����āv���L�^���邾���i�`�悵�Ȃ��j
        // �������E�i���E�̋��ԁj�F�s=iy, ��=���Eindex(0..DivX-2)
        int[,] vBoundaryX = new int[DivY, DivX - 1];
        int[,] vDoorYLeft = new int[DivY, DivX - 1];   // �������̏o��Y
        int[,] vDoorYRight = new int[DivY, DivX - 1];   // �E�����̏o��Y
        bool[,] vHasLeft = new bool[DivY, DivX - 1];
        bool[,] vHasRight = new bool[DivY, DivX - 1];

        // �������E�i�㉺�̋��ԁj�F�s=���Eindex(0..DivY-2), ��=ix
        int[,] hBoundaryY = new int[DivY - 1, DivX];
        int[,] hDoorXLower = new int[DivY - 1, DivX];   // �������i���݂� iy�j�����ւ̏o��X
        int[,] hDoorXUpper = new int[DivY - 1, DivX];   // �㑤���iiy-1�j���牺�ւ̏o��X
        bool[,] hHasLower = new bool[DivY - 1, DivX];
        bool[,] hHasUpper = new bool[DivY - 1, DivX];

        // ���T�C�Y���v�Z
        SectionSize();

        for (int iy = 0; iy < DivY; iy++)
        {
            for (int ix = 0; ix < DivX; ix++)
            {
                if (!hasRoom[iy, ix]) continue;

                // �e���̃X�^�[�g�ʒu�����߂�
                RoomPosition(iy, ix);

                // ���̕����̋�`
                int rX0 = roomX0A[iy, ix], rX1 = roomX1A[iy, ix];
                int rY0 = roomY0A[iy, ix], rY1 = roomY1A[iy, ix];

                bool hasLeft = ix > 0;
                bool hasRight = ix < DivX - 1;
                bool hasUp = iy > 0;
                bool hasDown = iy < DivY - 1;

                int xGapLeft = x0 - Gap; // ���Ƃ̋��E��
                int xGapRight = x1;       // �E�Ƃ̋��E��
                int yGapUp = y1;       // ��Ƃ̋��E�s
                int yGapDown = y0 - Gap; // ���Ƃ̋��E�s

                // --- �E�ׂւ̒�āi�����̒�ĂƂ��ċL�^�j---
                if (hasRight && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int yDoor = UnityEngine.Random.Range(rY0, rY1);
                    int bx = ix; // ���̐������E�̃C���f�b�N�X
                    vBoundaryX[iy, bx] = xGapRight;
                    vDoorYLeft[iy, bx] = yDoor;
                    vHasLeft[iy, bx] = true;
                }
                // --- ���ׂւ̒�āi�E���̒�ĂƂ��ċL�^�j---
                if (hasLeft && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int yDoor = UnityEngine.Random.Range(rY0, rY1);
                    int bx = ix - 1; // �������E
                    vBoundaryX[iy, bx] = xGapLeft;
                    vDoorYRight[iy, bx] = yDoor;
                    vHasRight[iy, bx] = true;
                }
                // --- ��ׂւ̒�āi�����̒�ĂƂ��ċL�^�j---
                if (hasUp && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int xDoor = UnityEngine.Random.Range(rX0, rX1);
                    int by = iy - 1; // �㑤���E
                    hBoundaryY[by, ix] = yGapUp;
                    hDoorXLower[by, ix] = xDoor;
                    hHasLower[by, ix] = true;
                }
                // --- ���ׂւ̒�āi�㑤�̒�ĂƂ��ċL�^�j---
                if (hasDown && UnityEngine.Random.value <= corridorSpawnRate)
                {
                    int xDoor = UnityEngine.Random.Range(rX0, rX1);
                    int by = iy; // �������E
                    hBoundaryY[by, ix] = yGapDown;
                    hDoorXUpper[by, ix] = xDoor;
                    hHasUpper[by, ix] = true;
                }
            }
        }

        // �e�������Œ�1�{�͑������Ɛڑ������悤�ۏ؁i�������ӂ������j
        for (int iy = 0; iy < DivY; iy++)
        {
            for (int ix = 0; ix < DivX; ix++)
            {
                if (!hasRoom[iy, ix]) continue;

                // ���łɂǂ����̋��E�Łg�������Ӂh���������Ă���΃X�L�b�v
                bool alreadyConnected =
                    (ix > 0 && vHasLeft[iy, ix - 1] && vHasRight[iy, ix - 1]) ||
                    (ix < DivX - 1 && vHasLeft[iy, ix] && vHasRight[iy, ix]) ||
                    (iy > 0 && hHasLower[iy - 1, ix] && hHasUpper[iy - 1, ix]) ||
                    (iy < DivY - 1 && hHasLower[iy, ix] && hHasUpper[iy, ix]);

                if (alreadyConnected) continue;

                // �ׂɁg����������h�����������ɂ���
                var candidates = new List<System.Action>();

                // ����`�i���E���W���o�����߂Ɍv�Z�j
                int x0 = LeftPad + ix * (sectionW + Gap);
                int y0 = MapHeight - sectionH - iy * (sectionH + Gap);
                int x1 = Mathf.Min(x0 + sectionW, MapWidth);
                int y1 = Mathf.Min(y0 + sectionH, MapHeight);

                // �E�i���E index = ix�AX = x1�j
                if (ix < DivX - 1 && hasRoom[iy, ix + 1])
                {
                    candidates.Add(() =>
                    {
                        int bx = ix;
                        int Bx = x1; // �E���E��X
                        vBoundaryX[iy, bx] = Bx;
                        // ���������̏o��Y / �E�������̏o��Y �����ꂼ�ꃉ���_������
                        vDoorYLeft[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix], roomY1A[iy, ix]);
                        vDoorYRight[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix + 1], roomY1A[iy, ix + 1]);
                        vHasLeft[iy, bx] = true;
                        vHasRight[iy, bx] = true;
                    });
                }

                // ���i���E index = ix-1�AX = �����E = x0 - Gap�j
                if (ix > 0 && hasRoom[iy, ix - 1])
                {
                    candidates.Add(() =>
                    {
                        int bx = ix - 1;
                        int Bx = x0 - Gap; // �����E��X
                        vBoundaryX[iy, bx] = Bx;
                        vDoorYLeft[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix - 1], roomY1A[iy, ix - 1]); // ��������
                        vDoorYRight[iy, bx] = UnityEngine.Random.Range(roomY0A[iy, ix], roomY1A[iy, ix]);     // �E�������i=���݁j
                        vHasLeft[iy, bx] = true;
                        vHasRight[iy, bx] = true;
                    });
                }

                // ��i���E index = iy-1�AY = y1�j
                if (iy > 0 && hasRoom[iy - 1, ix])
                {
                    candidates.Add(() =>
                    {
                        int by = iy - 1;
                        int By = y1; // �㋫�E��Y
                        hBoundaryY[by, ix] = By;
                        hDoorXUpper[by, ix] = UnityEngine.Random.Range(roomX0A[iy - 1, ix], roomX1A[iy - 1, ix]); // �㑤����
                        hDoorXLower[by, ix] = UnityEngine.Random.Range(roomX0A[iy, ix], roomX1A[iy, ix]);     // ���������i=���݁j
                        hHasUpper[by, ix] = true;
                        hHasLower[by, ix] = true;
                    });
                }

                // ���i���E index = iy�AY = y0 - Gap�j
                if (iy < DivY - 1 && hasRoom[iy + 1, ix])
                {
                    candidates.Add(() =>
                    {
                        int by = iy;
                        int By = y0 - Gap; // �����E��Y
                        hBoundaryY[by, ix] = By;
                        hDoorXLower[by, ix] = UnityEngine.Random.Range(roomX0A[iy + 1, ix], roomX1A[iy + 1, ix]); // ��������
                        hDoorXUpper[by, ix] = UnityEngine.Random.Range(roomX0A[iy, ix], roomX1A[iy, ix]);     // �㑤�����i=���݁j
                        hHasLower[by, ix] = true;
                        hHasUpper[by, ix] = true;
                    });
                }

                // �אڕ��������������Ή������Ȃ�
                if (candidates.Count == 0) continue;

                // ��₩�烉���_����1�{���������I�Ɂg�������Ӂh�ɂ���
                candidates[UnityEngine.Random.Range(0, candidates.Count)]();
            }
        }

        // ��������Ă������E�����A���ۂɕ`�悷��i�X�|�[�N�{���E���j
        // �������E�FX�Œ�
        for (int iy = 0; iy < DivY; iy++)
        {
            for (int bx = 0; bx < DivX - 1; bx++)
            {
                if (!(vHasLeft[iy, bx] && vHasRight[iy, bx])) continue; // �Б������Ȃ�X�L�b�v

                int B = vBoundaryX[iy, bx];
                int yL = vDoorYLeft[iy, bx];
                int yR = vDoorYRight[iy, bx];

                // ����/�E���̕�����`
                int rX1L = roomX1A[iy, bx];       // �������̉E�ǁi�����I�[�j
                int rX0R = roomX0A[iy, bx + 1];   // �E�����̍��ǁi�����n�[�j

                // �X�|�[�N�i���E�g��O�h�܂Łj
                for (int x = rX1L; x < B; x++) map[yL, x] = 2;      // �������E��O
                for (int x = rX0R - 1; x > B; x--) map[yR, x] = 2;  // �E�����E��O

                // ���E������ԁiY�����j
                int y0 = Mathf.Min(yL, yR), y1 = Mathf.Max(yL, yR);
                for (int y = y0; y <= y1; y++) map[y, B] = 2;
            }
        }

        // �������E�FY�Œ�
        for (int by = 0; by < DivY - 1; by++)
        {
            for (int ix = 0; ix < DivX; ix++)
            {
                if (!(hHasLower[by, ix] && hHasUpper[by, ix])) continue; // �Б������Ȃ�X�L�b�v

                int B = hBoundaryY[by, ix];
                int xD = hDoorXLower[by, ix]; // �����i���� iy�j�̏o��
                int xU = hDoorXUpper[by, ix]; // �㑤�iiy-1�j�̏o��

                // ����/�㑤�̕�����`
                int rY1Lower = roomY1A[by + 1, ix]; // ���������̏�ǁi�����I�[�j
                int rY0Upper = roomY0A[by, ix];     // �㑤�����̉��ǁi�����n�[�j

                // �X�|�[�N�i���E�g��O�h�܂Łj
                for (int y = rY1Lower; y < B; y++) map[y, xD] = 2;   // �������E��O
                for (int y = rY0Upper - 1; y > B; y--) map[y, xU] = 2; // �と���E��O

                // ���E������ԁiX�����j
                int x0 = Mathf.Min(xD, xU), x1 = Mathf.Max(xD, xU);
                for (int x = x0; x <= x1; x++) map[B, x] = 2;
            }
        }
    }



}
