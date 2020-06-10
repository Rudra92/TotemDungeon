using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundBehaviour : MonoBehaviour
{

    private Sound m_sound;

    private SphereCollider m_collider;
    private AudioManager am;
    private GameController gc;

    private bool enabled = true;

    void Awake()
    {
        gc = FindObjectOfType<GameController>();
        am = FindObjectOfType<AudioManager>();
        m_collider = gameObject.AddComponent<SphereCollider>();
        m_collider.isTrigger = true;
        m_collider.radius = 5f;
    }
    void Start()
    {

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnDrawGizmos()
    {
        /*
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 1.5f);
    */
    }

    public void Initialise(Sound sound, Vector3 pos)
    {
        m_sound = sound;
        gameObject.name = m_sound.name;
        transform.position = pos;
    }

    IEnumerator timeout(float time)
    {
        enabled = false;
        yield return new WaitForSeconds(time);
        enabled = true;
        // Code to execute after the delay
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        if(other.tag == "Player")
        {

            // option 1 - multiple different sources may trigger, same ones will not
            if (!m_sound.source.isPlaying)
            {
                //check if there is overlap
                am.checkOverlap();
                m_sound.source.PlayOneShot(m_sound.clip);

                // increment played sound
                int progression = gc.collected;
                am.incrementSoundPlayedCount(m_sound, progression);

                StartCoroutine(timeout(10f));

                
            }
            /*
            // option 2 - only one source plays at a time
            if (!am.IsAnySoundPlaying())
            {
                m_sound.source.PlayOneShot(m_sound.clip);
                timesPlayed++;
            }
            */
            //todo add 3d random location next to the player
        }
    }
}
