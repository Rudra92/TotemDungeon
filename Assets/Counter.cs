using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour
{

    public static Counter singleton;

    public int counter;
    // Start is called before the first frame update
    void Start()
    {
        counter = FindObjectOfType<GameController>().iterations - 1;
        DontDestroyOnLoad(gameObject);

        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
