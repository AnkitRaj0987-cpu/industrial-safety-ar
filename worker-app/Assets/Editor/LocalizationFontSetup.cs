using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace IndustrialSafetyAR.Editor
{
    [InitializeOnLoad]
    public static class LocalizationFontSetup
    {
        static LocalizationFontSetup()
        {
            EditorApplication.delayCall += EnsureFontsConfigured;
        }

        [MenuItem("Industrial Safety AR/Configure Localization Fallback Fonts")]
        public static void EnsureFontsConfigured()
        {
            try
            {
                SetupFonts();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationFontSetup] Failed: {ex}");
            }
        }

        public static void SetupFonts()
        {
            string resourcesFontsDir = "Assets/TextMesh Pro/Resources/Fonts & Materials";
            if (!Directory.Exists(resourcesFontsDir))
            {
                Directory.CreateDirectory(resourcesFontsDir);
            }

            // 1. Devanagari (Hindi)
            string devaTtfPath = "Assets/TextMesh Pro/Fonts/NotoSansDevanagari-Regular.ttf";
            string devaAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSansDevanagari SDF.asset";
            var devaFont = AssetDatabase.LoadAssetAtPath<Font>(devaTtfPath);
            TMP_FontAsset devaAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(devaAssetPath);

            if (devaAsset == null && devaFont != null)
            {
                devaAsset = TMP_FontAsset.CreateFontAsset(devaFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                if (devaAsset != null)
                {
                    devaAsset.name = "NotoSansDevanagari SDF";
                    AssetDatabase.CreateAsset(devaAsset, devaAssetPath);
                    if (devaAsset.material != null)
                    {
                        devaAsset.material.name = "NotoSansDevanagari SDF Material";
                        AssetDatabase.AddObjectToAsset(devaAsset.material, devaAsset);
                    }
                    if (devaAsset.atlasTextures != null)
                    {
                        foreach (var tex in devaAsset.atlasTextures)
                        {
                            if (tex != null)
                            {
                                tex.name = "NotoSansDevanagari SDF Atlas";
                                AssetDatabase.AddObjectToAsset(tex, devaAsset);
                            }
                        }
                    }
                    AssetDatabase.SaveAssets();
                    Debug.Log("[LocalizationFontSetup] Created NotoSansDevanagari SDF asset.");
                }
            }

            // 2. Ol Chiki (Santali)
            string olChikiTtfPath = "Assets/TextMesh Pro/Fonts/NotoSansOlChiki-Regular.ttf";
            string olChikiAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSansOlChiki SDF.asset";
            var olChikiFont = AssetDatabase.LoadAssetAtPath<Font>(olChikiTtfPath);
            TMP_FontAsset olChikiAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(olChikiAssetPath);

            if (olChikiAsset == null && olChikiFont != null)
            {
                olChikiAsset = TMP_FontAsset.CreateFontAsset(olChikiFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                if (olChikiAsset != null)
                {
                    olChikiAsset.name = "NotoSansOlChiki SDF";
                    AssetDatabase.CreateAsset(olChikiAsset, olChikiAssetPath);
                    if (olChikiAsset.material != null)
                    {
                        olChikiAsset.material.name = "NotoSansOlChiki SDF Material";
                        AssetDatabase.AddObjectToAsset(olChikiAsset.material, olChikiAsset);
                    }
                    if (olChikiAsset.atlasTextures != null)
                    {
                        foreach (var tex in olChikiAsset.atlasTextures)
                        {
                            if (tex != null)
                            {
                                tex.name = "NotoSansOlChiki SDF Atlas";
                                AssetDatabase.AddObjectToAsset(tex, olChikiAsset);
                            }
                        }
                    }
                    AssetDatabase.SaveAssets();
                    Debug.Log("[LocalizationFontSetup] Created NotoSansOlChiki SDF asset.");
                }
            }

            // 3. Fallback configuration on LiberationSans SDF
            string liberationPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(liberationPath);
            if (liberation != null)
            {
                SerializedObject so = new SerializedObject(liberation);
                so.Update();
                SerializedProperty prop = so.FindProperty("m_FallbackFontAssetTable") ?? so.FindProperty("fallbackFontAssets");
                if (prop != null && prop.isArray)
                {
                    bool hasDeva = false;
                    bool hasOlChiki = false;
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var elem = prop.GetArrayElementAtIndex(i);
                        if (elem.objectReferenceValue == devaAsset) hasDeva = true;
                        if (elem.objectReferenceValue == olChikiAsset) hasOlChiki = true;
                    }
                    if (!hasDeva && devaAsset != null)
                    {
                        prop.InsertArrayElementAtIndex(prop.arraySize);
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = devaAsset;
                    }
                    if (!hasOlChiki && olChikiAsset != null)
                    {
                        prop.InsertArrayElementAtIndex(prop.arraySize);
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = olChikiAsset;
                    }
                    so.ApplyModifiedProperties();
                    Debug.Log("[LocalizationFontSetup] SerializedObject updated LiberationSans SDF fallback table.");
                }

                if (liberation.fallbackFontAssetTable == null)
                {
                    liberation.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }
                if (devaAsset != null && !liberation.fallbackFontAssetTable.Contains(devaAsset))
                {
                    liberation.fallbackFontAssetTable.Add(devaAsset);
                }
                if (olChikiAsset != null && !liberation.fallbackFontAssetTable.Contains(olChikiAsset))
                {
                    liberation.fallbackFontAssetTable.Add(olChikiAsset);
                }

                EditorUtility.SetDirty(liberation);
            }

            // 4. Fallback configuration on TMP Settings
            if (TMP_Settings.instance != null)
            {
                var settings = TMP_Settings.instance;
                var fallbacks = TMP_Settings.fallbackFontAssets;
                if (fallbacks == null)
                {
                    fallbacks = new List<TMP_FontAsset>();
                    TMP_Settings.fallbackFontAssets = fallbacks;
                }

                bool modified = false;
                if (devaAsset != null && !fallbacks.Contains(devaAsset))
                {
                    fallbacks.Add(devaAsset);
                    modified = true;
                }
                if (olChikiAsset != null && !fallbacks.Contains(olChikiAsset))
                {
                    fallbacks.Add(olChikiAsset);
                    modified = true;
                }

                if (modified)
                {
                    EditorUtility.SetDirty(settings);
                    Debug.Log("[LocalizationFontSetup] Updated TMP Settings fallback font assets.");
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}
