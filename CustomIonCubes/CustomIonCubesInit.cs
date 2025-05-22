using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CustomIonCubes
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0.0.33")]
    internal class CustomIonCubesInit : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.CustomIonCubes";
        public const string NAME = "CustomIonCubes";
        public const string VERSION = "0.1";

        internal static ManualLogSource _log;
        internal static Material _hueshift;
        internal static Material _hueDisco;

        private void Awake()
        {
            _log = Logger;
            _log.LogInfo($"{NAME} v{VERSION} ready.");
            
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName, "hueshiftshader"));
            var shaderMaterial = bundle.LoadAsset<Material>("stencilhueshift");
            _hueshift = shaderMaterial;
            _log.LogDebug($"Shader: {shaderMaterial}");
            _hueDisco = bundle.LoadAsset<Material>("stencildisco");

            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        private IEnumerator Start()
        {
            // Run a coroutine to read and parse all custom cube colour files from disk.
            ColorSerializer serializer = new ColorSerializer("colors");
            TaskResult<List<CubeColor>> result = new TaskResult<List<CubeColor>>();
            yield return serializer.LoadAllColors(result);
            foreach (var color in result.value)
            {
                CustomCubeHandler.RegisterCube(color);
            }
            
            _log.LogInfo($"{NAME} has loaded {result.value.Count} custom cube colours from files on disk.");
        }
    }
}