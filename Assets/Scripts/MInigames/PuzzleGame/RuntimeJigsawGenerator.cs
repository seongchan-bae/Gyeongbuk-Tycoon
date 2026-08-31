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

    private Sprite CreatePieceSprite(Texture2D src, Rect spriteRect, int r, int c, int top, int right, int bottom, int left, float pW, float pH)
    {
        int baseW = Mathf.RoundToInt(pW);
        int baseH = Mathf.RoundToInt(pH);
        int padW = Mathf.RoundToInt(pW * tabRatio);
        int padH = Mathf.RoundToInt(pH * tabRatio);

        int texW = baseW + padW * 2;
        int texH = baseH + padH * 2;

        Texture2D pieceTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        
        // 전체 알파 투명 채널로 초기화
        Color[] clearColors = new Color[texW * texH];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
        pieceTex.SetPixels(clearColors);

        int startX = Mathf.RoundToInt(spriteRect.x + c * pW - padW);
        int startY = Mathf.RoundToInt(spriteRect.y + (rows - 1 - r) * pH - padH);

        Vector2 center = new Vector2(texW / 2f, texH / 2f);
        float tabRadius = padW * 0.8f;

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                // 기본 사각형 조각 범위 내부 체크
                bool isInsideBase = (x >= padW && x < padW + baseW && y >= padH && y < padH + baseH);
                bool isInsideShape = isInsideBase;

                // 상단 돌기/홈 마스킹 연산
                if (top != 0)
                {
                    Vector2 tabCenter = new Vector2(center.x, padH + baseH);
                    bool inCircle = Vector2.Distance(new Vector2(x, y), tabCenter) <= tabRadius;
                    if (top == 1 && inCircle) isInsideShape = true;
                    else if (top == -1 && inCircle) isInsideShape = false;
                }
                // 하단 돌기/홈 마스킹 연산
                if (bottom != 0)
                {
                    Vector2 tabCenter = new Vector2(center.x, padH);
                    bool inCircle = Vector2.Distance(new Vector2(x, y), tabCenter) <= tabRadius;
                    if (bottom == 1 && inCircle) isInsideShape = true;
                    else if (bottom == -1 && inCircle) isInsideShape = false;
                }
                // 우측 돌기/홈 마스킹 연산
                if (right != 0)
                {
                    Vector2 tabCenter = new Vector2(padW + baseW, center.y);
                    bool inCircle = Vector2.Distance(new Vector2(x, y), tabCenter) <= tabRadius;
                    if (right == 1 && inCircle) isInsideShape = true;
                    else if (right == -1 && inCircle) isInsideShape = false;
                }
                // 좌측 돌기/홈 마스킹 연산
                if (left != 0)
                {
                    Vector2 tabCenter = new Vector2(padW, center.y);
                    bool inCircle = Vector2.Distance(new Vector2(x, y), tabCenter) <= tabRadius;
                    if (left == 1 && inCircle) isInsideShape = true;
                    else if (left == -1 && inCircle) isInsideShape = false;
                }

                // 퍼즐 모양 안쪽에 포함된 픽셀만 원본 이미지 픽셀로 채움
                if (isInsideShape)
                {
                    int srcX = startX + x;
                    int srcY = startY + y;
                    if (srcX >= 0 && srcX < src.width && srcY >= 0 && srcY < src.height)
                    {
                        pieceTex.SetPixel(x, y, src.GetPixel(srcX, srcY));
                    }
                }
            }
        }

        pieceTex.Apply();
        return Sprite.Create(pieceTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f));
    }
}