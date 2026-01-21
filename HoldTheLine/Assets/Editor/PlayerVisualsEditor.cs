// PlayerVisualsEditor.cs - Editor tool for managing player visuals
// Location: Assets/Editor/
// Menu: Tools > Player Visuals > ...

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace HoldTheLine.Editor
{
    /// <summary>
    /// Editor tools for managing player robot visuals.
    /// </summary>
    public static class PlayerVisualsEditor
    {
        [MenuItem("Tools/Player Visuals/Install Robot on Selected Player")]
        public static void InstallRobotOnSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Please select the Player GameObject in the Hierarchy.", "OK");
                return;
            }

            if (!selected.CompareTag("Player"))
            {
                if (!EditorUtility.DisplayDialog("Not Tagged as Player",
                    $"'{selected.name}' is not tagged as 'Player'. Install robot visuals anyway?",
                    "Yes", "Cancel"))
                {
                    return;
                }
            }

            // Check if already has the component
            PlayerVisualsInstaller existing = selected.GetComponent<PlayerVisualsInstaller>();
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Already Installed",
                    "PlayerVisualsInstaller is already on this object. Remove and reinstall?",
                    "Reinstall", "Cancel"))
                {
                    return;
                }

                // Remove existing
                Object.DestroyImmediate(existing);

                // Remove existing visuals child
                Transform visuals = selected.transform.Find("Visuals");
                if (visuals != null)
                {
                    Object.DestroyImmediate(visuals.gameObject);
                }
            }

            // Add the component
            selected.AddComponent<PlayerVisualsInstaller>();

            EditorUtility.DisplayDialog("Success",
                $"PlayerVisualsInstaller added to '{selected.name}'.\n\n" +
                "Press Play to see the robot visuals.", "OK");

            Debug.Log($"[PlayerVisualsEditor] Installed robot visuals on {selected.name}");
        }

        [MenuItem("Tools/Player Visuals/Remove Robot from Selected Player")]
        public static void RemoveRobotFromSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Please select the Player GameObject in the Hierarchy.", "OK");
                return;
            }

            PlayerVisualsInstaller installer = selected.GetComponent<PlayerVisualsInstaller>();
            if (installer == null)
            {
                EditorUtility.DisplayDialog("Not Found",
                    "No PlayerVisualsInstaller found on selected object.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Confirm Removal",
                "Remove robot visuals from player?",
                "Remove", "Cancel"))
            {
                // Remove the component
                Object.DestroyImmediate(installer);

                // Remove visuals child if exists
                Transform visuals = selected.transform.Find("Visuals");
                if (visuals != null)
                {
                    Object.DestroyImmediate(visuals.gameObject);
                }

                // Re-enable the original mesh renderer
                MeshRenderer rend = selected.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.enabled = true;
                }

                EditorUtility.DisplayDialog("Success",
                    "Robot visuals removed. Original capsule will be visible.", "OK");

                Debug.Log($"[PlayerVisualsEditor] Removed robot visuals from {selected.name}");
            }
        }

        [MenuItem("Tools/Player Visuals/Find Player in Scene")]
        public static void FindPlayerInScene()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Selection.activeGameObject = player;
                EditorGUIUtility.PingObject(player);
                Debug.Log($"[PlayerVisualsEditor] Found player: {player.name}");
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found",
                    "No GameObject with 'Player' tag found in scene.\n\n" +
                    "Note: The player is created at runtime by AutoInitializer. " +
                    "Press Play first to create it.", "OK");
            }
        }

        [MenuItem("Tools/Player Visuals/About Robot Visuals")]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog("Player Robot Visuals",
                "This system replaces the basic capsule player visual with a procedural robot.\n\n" +
                "Features:\n" +
                "- Procedural robot made from Unity primitives\n" +
                "- Procedural animation (no AnimatorController needed)\n" +
                "- Idle: Subtle breathing/bobbing\n" +
                "- Move: Walking animation with arm/leg swing\n" +
                "- URP-compatible materials\n\n" +
                "The robot visuals are created at runtime by PlayerVisualsInstaller.\n" +
                "Original colliders and scripts are preserved.\n\n" +
                "Controls: A/D or Arrow Keys to move horizontally.",
                "OK");
        }
    }
}
#endif
