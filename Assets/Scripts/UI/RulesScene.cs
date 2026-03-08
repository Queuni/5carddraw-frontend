using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuleScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnBackButtonClicked()
    {
        Utils.LoadScene("MainMenuScene");
    }
}
