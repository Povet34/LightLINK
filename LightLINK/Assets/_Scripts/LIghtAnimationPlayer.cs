using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class LightAnimationPlayer : MonoBehaviour
{
    /*
     Texture2D 애니메이션
     */
    private RawImage targetImage;
    private List<Texture2D> frames;
    private float frameRate;
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private bool useLighting = true;

    public void Initialize(float rate, bool isLighting, RawImage target = null)
    {
        frameRate = rate;
        useLighting = isLighting;
        targetImage = target;
    }

    public void PlayAnimation(List<Texture2D> animationFrames)
    {
        if (animationFrames == null || animationFrames.Count == 0)
        {
            Debug.LogError("애니메이션 프레임이 없습니다!");
            return;
        }

        frames = animationFrames;
        currentFrameIndex = 0;
        isPlaying = true;
        StartCoroutine(Play());
    }

    public void StopAnimation()
    {
        isPlaying = false;
    }

    public void RestartAnimation()
    {
        if (frames != null && frames.Count > 0)
        {
            currentFrameIndex = 0;
            isPlaying = true;
            StartCoroutine(Play());
        }
    }

    private IEnumerator Play()
    {
        var Decal = GetComponent<DecalMaterialChanger>();
        while (isPlaying)
        {
            if (Decal && !useLighting)
            {
                Decal.SetTexture("Base_Map", frames[currentFrameIndex]);
            }
            else
            {
                targetImage.texture = frames[currentFrameIndex];
            }
            currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
            yield return new WaitForSeconds(frameRate);
        }
    }
}