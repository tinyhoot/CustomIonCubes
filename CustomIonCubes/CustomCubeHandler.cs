using System;
using System.Collections.Generic;
using System.Reflection;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility.ModMessages;
using UnityEngine;

namespace CustomIonCubes
{
    public static class CustomCubeHandler
    {
        /// <summary>
        /// This prefix is automatically added before every id registered with this mod to avoid classId collisions with
        /// other mods.
        /// </summary>
        public const string IdPrefix = "cic_";

        /// <summary>
        /// The subject used by this mod when sending a global mod message to notify other mods of a newly registered
        /// custom ion cube.
        /// </summary>
        public const string ModMessageSubject = "CustomIonCube_Registered";

        private static readonly Dictionary<string, CubeColor> CubeColours = new Dictionary<string, CubeColor>();
        private static readonly int MainColor = Shader.PropertyToID("_Color");
        private static readonly int DetailsColor = Shader.PropertyToID("_DetailsColor");
        private static readonly int SquaresColor = Shader.PropertyToID("_SquaresColor");
        private static readonly int GlowColor = Shader.PropertyToID("_GlowColor");

        internal static readonly Dictionary<TechType, Material> Materials = new Dictionary<TechType, Material>();
        
        /// <summary>
        /// Register a custom <see cref="CubeColor"/> to create a new ion cube item with.<br />
        /// This method primarily exists for access using reflection, so that other mods can avoid a hard dependency.
        /// See <see cref="CubeColor"/> for an explanation of each argument.
        /// </summary>
        /// <returns>The TechType of the newly created item.</returns>
        /// <exception cref="ArgumentException">Raised if the color's id already exists.</exception>
        public static TechType RegisterCube(string id, Color main, Color details, Color squares, Color glow, Color light,
            Color icon)
        {
            var color = new CubeColor
            {
                Id = id,
                MainColor = main,
                Details = details,
                AnimatedSquares = squares,
                Glow = glow,
                Illumination = light,
                IconColor = icon
            };
            return RegisterCube(color);
        }

        /// <summary>
        /// Register a custom <see cref="CubeColor"/> to create a new ion cube item with.
        /// </summary>
        /// <returns>The TechType of the newly created item.</returns>
        /// <exception cref="ArgumentException">Raised if the color's id already exists.</exception>
        public static TechType RegisterCube(CubeColor color)
        {
            string classId = $"{IdPrefix}{color.Id}";
            if (CubeColours.ContainsKey(classId))
                throw new ArgumentException($"A custom cube with id '{color.Id}' already exists!");
            CubeColours.Add(classId, color);
            
            PrefabInfo prefabInfo = PrefabInfo
                .WithTechType(classId, false, Assembly.GetExecutingAssembly())
                .WithIcon(SpriteManager.Get(TechType.PrecursorIonCrystal))
                .WithSizeInInventory(new Vector2int(1, 1));
            CustomPrefab prefab = new CustomPrefab(prefabInfo);
            prefab
                .SetRecipe(new RecipeData(new CraftData.Ingredient(TechType.PrecursorIonCrystal)))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorsBasicMaterials);
            prefab
                .SetUnlock(TechType.PrecursorIonCrystal)
                .SetHardLocked(false)
                .WithPdaGroupCategory(TechGroup.Resources, TechCategory.BasicMaterials);
            
            prefab.SetGameObject(new CloneTemplate(prefabInfo, TechType.PrecursorIonCrystal));
            prefab.SetPrefabPostProcessor(PostProcessor);
            prefab.Register();
            
            // Also set up the recipe to turn this cube back into a regular ion cube.
            CraftDataHandler.SetRecipeData(TechType.PrecursorIonCrystal, new RecipeData(new CraftData.Ingredient(prefabInfo.TechType)));
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.PrecursorIonCrystal, CraftTreeHandler.Paths.FabricatorsBasicMaterials);
            CopyLanguageLines(prefabInfo.TechType);
            PrepareMaterial(prefabInfo.TechType, color.IconColor);
            
            // Notify any other mods out there with that a new custom cube exists.
            ModMessageSystem.SendGlobal(ModMessageSubject, prefabInfo.TechType);
            return prefabInfo.TechType;
        }

        /// <summary>
        /// Modify the material colours just before the cube object becomes active.
        /// </summary>
        private static void PostProcessor(GameObject cubeObject)
        {
            var identifier = cubeObject.GetComponent<PrefabIdentifier>();
            string classId = identifier.classId;
            CustomIonCubesInit._log.LogDebug($"Post processing cube '{classId}'.");
            if (!CubeColours.TryGetValue(classId, out var colors))
            {
                CustomIonCubesInit._log.LogError($"Got classId without color data: {classId}");
                return;
            }
            
            // Ensure we grab the material for both the mesh and the viewmodel.
            foreach (var render in cubeObject.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                // CustomIonCubesInit._log.LogDebug($"Modifying MeshRenderer on '{render.gameObject.name}'");
                Material material = render.material;
                material.SetColor(MainColor, colors.MainColor);
                material.SetColor(DetailsColor, colors.Details);
                material.SetColor(SquaresColor, colors.AnimatedSquares);
                material.SetColor(GlowColor, colors.Glow);
            }
            // Also modify the point light.
            var light = cubeObject.GetComponentInChildren<Light>();
            light.color = colors.Illumination;
        }

        /// <summary>
        /// Copy the text displayed in-game from the vanilla ion cube.
        /// </summary>
        private static void CopyLanguageLines(TechType techType)
        {
            string language = Language.main.currentLanguage;
            string name = Language.main.Get(TechType.PrecursorIonCrystal.AsString());
            string tooltip = Language.main.Get("Tooltip_" + TechType.PrecursorIonCrystal.AsString());
            LanguageHandler.SetTechTypeName(techType, name, language);
            LanguageHandler.SetTechTypeTooltip(techType, tooltip, language);
        }

        private static void PrepareMaterial(TechType techType, Color iconColor)
        {
            var material = UnityEngine.Object.Instantiate(CustomIonCubesInit._hueshift);
            Color.RGBToHSV(iconColor, out float h, out float s, out float v);
            CustomIonCubesInit._log.LogDebug($"Setting icon hue as: {h}");
            material.SetFloat("_Hue", h);
            // material.color = iconColor;
            Materials.Add(techType, material);
        }
    }
}