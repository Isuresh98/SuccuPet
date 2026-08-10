using UnityEngine;

public class ShowSavePath : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("JSON Save Folder: " +
                  Application.persistentDataPath);
    }
}