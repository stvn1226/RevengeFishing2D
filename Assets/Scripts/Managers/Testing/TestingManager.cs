using UnityEngine;

public class TestingManager : MonoBehaviour
{
    public static TestingManager instance;

    public bool isTesting = false;
    
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
        {
            Debug.LogError("More than one instance of this singleton exists");
            Destroy(gameObject);
        }
    }
}
