using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public List<JigsawPiece> pieces = new List<JigsawPiece>();
    public RectTransform puzzleContainer;
    public float snapThreshold = 35f;

    [Header("Board & Scatter Area")]
    [Tooltip("퍼즐 틀(보드)의 실제 크기")]
    public Vector2 puzzleBoardSize = new Vector2(1000f, 650f);

    [Tooltip("조각들이 흩뿌려질 전체 영역 크기 (보드보다 크게 설정)")]
    public Vector2 scatterAreaSize = new Vector2(1600f, 900f);

    [Tooltip("보드와의 최소 이격 거리 (패딩)")]
    public float boardPadding = 50f;

    // 현재 정답 위치/그룹에 잠긴(Lock) 조각 개수
    private int lockedPieceCount = 0;

    /// <summary>
    /// 기존 생성된 퍼즐 조각들을 완전히 삭제하고 초기화
    /// </summary>
    public void ClearExistingPieces()
    {
        foreach (var piece in pieces)
        {
            if (piece != null)
            {
                // Destroy 대신 DestroyImmediate를 사용해 동일 프레임에서 즉시 삭제
                DestroyImmediate(piece.gameObject);
            }
        }
        pieces.Clear();
        lockedPieceCount = 0;
    }

    public void SetupPuzzle()
    {
        if (pieces == null || pieces.Count == 0) return;

        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            if (piece.container == null && puzzleContainer != null)
            {
                piece.container = puzzleContainer;
            }
            piece.snapThreshold = snapThreshold;

            // 💡 퍼즐 틀(보드) 외각 영역에만 무작위 배치
            Vector2 randomScatterPos = GetRandomOutsidePosition();

            RectTransform rt = piece.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = randomScatterPos;
            }
        }
    }

    /// <summary>
    /// 조각이 Lock 될 때마다 JigsawPiece에서 호출해주는 함수
    /// </summary>
    public void RegisterLockedPiece()
    {
        lockedPieceCount++;
        Debug.Log($"[PuzzleManager] Lock 카운트: {lockedPieceCount} / {pieces.Count}");
    }

    // 퍼즐 틀 외각의 4개 스폰 영역(좌, 우, 상, 하) 중 하나를 선택하여 좌표 생성
    private Vector2 GetRandomOutsidePosition()
    {
        float boardHalfW = (puzzleBoardSize.x / 2f) + boardPadding;
        float boardHalfH = (puzzleBoardSize.y / 2f) + boardPadding;

        float outerHalfW = scatterAreaSize.x / 2f;
        float outerHalfH = scatterAreaSize.y / 2f;

        // 0: Left, 1: Right, 2: Top, 3: Bottom
        int zone = Random.Range(0, 4);

        float posX = 0f;
        float posY = 0f;

        switch (zone)
        {
            case 0: // Left Zone
                posX = Random.Range(-outerHalfW, -boardHalfW);
                posY = Random.Range(-outerHalfH, outerHalfH);
                break;
            case 1: // Right Zone
                posX = Random.Range(boardHalfW, outerHalfW);
                posY = Random.Range(-outerHalfH, outerHalfH);
                break;
            case 2: // Top Zone
                posX = Random.Range(-outerHalfW, outerHalfW);
                posY = Random.Range(boardHalfH, outerHalfH);
                break;
            case 3: // Bottom Zone
                posX = Random.Range(-outerHalfW, outerHalfW);
                posY = Random.Range(-outerHalfH, -boardHalfH);
                break;
        }

        return new Vector2(posX, posY);
    }

    /// <summary>
    /// 카운트와 전체 조각 수가 일치하는지만 단순 비교
    /// </summary>
    public bool CheckIsComplete()
    {
        if (pieces == null || pieces.Count == 0) return false;

        // 전체 조각 수와 잠긴 조각 수가 같으면 클리어
        return lockedPieceCount == pieces.Count;
    }
}