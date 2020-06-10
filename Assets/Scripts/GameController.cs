using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameController : MonoBehaviour
{
    public NystromGenerator levelGenerator;
    [HideInInspector]
    public bool loadLevel = false;
    [HideInInspector]
    public bool loadCollectibles = false;
    [HideInInspector]
    public bool save = false;
    public float playerSpeed;
    [HideInInspector]
    public float npcChaseSpeed;
    [HideInInspector]
    public float npcSeekSpeed;
    public bool randomAlgorithm = false;
    public bool option1 = false;
    public bool option2 = true;
    public bool test = false;
    public bool simulation = false;
    [HideInInspector]
    public int iterations = 1;
    [HideInInspector]
    public float repCrit = 1f;
    [HideInInspector]
    public float countCrit = 1f;
    [HideInInspector]
    public float covCrit = 1f;
    [HideInInspector]
    public float progCrit = 1f;
    [HideInInspector]
    public float overCrit = 1f;
    
    [HideInInspector]
    public bool npcEnabled = false;
    [HideInInspector]
    public int collectibles = 5;
    [HideInInspector]
    public int collected = 0;
    private GameObject player;
    private Counter counter;
    [HideInInspector]
    public bool generated = false;

    private AudioManager am;
    private AIAgent aiAgent;
    private bool end = false;
    private bool soundPlaced = false;

    // Start is called before the first frame update
    void Start()
    {
        am = FindObjectOfType<AudioManager>();
        counter = FindObjectOfType<Counter>();
        
        levelGenerator.setupGame(collectibles, npcEnabled, loadLevel, loadCollectibles);

        generated = true;

        player = GameObject.FindGameObjectWithTag("Player");

        if (simulation) aiAgent = player.AddComponent<AIAgent>();

        //am.PlayTheme();
        counter.counter--;

        
    }

    // Update is called once per frame
    void Update()
    {
        if(!soundPlaced && levelGenerator.readyToPlace())
        {
            soundPlaced = true;
            if (randomAlgorithm)
            {
                am.BaselineAlgorithm();
            } 
            if (!randomAlgorithm)
            {
                if(option1)
                {
                    am.pathsAlgorithm();

                }
                if(option2)
                {
                    am.distanceAlgorithm();
                }
                
            }
            
            
        }

        if(!end && collected == collectibles)
        {
            Debug.Log("Finish Game");
            end = true;
            if (simulation)
            {
                Debug.Log("Evaluation Function result : " + aiAgent.EvaluateSound());
                if (counter.counter > 0)
                {
                    levelGenerator.Restart();
                }
            }
            //QuitGame();
        }

        // debugging function
        if(test)
        {
            test = false;
            
        }
        
    }

    public void collect()
    {
        collected++;
        if(option1)
        {
            am.updatePathsAlgorithm();
        }
    }

    public void QuitGame()
    {
        // save any game data here
        #if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

// Custom Editor using SerializedProperties.
// Automatic handling of multi-object editing, undo, and Prefab overrides.
[CustomEditor(typeof(GameController))]
[CanEditMultipleObjects]
public class EditorGUIGameController : Editor
{

    SerializedProperty npc_chase_prop;
    SerializedProperty npc_seek_prop;
    SerializedProperty npc_enabled_prop;
    SerializedProperty collectible_number_prop;
    SerializedProperty collected_number_prop;

    SerializedProperty save_prop;
    SerializedProperty generated_prop;
    SerializedProperty load_prop;
    SerializedProperty load_collect_prop;
    SerializedProperty simulation_prop;
    SerializedProperty simulation_iter;

    //criteria
    SerializedProperty rep_crit_prop;
    SerializedProperty count_crit_prop;
    SerializedProperty cover_crit_prop;
    SerializedProperty prog_crit_prop;
    SerializedProperty overlap_crit_prop;

    void OnEnable()
    {
        collectible_number_prop = serializedObject.FindProperty("collectibles"); ;
        collected_number_prop = serializedObject.FindProperty("collected"); ;

        npc_enabled_prop = serializedObject.FindProperty("npcEnabled");
        npc_chase_prop = serializedObject.FindProperty("npcChaseSpeed");
        npc_seek_prop = serializedObject.FindProperty("npcSeekSpeed");

        save_prop = serializedObject.FindProperty("save");
        generated_prop = serializedObject.FindProperty("generated");
        load_prop = serializedObject.FindProperty("loadLevel");
        load_collect_prop = serializedObject.FindProperty("loadCollectibles");

        simulation_prop = serializedObject.FindProperty("simulation");
        simulation_iter = serializedObject.FindProperty("iterations");

        //criteria
        rep_crit_prop = serializedObject.FindProperty("repCrit");
        count_crit_prop = serializedObject.FindProperty("countCrit");
        cover_crit_prop = serializedObject.FindProperty("covCrit");
        prog_crit_prop = serializedObject.FindProperty("progCrit");
        overlap_crit_prop = serializedObject.FindProperty("overCrit");

    }

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();


        if (simulation_prop.boolValue)
        {
            EditorGUILayout.PropertyField(simulation_iter, new GUIContent("Simulation Iterations"));
            EditorGUILayout.PropertyField(rep_crit_prop, new GUIContent("Repetition Criterion weight"));
            EditorGUILayout.PropertyField(count_crit_prop, new GUIContent("Count Criterion weight"));
            EditorGUILayout.PropertyField(cover_crit_prop, new GUIContent("Coverage Criterion weight"));
            EditorGUILayout.PropertyField(prog_crit_prop, new GUIContent("Coherence Criterion weight"));
            EditorGUILayout.PropertyField(overlap_crit_prop, new GUIContent("Overlap Criterion weight"));
        }

        EditorGUILayout.PropertyField(collected_number_prop, new GUIContent("Items Collected"));

        GUI.enabled = !Application.isPlaying;
        EditorGUILayout.PropertyField(collectible_number_prop, new GUIContent("Collectibles"));
        EditorGUILayout.PropertyField(load_prop, new GUIContent("Use pre-computed level"));
        if(load_prop.boolValue)
        {
            EditorGUILayout.PropertyField(load_collect_prop, new GUIContent("Load collectible locations"));
        }


        if (save_prop.boolValue)
        {
            GUI.enabled = false ;
        }
        else
        {
            GUI.enabled = true;
        }
        if (generated_prop.boolValue) 
        {
            EditorGUILayout.PropertyField(save_prop, new GUIContent("Save Level"));
        }

        GUI.enabled = !Application.isPlaying;
        EditorGUILayout.PropertyField(npc_enabled_prop, new GUIContent("Enable NPC"));
        bool npcEnable = npc_enabled_prop.boolValue;
        GUI.enabled = true;


        if (npcEnable)
        {
            EditorGUILayout.PropertyField(npc_chase_prop, new GUIContent("NPC chase speed"));
            EditorGUILayout.PropertyField(npc_seek_prop, new GUIContent("NPC seek speed"));
        }

        
        // Apply changes to the serializedProperty - always do this at the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }
}
