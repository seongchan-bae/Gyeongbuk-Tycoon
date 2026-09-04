using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RuntimeJigsawGenerator : MonoBehaviour
{
    [Header("Dependencies")]
    public PuzzleManager puzzleManager;
    public RectTransform puzzleContainer; // 퍼즐 조각들이 생성될 Parent UI

    [Header("Puzzle Source")]
    public Sprite mainSprite; // 퍼즐로 분할할 원본 이미지

    [Header("Grid Settings")]
    public int rows = 3;
    public int cols = 3;

    [Header("Visual Settings")]
    [Range(0.1f, 0.4f)]
    public float tabRatio = 0.2f; // 돌기(Tab) 크기 비율

    [ContextMenu("Generate Puzzle")]
    public void GeneratePuzzle()
    {
        if (mainSprite == null || puzzleContainer == null)
        {
            Debug.LogError("[RuntimeJigsawGenerator] MainSprite 또는 PuzzleContainer가 설정되지 않았습니다.");
            return;
        }

        // 1. 기존 생성된 퍼즐 조각 제거
        if (puzzleManager != null)
        {
            puzzleManager.ClearExistingPieces();
        }

        // 2. 탭 모양 패턴 생성 (상, 우, 하, 좌)
        int[,,] tabs = GenerateTabPattern(rows, cols);

        // 3. 텍스처 데이터 준비
        Texture2D srcTex = mainSprite.texture;
        
        // 퍼즐 보드 UI 크기 기준 (puzzleContainer 크기 반영)
        float containerW = puzzleContainer.rect.width > 0 ? puzzleContainer.rect.width : 1000f;
        float containerH = puzzleContainer.rect.height > 0 ? puzzleContainer.rect.height : 650f;

        float UIpieceW = containerW / cols;
        float UIpieceH = containerH / rows;

        // 원본 이미지 텍스처 픽셀 크기 기준
        float sourceW = mainSprite.rect.width;
        float sourceH = mainSprite.rect.height;
        float pieceTexW = sourceW / cols;
        float pieceTexH = sourceH / rows;

        float startX = -containerW / 2f + UIpieceW / 2f;
        float startY = containerH / 2f - UIpieceH / 2f;

        List<JigsawPiece> createdPieces = new List<JigsawPiece>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int top = tabs[r, c, 0];
                int right = tabs[r, c, 1];
                int bottom = tabs[r, c, 2];
                int left = tabs[r, c, 3];

                // 4. 돌기가 포함된 퍼즐 조각 Sprite 생성
                Sprite pieceSprite = CreatePieceSprite(srcTex, mainSprite.rect, r, c, top, right, bottom, left, pieceTexW, pieceTexH);

                // 5. UI GameObject 생성
                GameObject pieceGO = new GameObject($"Piece_{r}_{c}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(JigsawPiece));
                pieceGO.transform.SetParent(puzzleContainer, false);

                Image img = pieceGO.GetComponent<Image>();
                img.sprite = pieceSprite;
                img.type = Image.Type.Simple;

                // 6. 돌기 비율을 포함한 최종 UI 조각 크기 설정
                float paddedUIW = UIpieceW * (1f + 2f * tabRatio);
                float paddedUIH = UIpieceH * (1f + 2f * tabRatio);

                RectTransform rt = pieceGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(paddedUIW, paddedUIH);

                // 정답 기준 좌표 (Local Position)
                Vector2 targetLocalPos = new Vector2(startX + c * UIpieceW, startY - r * UIpieceH);

                // 7. JigsawPiece 스크립트 데이터 초기화
                JigsawPiece pieceScript = pieceGO.GetComponent<JigsawPiece>();
                if (pieceScript != null)
                {
                    pieceScript.Init(puzzleContainer, r, c, targetLocalPos, 35f);
                }

                createdPieces.Add(pieceScript);
            }
        }

        // 8. PuzzleManager 등록 및 무작위 배치 실행
        if (puzzleManager != null)
        {
            puzzleManager.pieces = createdPieces;
            puzzleManager.SetupPuzzle();
        }
    }

    public void GeneratePuzzleWithImage(Sprite newSprite)
    {
        if (newSprite != null)
        {
            mainSprite = newSprite;
        }
        GeneratePuzzle();
    }

    private int[,,] GenerateTabPattern(int rCount, int cCount)
    {
        int[,,] tabs = new int[rCount, cCount, 4];

        for (int r = 0; r < rCount; r++)
        {
            for (int c = 0; c < cCount; c++)
            {
                if (r == 0) tabs[r, c, 0] = 0;
                else tabs[r, c, 0] = -tabs[r - 1, c, 2];

                if (c == 0) tabs[r, c, 3] = 0;
                else tabs[r, c, 3] = -tabs[r, c - 1, 1];

                if (c == cCount - 1) tabs[r, c, 1] = 0;
                else tabs[r, c, 1] = Random.value > 0.5f ? 1 : -1;

                if (r == rCount - 1) tabs[r, c, 2] = 0;
                else tabs[r, c, 2] = Random.value > 0.5f ? 1 : -1;
            }
        }
        return tabs;
    }

    // 원본 텍스처 픽셀을 조각마다 다시 읽지 않도록 한 번만 받아 캐시한다.
    private Texture2D cachedSrcTex;
    private Color32[] cachedSrcPixels;
    private int cachedSrcW;
    private int cachedSrcH;

    private void EnsureSourcePixels(Texture2D src)
    {
        if (cachedSrcTex == src && cachedSrcPixels != null) return;

        cachedSrcTex = src;
        cachedSrcW = src.width;
        cachedSrcH = src.height;
        cachedSrcPixels = src.GetPixels32();
    }

    private Sprite CreatePieceSprite(Texture2D src, Rect spriteRect, int r, int c, int top, int right, int bottom, int left, float pW, float pH)
    {
        // Texture2D.GetPixel / SetPixel 은 픽셀 하나마다 네이티브 호출이 일어나 매우 느리다.
        // 조각 하나가 수십만 픽셀이고 조각이 9개라 수백만 번 호출되면서 몇 초씩 멈췄다.
        // 원본은 GetPixels32()로 한 번만 읽고, 조각은 배열에 채운 뒤 SetPixels32()로 한 번에 쓴다.
        EnsureSourcePixels(src);

        int baseW = Mathf.RoundToInt(pW);
        int baseH = Mathf.RoundToInt(pH);
        int padW = Mathf.RoundToInt(pW * tabRatio);
        int padH = Mathf.RoundToInt(pH * tabRatio);

        int texW = baseW + padW * 2;
        int texH = baseH + padH * 2;

        Color32[] pieceColors = new Color32[texW * texH]; // 기본값이 (0,0,0,0) = 투명

        int startX = Mathf.RoundToInt(spriteRect.x + c * pW - padW);
        int startY = Mathf.RoundToInt(spriteRect.y + (rows - 1 - r) * pH - padH);

        float centerX = texW / 2f;
        float centerY = texH / 2f;
        float tabRadius = padW * 0.8f;
        float tabRadiusSqr = tabRadius * tabRadius;

        // 돌기/홈 원의 중심 (상, 하, 우, 좌)
        float topCx = centerX, topCy = padH + baseH;
        float botCx = centerX, botCy = padH;
        float rightCx = padW + baseW, rightCy = centerY;
        float leftCx = padW, leftCy = centerY;

        for (int y = 0; y < texH; y++)
        {
            int rowOffset = y * texW;
            int srcY = startY + y;
            bool srcRowValid = (srcY >= 0 && srcY < cachedSrcH);
            int srcRowOffset = srcY * cachedSrcW;

            bool inBaseRow = (y >= padH && y < padH + baseH);

            for (int x = 0; x < texW; x++)
            {
                bool isInsideShape = inBaseRow && (x >= padW && x < padW + baseW);

                if (top != 0 && SqrDist(x, y, topCx, topCy) <= tabRadiusSqr)
                    isInsideShape = (top == 1);
                if (bottom != 0 && SqrDist(x, y, botCx, botCy) <= tabRadiusSqr)
                    isInsideShape = (bottom == 1);
                if (right != 0 && SqrDist(x, y, rightCx, rightCy) <= tabRadiusSqr)
                    isInsideShape = (right == 1);
                if (left != 0 && SqrDist(x, y, leftCx, leftCy) <= tabRadiusSqr)
                    isInsideShape = (left == 1);

                if (!isInsideShape || !srcRowValid) continue;

                int srcX = startX + x;
                if (srcX < 0 || srcX >= cachedSrcW) continue;

                pieceColors[rowOffset + x] = cachedSrcPixels[srcRowOffset + srcX];
            }
        }

        Texture2D pieceTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        pieceTex.SetPixels32(pieceColors);
        pieceTex.Apply();

        return Sprite.Create(pieceTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f));
    }

    private static float SqrDist(int x, int y, float cx, float cy)
    {
        float dx = x - cx;
        float dy = y - cy;
        return dx * dx + dy * dy;
    }
}
