using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PrivacyPolicy : MonoBehaviour {

    public GameObject panel;
    public TextAsset[] dialogContentTexts;
    private string[] dialogContentTextCaches;
    public Text contentField;
    
	void Start () {
        panel.SetActive(false);
        dialogContentTextCaches = new string[dialogContentTexts.Length];
        for (int i = 0; i < dialogContentTexts.Length; i++)
        {
            dialogContentTextCaches[i] = dialogContentTexts[i].text;
        }        
	}

    public void Open()
    {
        panel.SetActive(true);
        //foreach (string content in dialogContentTextCaches)
        //    contentField.text += content;
    }
    public void Close()
    {
        panel.SetActive(false);
    }
}
