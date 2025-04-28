using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

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

        private void Awake()
        {
            _log = Logger;
            _log.LogInfo($"{NAME} v{VERSION} ready.");
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