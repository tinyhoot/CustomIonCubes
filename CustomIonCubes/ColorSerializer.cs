using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nautilus.Json.Converters;
using Nautilus.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomIonCubes
{
    internal class ColorSerializer
    {
        private string _colorDirectory;
        
        public ColorSerializer(string directory)
        {
            _colorDirectory = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName, directory);
        }

        /// <summary>
        /// Try to load all custom color profiles from the color directory.
        /// </summary>
        public IEnumerator LoadAllColors(IOut<List<CubeColor>> colors)
        {
            // Ensure the directory always exists, even if the user accidentally deleted it.
            if (!Directory.Exists(_colorDirectory))
                Directory.CreateDirectory(_colorDirectory);
            
            List<Task<CubeColor>> tasks = new List<Task<CubeColor>>();
            foreach (string path in Directory.EnumerateFiles(_colorDirectory))
            {
                tasks.Add(LoadAsync(path));
            }

            // Wait until all these async file operations are done.
            yield return new WaitUntil(() => tasks.TrueForAll(t => t.IsCompleted || t.IsFaulted));
            colors.Set(tasks.Where(t => t.IsCompleted && t.Result != null).Select(t => t.Result).ToList());
        }

        /// <summary>
        /// Load a <see cref="CubeColor"/> from a file on disk.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        private async Task<CubeColor> LoadAsync(string path)
        {
            CustomIonCubesInit._log.LogDebug($"Attempting to read colour data from '{Path.GetFileName(path)}'");
            try
            {
                using StreamReader reader = new StreamReader(File.OpenRead(path));
                string json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<CubeColor>(json, new ColorConverter());
            }
            catch (Exception ex)
            {
                CustomIonCubesInit._log.LogWarning($"Failed to parse data from file '{Path.GetFileName(path)}'. Error was:");
                CustomIonCubesInit._log.LogWarning($"{ex.GetType()}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save an existing color to disk.
        /// </summary>
        public void Save(string path, CubeColor color)
        {
            JsonUtils.Save(color, Path.Combine(_colorDirectory, path), new ColorConverter());
        }
    }
}