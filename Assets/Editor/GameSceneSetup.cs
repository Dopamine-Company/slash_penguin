using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Dopamine.SlashPenguin;

namespace Dopamine.SlashPenguin.Editor
{
    public static class GameSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string SettingsPath = "Assets/Resources/PenguinGameSettings.asset";

        [MenuItem("SlashPenguin/① Create Whitebox Scene", priority = 1)]
        public static void CreateWhiteboxScene()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Resources");

            // PenguinGameSettings SO 생성 (없으면)
            if (!File.Exists(Path.GetFullPath(SettingsPath)))
            {
                var settings = ScriptableObject.CreateInstance<PenguinGameSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            // 새 씬 생성 (Main Camera + Directional Light 포함)
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // GameManagers 오브젝트 생성
            var managersGO = new GameObject("GameManagers");
            var bootstrap = managersGO.AddComponent<GameBootstrap>();

            // PenguinGameSettings 연결
            var so = new SerializedObject(bootstrap);
            so.FindProperty("_settings").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PenguinGameSettings>(SettingsPath);
            so.ApplyModifiedProperties();

            // 씬 저장
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Build Settings 첫 번째 씬으로 등록
            RegisterInBuildSettings(ScenePath);

            AssetDatabase.Refresh();
            Debug.Log("[SlashPenguin] ✓ GameScene 생성 완료 → " + ScenePath
                + "\n  Play 버튼을 누르면 흰박스 루프가 시작됩니다.");
        }

        [MenuItem("SlashPenguin/① Create Whitebox Scene", validate = true)]
        private static bool ValidateCreate() =>
            !File.Exists(Path.GetFullPath(ScenePath));   // 이미 있으면 메뉴 비활성화

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void RegisterInBuildSettings(string scenePath)
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
                if (s.path == scenePath) return;

            var updated = new EditorBuildSettingsScene[existing.Length + 1];
            updated[0] = new EditorBuildSettingsScene(scenePath, true);
            System.Array.Copy(existing, 0, updated, 1, existing.Length);
            EditorBuildSettings.scenes = updated;
        }
    }
}
