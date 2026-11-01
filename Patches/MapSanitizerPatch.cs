// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(CustomMapLoader), "SanitizeObject")]
    internal static class MapSanitizerPatch
    {
        private static readonly FieldInfo ApprovedLayersField = AccessTools.Field(typeof(CustomMapLoader), "APPROVED_LAYERS");
        private static readonly FieldInfo ComponentAllowlistField = AccessTools.Field(typeof(CustomMapLoader), "componentAllowlist");
        private static readonly FieldInfo ComponentTypeStringAllowlistField = AccessTools.Field(typeof(CustomMapLoader), "componentTypeStringAllowList");

        private static HashSet<int> approvedLayers;
        private static HashSet<Type> approvedComponentTypes;
        private static List<string> approvedTypeNameFragments;
        private static readonly Dictionary<Type, bool> TypeNameApprovalCache = new Dictionary<Type, bool>();

        private static bool Prefix(GameObject gameObject, GameObject mapRoot, ref bool __result)
        {
            if (!(Plugin.Instance?.OptimizeCustomMapSanitizer ?? false))
                return true;

            EnsureCaches();

            if (gameObject == null)
            {
                Debug.LogError("CustomMapLoader::SanitizeObject gameobject null");
                __result = false;
                return false;
            }

            if (!approvedLayers.Contains(gameObject.layer))
                gameObject.layer = 0;

            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    Debug.Log("CustomMapLoader::SanitizeObject null component has caused " + gameObject.name + " to be DELETED");
                    UnityEngine.Object.Destroy(gameObject);
                    __result = false;
                    return false;
                }

                Type componentType = component.GetType();
                bool isAllowed = approvedComponentTypes.Contains(componentType);
                if (isAllowed && componentType == typeof(Camera))
                {
                    Camera camera = (Camera)component;
                    if (camera != null && camera.targetTexture == null)
                        isAllowed = false;
                }

                if (!isAllowed)
                    isAllowed = IsTypeNameAllowed(componentType);

                if (!isAllowed)
                {
                    Debug.Log($"CustomMapLoader::SanitizeObject component type {componentType} has caused {gameObject.name} to be DELETED");
                    UnityEngine.Object.Destroy(gameObject);
                    __result = false;
                    return false;
                }
            }

            if (gameObject.transform.parent == null && gameObject.transform != mapRoot.transform)
                gameObject.transform.SetParent(mapRoot.transform);

            __result = true;
            return false;
        }

        private static void EnsureCaches()
        {
            if (approvedLayers != null)
                return;

            approvedLayers = new HashSet<int>((List<int>)ApprovedLayersField.GetValue(null));
            approvedComponentTypes = new HashSet<Type>((List<Type>)ComponentAllowlistField.GetValue(null));
            approvedTypeNameFragments = (List<string>)ComponentTypeStringAllowlistField.GetValue(null);
        }

        private static bool IsTypeNameAllowed(Type componentType)
        {
            if (TypeNameApprovalCache.TryGetValue(componentType, out bool isAllowed))
                return isAllowed;

            string typeName = componentType.ToString();
            for (int i = 0; i < approvedTypeNameFragments.Count; i++)
            {
                if (typeName.Contains(approvedTypeNameFragments[i]))
                {
                    TypeNameApprovalCache[componentType] = true;
                    return true;
                }
            }

            TypeNameApprovalCache[componentType] = false;
            return false;
        }
    }
}
