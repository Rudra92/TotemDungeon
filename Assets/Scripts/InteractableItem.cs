using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    private bool interactable = false;
    public float boxWidth = 150;
    public float boxHeight = 30;
    public string labelText = "Press E to collect";

    private bool simulation = false;
    private GameController gc;

    void Awake()
    {
        gc = FindObjectOfType<GameController>();
        simulation = gc.simulation;

    }

    private void OnGUI()
    {
        if (interactable)
        {
            GUI.Box(new Rect(Screen.width/2f - boxWidth/2f  , Screen.height/1.7f - boxHeight/2f , boxWidth, boxHeight), (labelText));
        }
    }

    public void Update()
    {
        if(interactable)
        {
            if (Input.GetKeyUp(KeyCode.E))
            {
                interactable = false;
                Destroy(gameObject);


                FindObjectOfType<GameController>().collect();

                return;
            }
        }
        
    }


    private void OnTriggerEnter(Collider c)
    {
        if (c.gameObject.tag == "Player")
        {
            interactable = true;
            if(simulation)
            {
                gc.collect();

                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider c )
    {
        interactable = false;
    }
}
