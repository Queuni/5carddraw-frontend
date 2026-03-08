using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedPortraitAspect : MonoBehaviour
{
    public float targetAspect = 9f / 16f; // 1080x1920

    void Update()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera cam = GetComponent<Camera>();

        if (scaleHeight < 1f)
        {
            // Letterbox (top/bottom)
            cam.rect = new Rect(
                0,
                (1f - scaleHeight) / 2f,
                1f,
                scaleHeight
            );
        }
        else
        {
            // Pillarbox (left/right)
            float scaleWidth = 1f / scaleHeight;
            cam.rect = new Rect(
                (1f - scaleWidth) / 2f,
                0,
                scaleWidth,
                1f
            );
        }
    }
}
