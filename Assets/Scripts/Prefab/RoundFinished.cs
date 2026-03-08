using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundFinished : MonoBehaviour
{
    public Button continueButton;
    public Button quitButton;
    public TextMeshProUGUI contentMesh;
    public TextMeshProUGUI titleMesh;

    // Start is called before the first frame update
    void Start()
    {
        if (titleMesh == null)
        {
            Debug.Log("title mesh not found");
        }

        if (contentMesh == null)
        {
            Debug.Log("content mesh not found");
        }

        if (continueButton == null)
        {
            Debug.Log("continue button not found");
        }

        if (quitButton == null)
        {
            Debug.Log("quit button not found");
        }
    }

   
    public void showDialog(string title, string content)
    {
        titleMesh.text = title;
        contentMesh.text = content;
        this.gameObject.SetActive(true);
    }

    public void hideDialog()
    {
        this.gameObject.SetActive(false);
    }
}
