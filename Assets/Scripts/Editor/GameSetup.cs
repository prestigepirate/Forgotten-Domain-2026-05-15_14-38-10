#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgottenDomain.Editor
{
    public class GameSetup : EditorWindow
    {
        [MenuItem("Window/Forgotten Domain/Setup Game")]
        public static void Run()
        {
            // 1. Cards — use existing, don't overwrite.
            var cards = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Cards" });
            if (cards.Length == 0) Debug.LogWarning("[GameSetup] No cards in Assets/Cards. Create via Assets → Forgotten Domain → CardData.");

            // 2. Game object.
            var existing = FindAnyObjectByType<GameManager>();
            if (existing != null) DestroyImmediate(existing.gameObject);

            var gameGO = new GameObject("Game");
            var gm = gameGO.AddComponent<GameManager>();
            var hm = gameGO.AddComponent<HandManager>();

            var serializedHM = new SerializedObject(hm);
            var prop = serializedHM.FindProperty("deckCards");
            prop.ClearArray();
            foreach (var guid in cards)
            {
                var card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (card == null) continue;
                prop.InsertArrayElementAtIndex(prop.arraySize);
                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = card;
            }
            serializedHM.ApplyModifiedProperties();

            // 3. EventSystem.
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 4. Camera.
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraController>() == null)
            {
                cam.gameObject.AddComponent<CameraController>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
            }

            EditorSceneManager.MarkSceneDirty(gameGO.scene);
            Debug.Log($"[GameSetup] Done. {prop.arraySize} cards wired. GameManager + HandManager + CameraController ready.");
        }
    }
}
#endif
