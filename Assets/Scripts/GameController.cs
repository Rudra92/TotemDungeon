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
    public bool save = false;
    public float playerSpeed;
    [HideInInspector]
    public float npcChaseSpeed;
    [HideInInspector]
    public float npcSeekSpeed;
    public bool test = false;
    [HideInInspector]
    public int collectibles = 5;
    public int collected = 0;
    [HideInInspector]
    public bool npcEnabled = false;

    private GameObject player;
    [HideInInspector]
    public bool generated = false;

    // Start is called before the first frame update
    void Start()
    {
        levelGenerator.setupGame(collectibles, npcEnabled, loadLevel);
        generated = true;

        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if(collected == collectibles)
        {
            Debug.Log("Finish Game");
            QuitGame();
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
public class EditorGUILayoutPropertyField : Editor
{

    SerializedProperty npc_chase_prop;
    SerializedProperty npc_seek_prop;
    SerializedProperty npc_enabled_prop;
    SerializedProperty collectible_number_prop;
    SerializedProperty save_prop;
    SerializedProperty generated_prop;
    SerializedProperty load_prop;



    void OnEnable()
    {
        collectible_number_prop = serializedObject.FindProperty("collectibles"); ;

        npc_enabled_prop = serializedObject.FindProperty("npcEnabled");
        npc_chase_prop = serializedObject.FindProperty("npcChaseSpeed");
        npc_seek_prop = serializedObject.FindProperty("npcSeekSpeed");

        save_prop = serializedObject.FindProperty("save");
        generated_prop = serializedObject.FindProperty("generated");
        load_prop = serializedObject.FindProperty("loadLevel");


    }

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        GUI.enabled = !Application.isPlaying;
        EditorGUILayout.PropertyField(collectible_number_prop, new GUIContent("Collectibles"));
        EditorGUILayout.PropertyField(load_prop, new GUIContent("Use pre-computed level"));
        if(save_prop.boolValue)
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
        EditorGUILayout.PropertyField(npc_enabled_prop, new GUIContent("Enable NPC"));

        GUI.enabled = !Application.isPlaying;
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
