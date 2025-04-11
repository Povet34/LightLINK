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
    [Tooltip("�������� ������� ����. üũ ���� �� ��Į���.")]
    public bool useLighting = true;
    [Tooltip("�ִϸ��̼��� ������� ����. üũ ���� �� ���� �̹����� ǥ�õ˴ϴ�.")]
    public bool useAnimation = true;
    [Tooltip("���� ������ ������ �� ������(/)�� ����ϼ���. ��: Lamp/SubFolder")]
    public string folderPath = "Lamp";
    [Tooltip("�ִϸ��̼� ��忡�� ����� ���� ���λ�. ��: PlaneIce_001.bmp")]
    public string filePrefix = "PlaneIce_";
    [Tooltip("�ִϸ��̼� ��尡 ���� ���� �� ����� ���� ���� �̸�. ��: PlaneIce.bmp")]
    public string singleFileName = "PlaneIce.bmp";
    [Tooltip("�ִϸ��̼� ��忡�� �ε��� ������ �� (�̹��� ����).")]
    public int frameCount = 10;
    [Tooltip("�ִϸ��̼� ������ ���� (�� ����).")]
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
            Debug.LogError("RawImage ��ü�� �Ҵ���� �ʾҽ��ϴ�!");
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
    // �����ӿ� bmp �ε�
    #region LoadFrames
    // ���� BMP ������ �ε��ϴ� �ڷ�ƾ
    IEnumerator LoadSingleFrame()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, folderPath, singleFileName)
            .Replace(Path.DirectorySeparatorChar, '/');
        Debug.Log($"���� ���� �ε� ��: {filePath}");

        using (UnityWebRequest uwr = UnityWebRequest.Get(filePath))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"���� �ε� ���� ({singleFileName}): {uwr.error}");
                yield break;
            }

            byte[] fileData = uwr.downloadHandler.data;
            Texture2D sourceTexture = DecodeBMP(fileData);
            if (sourceTexture == null)
            {
                Debug.LogError($"BMP ���ڵ� ����: {singleFileName}");
                yield break;
            }

            Texture2D frame = ConvertBlackToTransparent(sourceTexture);
            if (useLighting)
            {
                rawImage.texture = frame;
                // UV Rect ���� �߰�
                rawImage.uvRect = new Rect(0, 0, 1, 1); // ��ü �ؽ�ó ǥ��
            }
            else
            {
                var Decal = GetComponent<DecalMaterialChanger>();
                if(Decal)
                    Decal.SetTexture("Base_Map", frame);
            }
            frames.Add(frame);
            Debug.Log($"���� ������ �ε� ����: {singleFileName}, ũ��: {frame.width}x{frame.height}");
        }
    }

    // ��� BMP ������ �ε��ϴ� �ڷ�ƾ (�ִϸ��̼ǿ�)
    IEnumerator LoadAllFrames()
    {
        for (int i = 0; i < frameCount; ++i)
        {
            string fileName = $"{filePrefix}{i}.bmp";
            string filePath = Path.Combine(Application.streamingAssetsPath, folderPath, fileName)
                .Replace(Path.DirectorySeparatorChar, '/');
            //Debug.Log($"���� �ε� ��: {filePath}");

            using (UnityWebRequest uwr = UnityWebRequest.Get(filePath))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"���� �ε� ���� ({fileName}): {uwr.error}");
                    continue;
                }

                byte[] fileData = uwr.downloadHandler.data;
                Texture2D sourceTexture = DecodeBMP(fileData);
                if (sourceTexture == null)
                {
                    Debug.LogError($"BMP ���ڵ� ����: {fileName}");
                    continue;
                }

                Texture2D frame = ConvertBlackToTransparent(sourceTexture);
                frames.Add(frame);
                // ù �������� ��� UV Rect ����
                if (i == 0 && useLighting)
                {
                    rawImage.texture = frame;
                    rawImage.uvRect = new Rect(0, 0, 1, 1); // ��ü �ؽ�ó ǥ��
                }
                //Debug.Log($"������ �ε� ����: {fileName}, ũ��: {frame.width}x{frame.height}");
            }
        }
        Debug.Log($"{frames.Count} / {frameCount} ������ �ε� ����");

        if (frames.Count > 0)
        {
            animationPlayer.PlayAnimation(frames); // �ִϸ��̼� ��� ����
            Debug.Log($"�ִϸ��̼� ����");
        }

        yield return null;
    }

    // BMP ���� ���ڵ� (8��Ʈ�� 24��Ʈ ����)
    Texture2D DecodeBMP(byte[] fileData)
    {
        if (fileData.Length < 54 || fileData[0] != 'B' || fileData[1] != 'M')
        {
            Debug.LogError("��ȿ���� ���� BMP ����");
            return null;
        }

        int width = System.BitConverter.ToInt32(fileData, 18);
        int height = System.BitConverter.ToInt32(fileData, 22);
        short bitDepth = System.BitConverter.ToInt16(fileData, 28);
        int compression = System.BitConverter.ToInt32(fileData, 30);


        if (compression != 0)
        {
            Debug.Log($"��Ʈ ����: {bitDepth}, ���� ���: {compression}");
            Debug.LogError("����� BMP�� �������� �ʽ��ϴ� (RLE ���� ��)");
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
            Debug.LogError($"�������� �ʴ� ��Ʈ ����: {bitDepth} (���� 8��Ʈ�� 24��Ʈ�� ����)");
            return null;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // �������� �����ϰ� ��ȯ
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

    // �ִϸ��̼� ��� ����
    public void StopAnimation()
    {
        isPlaying = false;
    }

    // ��ũ��Ʈ�� ��Ȱ��ȭ�� �� ���ҽ� ����
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