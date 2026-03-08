using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SensScrollView : MonoBehaviour
{
    private ScrollRect scrollRect;

    // Start is called before the first frame update
    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_WEBGL
        float wheel = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(wheel) > 0.001f)
        {
            scrollRect.verticalNormalizedPosition += wheel * 0.1f;
        }
#endif
    }
}
