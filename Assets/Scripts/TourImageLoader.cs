using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// TourAPI detailImage2 엔드포인트로 이미지를 가져온다.
/// BuildingPopupUI.FetchTourInfo와 동일한 API 키·요청 방식을 사용한다.
/// 한 번 받은 이미지는 캐시에 보관해 중복 요청을 막는다.
/// </summary>
public class TourImageLoader : MonoBehaviour
{
    private static TourImageLoader instance;
    public static TourImageLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TourImageLoader>();
                if (instance == null)
                {
                    var go = new GameObject("TourImageLoader (auto)");
                    instance = go.AddComponent<TourImageLoader>();
                }
            }
            return instance;
        }
    }

    [Header("TourAPI 설정")]
    [Tooltip("BuildingPopupUI 와 동일한 data.go.kr 디코딩 키를 입력한다.")]
    [SerializeField] private string tourApiKey = "616315cd61c155564e9088acbc319ff980ccc75a67ed38601b3876602d23ee9d";

    private readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// contentId 에 해당하는 대표 이미지를 비동기로 가져온다.
    /// 캐시에 있으면 즉시 콜백 호출. 실패 시 null 전달.
    /// </summary>
    public void LoadImage(string contentId, Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(contentId)) { onComplete?.Invoke(null); return; }

        if (cache.TryGetValue(contentId, out Sprite cached))
        {
            onComplete?.Invoke(cached);
            return;
        }

        StartCoroutine(FetchRoutine(contentId, onComplete));
    }

    private IEnumerator FetchRoutine(string contentId, Action<Sprite> onComplete)
    {
        // 1단계: PhotoGalleryService1 — 이미지 URL 획득
        string metaUrl = $"https://apis.data.go.kr/B551011/PhotoGalleryService1/galleryList1" +
                         $"?serviceKey={tourApiKey}" +
                         $"&contentId={contentId}" +
                         $"&MobileOS=ETC" +
                         $"&MobileApp=GyeongbukTycoon" +
                         $"&_type=json" +
                         $"&numOfRows=1" +
                         $"&pageNo=1";

        using UnityWebRequest metaReq = UnityWebRequest.Get(metaUrl);
        yield return metaReq.SendWebRequest();

        if (metaReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[TourImageLoader] URL 요청 실패 (contentId={contentId}): {metaReq.error}");
            onComplete?.Invoke(null);
            yield break;
        }

        string imageUrl = ParseImageUrl(metaReq.downloadHandler.text);
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogWarning($"[TourImageLoader] contentId={contentId} 에 해당하는 이미지가 없습니다.");
            onComplete?.Invoke(null);
            yield break;
        }

        // 2단계: URL → Texture2D 다운로드
        using UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return imgReq.SendWebRequest();

        if (imgReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[TourImageLoader] 이미지 다운로드 실패 ({imageUrl}): {imgReq.error}");
            onComplete?.Invoke(null);
            yield break;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(imgReq);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

        cache[contentId] = sprite;
        Debug.Log($"[TourImageLoader] 이미지 로드 완료 (contentId={contentId})");
        onComplete?.Invoke(sprite);
    }

    // PhotoGalleryService1 응답에서 galWebImageUrl 추출
    private string ParseImageUrl(string json)
    {
        const string key = "\"galWebImageUrl\":\"";
        int start = json.IndexOf(key);
        if (start < 0) return null;
        start += key.Length;
        int end = json.IndexOf("\"", start);
        if (end < 0) return null;
        return json.Substring(start, end - start);
    }
}
