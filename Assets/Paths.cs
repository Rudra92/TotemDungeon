using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paths : MonoBehaviour
{
    [SerializeField] private NystromGenerator levelGenerator;
    private List<GameObject> collectibles;
    
    // Start is called before the first frame update
    void Start()
    {
        collectibles = levelGenerator.mCollectibles;
        Destroy(collectibles[0]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
