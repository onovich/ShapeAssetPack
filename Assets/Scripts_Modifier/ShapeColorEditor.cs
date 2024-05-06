#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ShapeColorEditor : EditorWindow {
    Texture2D selectedImage;
    Color dstColor = Color.white;
    bool overwriteOriginal = false;
 
    [MenuItem("Tools/Shape Color Editor")]
    public static void ShowWindow() {
        GetWindow<ShapeColorEditor>("Shape Color Editor");
    }

    private void OnGUI() {
        GUILayout.Label("Select Image", EditorStyles.boldLabel);
        selectedImage = (Texture2D)EditorGUILayout.ObjectField("Image", selectedImage, typeof(Texture2D), false);

        dstColor = EditorGUILayout.ColorField("Dst Color", dstColor);
        overwriteOriginal = EditorGUILayout.Toggle("Overwrite Original", overwriteOriginal);

        if (GUILayout.Button("Process Image") && selectedImage != null) {
            string path = AssetDatabase.GetAssetPath(selectedImage);
            bool originalIsReadable = SetTextureReadable(path, true); 
            Texture2D editedImage = EditImage(selectedImage, dstColor);
            SaveEditedImage(editedImage, path, overwriteOriginal);
            SetTextureReadable(path, originalIsReadable); 
        }
    }

    private Texture2D EditImage(Texture2D image, Color dstColor) {
        int width = image.width;
        int height = image.height;
        Texture2D dstImage = new Texture2D(width, height, TextureFormat.ARGB32, false);
        dstImage.filterMode = image.filterMode;

        // Copy The Original Image
        CopyImage(image, dstImage);

        // Replace Color
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (image.GetPixel(x, y).a > 0.1f) {
                    ReplacePixel(image, dstImage, x, y, dstColor);
                }
            }
        }

        dstImage.Apply();
        return dstImage;
    }

    private void CopyImage(Texture2D source, Texture2D destination) {
        int width = source.width;
        int height = source.height;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                destination.SetPixel(x, y, source.GetPixel(x, y));
            }
        }
    }

    private void ReplacePixel(Texture2D source, Texture2D destination, int x, int y, Color dstColor) {
        int width = source.width;
        int height = source.height;

        for (int i = 0; i <= width; i++) {
            for (int j = 0; j <= height; j++) {
                var srcPixel = source.GetPixel(i, j);
                if (srcPixel.a <= 0.01f ) {
                    continue;
                }
                dstColor.a = srcPixel.a;
                destination.SetPixel(i, j, dstColor);
            }
        }
    }

    private void SaveEditedImage(Texture2D image, string originalPath, bool overwrite) {
        byte[] bytes = image.EncodeToPNG();
        string directory = Path.GetDirectoryName(originalPath);
        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        string newFilePath = overwrite ? originalPath : Path.Combine(directory, $"{filenameWithoutExtension}_edited.png");

        File.WriteAllBytes(newFilePath, bytes);

        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(newFilePath, ImportAssetOptions.ForceUpdate);

        ApplyOriginalTextureSettings(newFilePath, originalPath);

        AssetDatabase.Refresh();
    }

    private void ApplyOriginalTextureSettings(string newFilePath, string originalPath) {
        var originalImporter = AssetImporter.GetAtPath(originalPath) as TextureImporter;
        var newImporter = AssetImporter.GetAtPath(newFilePath) as TextureImporter;
        if (originalImporter != null && newImporter != null) {
            newImporter.spritePixelsPerUnit = originalImporter.spritePixelsPerUnit;
            newImporter.maxTextureSize = originalImporter.maxTextureSize;
            newImporter.textureCompression = originalImporter.textureCompression;
            newImporter.filterMode = originalImporter.filterMode;
            newImporter.textureType = originalImporter.textureType;
            var settings = originalImporter.GetDefaultPlatformTextureSettings();
            newImporter.SetPlatformTextureSettings(settings);

            newImporter.SaveAndReimport();
        }
    }

    private bool SetTextureReadable(string assetPath, bool isReadable) {
        var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (textureImporter != null) {
            bool originalIsReadable = textureImporter.isReadable;
            if (textureImporter.isReadable != isReadable) {
                textureImporter.isReadable = isReadable;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
            return originalIsReadable;
        }
        return false;
    }
}
#endif