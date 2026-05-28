using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FairyRgbController.Models
{
    public class EffectFolder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Uusi kansio";
        public List<string> EffectIds { get; set; } = new();
    }

    public class CustomEffect
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public byte EffectId { get; set; }
        public string CustomName { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public string? FolderId { get; set; }
    }

    public class EffectsData
    {
        public List<EffectFolder> Folders { get; set; } = new();
        public List<CustomEffect> CustomEffects { get; set; } = new();
        public string LastSelectedFolderId { get; set; } = "";
    }

    public static class EffectsManager
    {
        private static readonly string DataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FairyRgbController", "effects.json");

        private static EffectsData? _data;

        public static EffectsData Load()
        {
            if (_data != null) return _data;

            try
            {
                if (File.Exists(DataPath))
                {
                    var json = File.ReadAllText(DataPath);
                    _data = JsonSerializer.Deserialize<EffectsData>(json) ?? new EffectsData();
                }
                else
                {
                    _data = new EffectsData();
                }
            }
            catch
            {
                _data = new EffectsData();
            }

            // Ensure default folder exists
            if (_data.Folders.Count == 0)
            {
                var defaultFolder = new EffectFolder { Name = "Oletus" };
                _data.Folders.Add(defaultFolder);
            }

            return _data;
        }

        public static void Save()
        {
            if (_data == null) return;

            try
            {
                var dir = Path.GetDirectoryName(DataPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataPath, json);
            }
            catch { }
        }

        public static void AddFolder(string name)
        {
            var data = Load();
            data.Folders.Add(new EffectFolder { Name = name });
            Save();
        }

        public static void DeleteFolder(string folderId)
        {
            var data = Load();
            data.Folders.RemoveAll(f => f.Id == folderId);
            // Move effects to default folder
            foreach (var effect in data.CustomEffects.Where(e => e.FolderId == folderId))
            {
                effect.FolderId = data.Folders[0].Id;
            }
            Save();
        }

        public static void RenameEffect(string effectId, string newName)
        {
            var data = Load();
            var effect = data.CustomEffects.FirstOrDefault(e => e.Id == effectId);
            if (effect != null)
            {
                effect.CustomName = newName;
                Save();
            }
        }

        public static void MoveEffectToFolder(string effectId, string? folderId)
        {
            var data = Load();
            var effect = data.CustomEffects.FirstOrDefault(e => e.Id == effectId);
            if (effect != null)
            {
                effect.FolderId = folderId;
                Save();
            }
        }

        public static string GetEffectName(byte effectId)
        {
            var data = Load();
            var customEffect = data.CustomEffects.FirstOrDefault(e => e.EffectId == effectId);
            if (customEffect != null && !string.IsNullOrEmpty(customEffect.CustomName))
                return customEffect.CustomName;

            // Return default name
            return $"Efekti {effectId}";
        }

        public static CustomEffect GetOrCreateEffect(byte effectId, string originalName)
        {
            var data = Load();
            var effect = data.CustomEffects.FirstOrDefault(e => e.EffectId == effectId);
            if (effect == null)
            {
                effect = new CustomEffect
                {
                    EffectId = effectId,
                    OriginalName = originalName,
                    CustomName = originalName
                };
                data.CustomEffects.Add(effect);
                Save();
            }
            else if (string.IsNullOrEmpty(effect.OriginalName))
            {
                effect.OriginalName = originalName;
            }
            return effect;
        }
    }
}