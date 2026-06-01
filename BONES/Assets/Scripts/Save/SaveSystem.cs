using System;
using System.IO;
using UnityEngine;
using Bones.Model;

namespace Bones.Save
{
    /// <summary>
    /// JSON persistence to Application.persistentDataPath. Fully offline. Save on every settle /
    /// purchase / night transition so a kill resumes cleanly (plan: State + persistence).
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "bones_save.json";

        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(Path)) return new SaveData();
                string json = File.ReadAllText(Path);
                var data = JsonUtility.FromJson<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BONES] Save load failed, starting fresh: {e.Message}");
                return new SaveData();
            }
        }

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(Path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BONES] Save write failed: {e.Message}");
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(Path)) File.Delete(Path); }
            catch (Exception e) { Debug.LogWarning($"[BONES] Save delete failed: {e.Message}"); }
        }
    }
}
