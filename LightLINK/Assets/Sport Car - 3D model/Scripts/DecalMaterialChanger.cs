using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DecalMaterialChanger : MonoBehaviour
{
    public DecalProjector decalProjector; // 데칼 프로젝터 참조
    public Texture2D newDecalTexture; // 변경할 새로운 텍스처

    void Start()
    {
        if (decalProjector == null)
            decalProjector = GetComponent<DecalProjector>();
        SetTexture("Base_Map", newDecalTexture);
    }

    public void SetTexture(string name, Texture texture)
    {
        if (decalProjector == null)
        {
            Debug.LogWarning("decalProjector is Null");
            return;
        }

        Material newMaterialInstance = new Material(decalProjector.material);
        newMaterialInstance.SetTexture(name, texture); // Base Map 텍스처 변경
        decalProjector.material = newMaterialInstance;
    }
}