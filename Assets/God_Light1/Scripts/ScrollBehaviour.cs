using UnityEngine;
using System.Collections;

public class ScrollBehaviour : MonoBehaviour
{

    public int materialIndex;
    public string textureName = "_MainTex";
    public Vector2 uvAnimationRate = new Vector2(1f, 0f);
    private Vector2 uvOffset = Vector2.zero;


    private void LateUpdate()
    {
        uvOffset += (Vector2) (uvAnimationRate * Time.deltaTime);
        if (base.GetComponent<Renderer>().enabled)
        {
            base.GetComponent<Renderer>().materials[materialIndex].SetTextureOffset(textureName, uvOffset);
        }
    }
}


