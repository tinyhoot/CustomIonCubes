using System;
using System.Collections.Generic;
using System.Reflection;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;
using UnityEngine.U2D;

namespace CustomIonCubes
{
    public class CustomCubeHandler
    {
        /// <summary>
        /// This prefix is automatically added before every id registered with this mod to avoid classId collisions with
        /// other mods.
        /// </summary>
        public const string IdPrefix = "cic_";

        private static readonly Dictionary<string, CubeColor> CubeColours = new Dictionary<string, CubeColor>();
        private static readonly int MainColor = Shader.PropertyToID("_Color");
        private static readonly int DetailsColor = Shader.PropertyToID("_DetailsColor");
        private static readonly int SquaresColor = Shader.PropertyToID("_SquaresColor");
        private static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
        
        /// <summary>
        /// Register a custom <see cref="CubeColor"/> to create a new ion cube item with.<br />
        /// This method primarily exists for access using reflection, so that other mods can avoid a hard dependency.
        /// See <see cref="CubeColor"/> for an explanation of each argument.
        /// </summary>
        /// <returns>The TechType of the newly created item.</returns>
        /// <exception cref="ArgumentException">Raised if the color's id already exists.</exception>
        public static TechType RegisterCube(string id, Color main, Color details, Color squares, Color glow, Color light)
        {
            var color = new CubeColor
            {
                Id = id,
                MainColor = main,
                Details = details,
                AnimatedSquares = squares,
                Glow = glow,
                Illumination = light
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
                .WithIcon(GetColouredSprite(color.MainColor))
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
            
            CopyLanguageLines(prefabInfo.TechType);
            return prefabInfo.TechType;
        }

        private static Sprite GetColouredSprite(Color newColor)
        {
            Atlas atlas = Atlas.GetAtlas("Items");
            foreach (var serial in atlas.serialData)
            {
                CustomIonCubesInit._log.LogDebug(serial.name);
            }
            var y = atlas.serialData.Find(data => data.name.ToLower().Equals("precursorioncrystal"));
            CustomIonCubesInit._log.LogDebug($"Found serialdata '{y.name}' with {y.sprite}");
            
            var original = SpriteManager.Get(TechType.PrecursorIonCrystal);
            foreach (var uv in original.uv0)
            {
                CustomIonCubesInit._log.LogDebug($"UV: {uv}");
            }
            foreach (var vtx in original.vertices)
            {
                CustomIonCubesInit._log.LogDebug($"Vertex: {vtx}");
            }
            // foreach (var x in Atlas.nameToAtlas.Keys)
            // {
            //     CustomIonCubesInit._log.LogDebug(x);
            // }
            
            var x1 = RecolorSprite(SpriteManager.Get(TechType.PrecursorIonCrystal), newColor);
            // x1.border = original.border;
            // x1.size = original.size;
            // x1.inner = original.inner;
            // x1.outer = original.outer;
            // x1.triangles = original.triangles;
            // x1.uv0 = original.uv0;
            // x1.vertices = original.vertices;
            // x1.pixelsPerUnit = original.pixelsPerUnit;
            // x1.uv0 = new[] { new Vector2(0f, 0f), new Vector2(1f, 1f) };
            return x1;
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
        
        /// <summary>
        /// Change the hue of all colors of a sprite. Saturation and vibrancy are retained.
        /// </summary>
        public static Sprite RecolorSprite(Atlas.Sprite sprite, Color newColor)
        {
            Texture2D texture = CloneTexture(sprite);
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color oldColor = texture.GetPixel(x, y);
                    var swapped = SwapColor(oldColor, newColor);
                    texture.SetPixel(x, y, swapped);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// Subnautica packages its textures as non-readable, which makes it almost impossible to modify. This method
        /// produces an editable copy. Graciously provided by Nitrox.
        /// </summary>
        public static Texture2D CloneTexture(Atlas.Sprite sprite)
        {
            Texture2D sourceTexture = sprite.texture;
            
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                (int)sprite.size.x,
                (int)sprite.size.y,
                // sprite.texture.width,
                // sprite.texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            
            // Blit the pixels on texture to the RenderTexture
            var scale = sprite.uv0[1] - sprite.uv0[2];
            Graphics.Blit(sourceTexture, tmp, scale, sprite.uv0[2]);
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D clonedTexture = new Texture2D((int)sprite.size.x, (int)sprite.size.y);
            // Copy the pixels from the RenderTexture to the new Texture
            clonedTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            clonedTexture.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previous;
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            return clonedTexture;
            // "clonedTexture" now has the same pixels from "texture" and it's readable.
        }

        /// <summary>
        /// Swaps in a new color while retaining the general "feel" of the old one by changing hue but retaining
        /// saturation and vibrancy.
        /// </summary>
        public static Color SwapColor(Color oldColor, Color newColor)
        {
            Color.RGBToHSV(oldColor, out _, out float s, out float v);
            Color.RGBToHSV(newColor, out float replacementHue, out _, out _);
            return Color.HSVToRGB(replacementHue, s, v).WithAlpha(oldColor.a);
        }
    }
}