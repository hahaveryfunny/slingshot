// using UnityEngine;

// public class QuickMobileDebug : MonoBehaviour
// {
//     public static QuickMobileDebug Instance;
//     private string debugText = "";
//     private GUIStyle guiStyle;

//     float deltaTime = 0f;

//     void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     void Start()
//     {
//         guiStyle = new GUIStyle();
//         guiStyle.fontSize = 72; // Large enough for mobile
//         guiStyle.normal.textColor = new Color32(255, 227, 0, 255);
//         guiStyle.wordWrap = true;
//     }

//     void OnGUI()
//     {
//         GUI.Label(new Rect(0, 256, Screen.width - (Screen.width * .1f), Screen.height - (Screen.height * .1f)), debugText, guiStyle);
//     }

//     void Update()
//     {
//         Clear();
//         deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
//         float fps = 1.0f / deltaTime;
//         Log("FPS: " + Mathf.Ceil(fps).ToString());
//         //Log("Batches: " + UnityEditor.UnityStats.batches);
//     }

//     public static void Log(string message)
//     {
//         if (Instance != null)
//         {
//             Instance.debugText +=
//             //System.DateTime.Now.ToString("HH:mm:ss") + ": "+
//             message + "\n";

//             // Keep only last 20 lines
//             string[] lines = Instance.debugText.Split('\n');
//             if (lines.Length > 20)
//             {
//                 Instance.debugText = string.Join("\n", lines, lines.Length - 20, 20);
//             }
//         }

//         // Also log to Unity console
// //        Debug.Log(message);
//     }

//     public static void Clear()
//     {
//         if (Instance != null)
//         {
//             Instance.debugText = "";
//         }
//     }
// }