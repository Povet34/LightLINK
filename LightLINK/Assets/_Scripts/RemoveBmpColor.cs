using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class RemoveBmpColor : MonoBehaviour
{
    public RawImage rawImage;
    [Tooltip("라이팅을 사용할지 여부. 체크 해제 시 데칼사용.")]
    public bool useLighting = true;
    [Tooltip("애니메이션을 사용할지 여부. 체크 해제 시 단일 이미지만 표시됩니다.")]
    public bool useAnimation = true;
    [Tooltip("하위 폴더를 지정할 때 슬래시(/)를 사용하세요. 예: Lamp/SubFolder")]
    public string folderPath = "Lamp";
    [Tooltip("애니메이션 모드에서 사용할 파일 접두사. 예: PlaneIce_001.bmp")]
    public string filePrefix = "PlaneIce_";
    [Tooltip("애니메이션 모드가 꺼져 있을 때 사용할 단일 파일 이름. 예: PlaneIce.bmp")]
    public string singleFileName = "PlaneIce.bmp";
    [Tooltip("애니메이션 모드에서 로드할 프레임 수 (이미지 개수).")]
    public int frameCount = 10;
    [Tooltip("애니메이션 프레임 간격 (초 단위).")]
    public float frameRate = 0.1f;

    [SerializeField]
    private LightAnimationPlayer animationPlayer;
    private List<Texture2D> frames = new List<Texture2D>();
    private int currentFrameIndex = 0;
    private bool isPlaying = false;

    void Start()
    {
        if (useLighting && rawImage == null)
        {
            Debug.LogError("RawImage 객체가 할당되지 않았습니다!");
            return;
        }

        if (animationPlayer)
        {
            animationPlayer.Initialize(frameRate, useLighting, rawImage);
        }
        else if (useAnimation)
        {
            Debug.Log("animationPlayer is Null");
            return;
        }

        if (useAnimation)
            StartCoroutine(LoadAllFrames());
        else
            StartCoroutine(LoadSingleFrame());
    }
    // 프레임용 bmp 로딩
    #region LoadFrames
    // 단일 BMP 파일을 로드하는 코루틴
    IEnumerator LoadSingleFrame()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, folderPath, singleFileName)
            .Replace(Path.DirectorySeparatorChar, '/');
        Debug.Log($"단일 파일 로드 중: {filePath}");

        using (UnityWebRequest uwr = UnityWebRequest.Get(filePath))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"파일 로드 실패 ({singleFileName}): {uwr.error}");
                yield break;
            }

            byte[] fileData = uwr.downloadHandler.data;
            Texture2D sourceTexture = DecodeBMP(fileData);
            if (sourceTexture == null)
            {
                Debug.LogError($"BMP 디코딩 실패: {singleFileName}");
                yield break;
            }

            Texture2D frame = ConvertBlackToTransparent(sourceTexture);
            if (useLighting)
            {
                rawImage.texture = frame;
                // UV Rect 설정 추가
                rawImage.uvRect = new Rect(0, 0, 1, 1); // 전체 텍스처 표시
            }
            else
            {
                var Decal = GetComponent<DecalMaterialChanger>();
                if(Decal)
                    Decal.SetTexture("Base_Map", frame);
            }
            frames.Add(frame);
            Debug.Log($"단일 프레임 로드 성공: {singleFileName}, 크기: {frame.width}x{frame.height}");
        }
    }

    // 모든 BMP 파일을 로드하는 코루틴 (애니메이션용)
    IEnumerator LoadAllFrames()
    {
        for (int i = 0; i < frameCount; ++i)
        {
            string fileName = $"{filePrefix}{i}.bmp";
            string filePath = Path.Combine(Application.streamingAssetsPath, folderPath, fileName)
                .Replace(Path.DirectorySeparatorChar, '/');
            //Debug.Log($"파일 로드 중: {filePath}");

            using (UnityWebRequest uwr = UnityWebRequest.Get(filePath))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"파일 로드 실패 ({fileName}): {uwr.error}");
                    continue;
                }

                byte[] fileData = uwr.downloadHandler.data;
                Texture2D sourceTexture = DecodeBMP(fileData);
                if (sourceTexture == null)
                {
                    Debug.LogError($"BMP 디코딩 실패: {fileName}");
                    continue;
                }

                Texture2D frame = ConvertBlackToTransparent(sourceTexture);
                frames.Add(frame);
                // 첫 프레임일 경우 UV Rect 설정
                if (i == 0 && useLighting)
                {
                    rawImage.texture = frame;
                    rawImage.uvRect = new Rect(0, 0, 1, 1); // 전체 텍스처 표시
                }
                //Debug.Log($"프레임 로드 성공: {fileName}, 크기: {frame.width}x{frame.height}");
            }
        }
        Debug.Log($"{frames.Count} / {frameCount} 프레임 로드 성공");

        if (frames.Count > 0)
        {
            animationPlayer.PlayAnimation(frames); // 애니메이션 재생 위임
            Debug.Log($"애니메이션 시작");
        }

        yield return null;
    }

    // BMP 파일 디코딩 (8비트와 24비트 지원)
    Texture2D DecodeBMP(byte[] fileData)
    {
        if (fileData.Length < 54 || fileData[0] != 'B' || fileData[1] != 'M')
        {
            Debug.LogError("유효하지 않은 BMP 파일");
            return null;
        }

        int width = System.BitConverter.ToInt32(fileData, 18);
        int height = System.BitConverter.ToInt32(fileData, 22);
        short bitDepth = System.BitConverter.ToInt16(fileData, 28);
        int compression = System.BitConverter.ToInt32(fileData, 30);


        if (compression != 0)
        {
            Debug.Log($"비트 깊이: {bitDepth}, 압축 방식: {compression}");
            Debug.LogError("압축된 BMP는 지원되지 않습니다 (RLE 압축 등)");
            return null;
        }

        int pixelOffset = System.BitConverter.ToInt32(fileData, 10);
        Color32[] pixels = new Color32[width * height];

        if (bitDepth == 8)
        {
            Color32[] palette = new Color32[256];
            for (int i = 0; i < 256; i++)
            {
                int offset = 54 + i * 4;
                byte b = fileData[offset];
                byte g = fileData[offset + 1];
                byte r = fileData[offset + 2];
                palette[i] = new Color32(r, g, b, 255);
            }

            int rowBytes = (width + 3) & ~3;
            for (int y = 0; y < height; y++)
            {
                int rowOffset = pixelOffset + (height - 1 - y) * rowBytes;
                for (int x = 0; x < width; x++)
                {
                    byte index = fileData[rowOffset + x];
                    pixels[y * width + x] = palette[index];
                }
            }
        }
        else if (bitDepth == 24)
        {
            int rowBytes = ((width * 3) + 3) & ~3;
            for (int y = 0; y < height; y++)
            {
                int rowOffset = pixelOffset + (height - 1 - y) * rowBytes;
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = rowOffset + x * 3;
                    byte b = fileData[pixelIndex];
                    byte g = fileData[pixelIndex + 1];
                    byte r = fileData[pixelIndex + 2];
                    pixels[y * width + x] = new Color32(r, g, b, 255);
                }
            }
        }
        else
        {
            Debug.LogError($"지원되지 않는 비트 깊이: {bitDepth} (현재 8비트와 24비트만 지원)");
            return null;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // 검은색을 투명하게 변환
    Texture2D ConvertBlackToTransparent(Texture2D sourceTexture)
    {
        Color32[] pixels = sourceTexture.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r == 0 && pixels[i].g == 0 && pixels[i].b == 0)
                pixels[i] = new Color32(0, 0, 0, 0);
        }

        Texture2D newTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        newTexture.SetPixels32(pixels);
        newTexture.Apply();

        Destroy(sourceTexture);
        return newTexture;
    }
    #endregion

    // 애니메이션 재생 중지
    public void StopAnimation()
    {
        isPlaying = false;
    }

    // 스크립트가 비활성화될 때 리소스 정리
    void OnDisable()
    {
        isPlaying = false;
        foreach (var frame in frames)
        {
            if (frame != null)
            {
                Destroy(frame);
            }
        }
        frames.Clear();
    }
}