using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class JigsawPiece : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [HideInInspector] public int row;
    [HideInInspector] public int col;
    [HideInInspector] public Vector2 targetLocalPos; // 정답 anchoredPosition
    [HideInInspector] public RectTransform container;

    public float snapThreshold = 35f;
    public bool isLocked = false;

    public JigsawPiece topPiece;
    public JigsawPiece bottomPiece;
    public JigsawPiece leftPiece;
    public JigsawPiece rightPiece;

    public HashSet<JigsawPiece> group = new HashSet<JigsawPiece>();

    private RectTransform rectTransform;
    private Canvas canvas;
    

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (group == null) group = new HashSet<JigsawPiece>();
        group.Add(this);
    }

    public void Init(RectTransform containerTransform, int r, int c, Vector2 localPos, float threshold = 35f)
    {
        this.container = containerTransform;
        this.row = r;
        this.col = c;
        this.targetLocalPos = localPos;
        this.snapThreshold = threshold;
        this.isLocked = false;
    }

    public void Init(PuzzleManager manager, Vector2 targetPos, float threshold = 35f)
    {
        this.targetLocalPos = targetPos;
        this.snapThreshold = threshold;
        this.isLocked = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        foreach (var piece in group)
        {
            if (piece != null) piece.transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        float scale = (canvas != null) ? canvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / scale;

        foreach (var piece in group)
        {
            if (piece != null && piece.rectTransform != null)
            {
                piece.rectTransform.anchoredPosition += delta;
            }
        }
    }

    
    private void TrySnapNeighbors()
    {
        List<JigsawPiece> checkList = new List<JigsawPiece>(group);

        foreach (var p in checkList)
        {
            if (p == null) continue;
            CheckAndMerge(p, p.topPiece);
            CheckAndMerge(p, p.bottomPiece);
            CheckAndMerge(p, p.leftPiece);
            CheckAndMerge(p, p.rightPiece);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
{
    if (isLocked) return;

    // 1. 퍼즐 틀 스냅 검사
    JigsawPiece matchedPiece = null;
    foreach (var piece in group)
    {
        if (piece == null) continue;
        float dist = Vector2.Distance(piece.rectTransform.anchoredPosition, piece.targetLocalPos);
        if (dist <= snapThreshold)
        {
            matchedPiece = piece;
            break;
        }
    }

    if (matchedPiece != null)
    {
        Vector2 shift = matchedPiece.targetLocalPos - matchedPiece.rectTransform.anchoredPosition;
        PuzzleManager manager = FindFirstObjectByType<PuzzleManager>();

        foreach (var piece in group)
        {
            if (piece != null && !piece.isLocked)
            {
                piece.rectTransform.anchoredPosition += shift;
                piece.isLocked = true; // 잠금 처리
                
                // 🎯 스냅 카운트 증가
                if (manager != null) manager.RegisterLockedPiece();
            }
        }
        return;
    }

    // 2. 이웃 조각 간 스냅
    TrySnapNeighbors();
}

private void CheckAndMerge(JigsawPiece p1, JigsawPiece p2)
{
    if (p2 == null || p1.group.Contains(p2)) return;

    Vector2 expectedOffset = p2.targetLocalPos - p1.targetLocalPos;
    Vector2 currentOffset = p2.rectTransform.anchoredPosition - p1.rectTransform.anchoredPosition;

    if (Vector2.Distance(currentOffset, expectedOffset) <= snapThreshold)
    {
        Vector2 shift = expectedOffset - currentOffset;
        HashSet<JigsawPiece> p2Group = new HashSet<JigsawPiece>(p2.group);
        PuzzleManager manager = FindFirstObjectByType<PuzzleManager>();

        // 상대방 그룹이나 내 그룹 중 하나라도 이미 틀에 안착(isLocked)되어 있던 상태인지 검사
        bool shouldLockAll = p1.isLocked || p2.isLocked;

        foreach (var piece in p2Group)
        {
            if (piece != null)
            {
                piece.rectTransform.anchoredPosition += shift;
                p1.group.Add(piece);
                piece.group = p1.group;
            }
        }

        // 틀에 붙은 조각에 다른 조각을 가져다 붙인 경우, 안 잠겨 있던 조각들을 Lock 및 카운트 올려줌
        if (shouldLockAll)
        {
            foreach (var piece in p1.group)
            {
                if (piece != null && !piece.isLocked)
                {
                    piece.isLocked = true;
                    if (manager != null) manager.RegisterLockedPiece();
                }
            }
        }
    }
}
}