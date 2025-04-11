using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DecalMaterialChanger : MonoBehaviour
{
    public DecalProjector decalProjector; // ��Į �������� ����
    public Texture2D newDecalTexture; // ������ ���ο� �ؽ�ó

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
        newMaterialInstance.SetTexture(name, texture); // Base Map �ؽ�ó ����
        decalProjector.material = newMaterialInstance;
    }
}