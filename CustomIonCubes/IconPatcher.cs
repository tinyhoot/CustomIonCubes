using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CustomIonCubes
{
    [HarmonyPatch]
    internal static class IconPatcher
    {
        private static void ReplaceMaterial(uGUI_ItemIcon icon, TechType techType)
        {
            if (!CustomCubeHandler.Materials.TryGetValue(techType, out Material material))
            {
                // CustomIonCubesInit._log.LogDebug($"No custom material for {techType.AsString()}");
                return;
            }

            icon.foreground.material = material;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDragManager), nameof(ItemDragManager.InternalDragStart))]
        private static void PatchItemDragging(ItemDragManager __instance, bool __result, InventoryItem item)
        {
            // If the item was not allowed to be dragged, do nothing.
            if (!__result)
                return;
            ReplaceMaterial(__instance.draggedIcon, item.techType);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_BlueprintEntry), nameof(uGUI_BlueprintEntry.SetIcon))]
        private static void PatchBlueprintIcon(uGUI_BlueprintEntry __instance, TechType techType)
        {
            ReplaceMaterial(__instance.icon, techType);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_CraftingMenu), nameof(uGUI_CraftingMenu.CreateIcon))]
        private static void PatchCraftingMenu(uGUI_CraftingMenu __instance, uGUI_CraftingMenu.Node node)
        {
            ReplaceMaterial(node.icon, node.techType);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_EquipmentSlot), nameof(uGUI_EquipmentSlot.SetItem))]
        private static void PatchEquipmentSlot(uGUI_EquipmentSlot __instance, InventoryItem item)
        {
            ReplaceMaterial(__instance.icon, item.techType);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_ItemsContainer), nameof(uGUI_ItemsContainer.OnAddItem))]
        private static void PatchInventoryContainer(uGUI_ItemsContainer __instance, InventoryItem item)
        {
            ReplaceMaterial(__instance.items[item], item.techType);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_ItemsContainerView), nameof(uGUI_ItemsContainerView.OnAddItem))]
        private static void PatchInventoryContainerView(uGUI_ItemsContainerView __instance, InventoryItem item)
        {
            ReplaceMaterial(__instance.items[item], item.techType);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(uGUI_ItemSelector), nameof(uGUI_ItemSelector.CreateIcons))]
        private static IEnumerable<CodeInstruction> TranspileItemSelector(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // Match to the 'i++' instruction of the first loop, where all icons for all items are created.
            // That is effectively the end of that loop, where we can insert our own function call safely.
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Stloc_3)
            );
            if (matcher.IsInvalid)
            {
                CustomIonCubesInit._log.LogError("Failed to transpile ItemSelector! Selecting an item like " +
                                                 "replacing batteries in tools will not look right.");
                return matcher.InstructionEnumeration();
            }

            matcher.Insert(
                // Load the ItemIcon.
                new CodeInstruction(OpCodes.Ldloc_0),
                // Load the TechType.
                new CodeInstruction(OpCodes.Ldloc, 6),
                // Send the data to our material-replacing function.
                CodeInstruction.Call(typeof(IconPatcher), nameof(ReplaceMaterial))
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_RecipeEntry), nameof(uGUI_RecipeEntry.Initialize))]
        private static void PatchRecipeEntry(uGUI_RecipeEntry __instance, TechType techType)
        {
            ReplaceMaterial(__instance.icon, techType);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_RecipeItem), nameof(uGUI_RecipeItem.Set))]
        private static void PatchRecipeItem(uGUI_RecipeItem __instance, TechType techType)
        {
            ReplaceMaterial(__instance.icon, techType);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(uGUI_IconNotifier), nameof(uGUI_IconNotifier.Update))]
        private static IEnumerable<CodeInstruction> TranspileIconNotifier(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Callvirt,
                    AccessTools.Method(typeof(uGUI_ItemIcon), nameof(uGUI_ItemIcon.SetForegroundSprite))));
            if (matcher.IsInvalid)
            {
                CustomIonCubesInit._log.LogError("Failed to transpile IconNotifier! Animation adding items to " +
                                                 "inventory will not look right.");
                return matcher.InstructionEnumeration();
            }

            // Skip to after the instruction we just matched to.
            matcher.Advance(1);
            matcher.Insert(
                // Load the ItemIcon.
                new CodeInstruction(OpCodes.Ldloc_1),
                // Load the ItemRequest.
                new CodeInstruction(OpCodes.Ldloc_0),
                // Grab the TechType from the request.
                CodeInstruction.LoadField(typeof(uGUI_IconNotifier.Request), nameof(uGUI_IconNotifier.Request.techType)),
                // Send the data to our material-replacing function.
                CodeInstruction.Call(typeof(IconPatcher), nameof(ReplaceMaterial))
            );

            return matcher.InstructionEnumeration();
        }
    }
}