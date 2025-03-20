using UnityEngine;
using System.IO;

public class MaterialGenerator : MonoBehaviour
{
    public string stoneTextureName = "stone_texture"; // Nom du fichier dans Assets
    public string waterTextureName = "water_texture"; // Nom du fichier dans Assets

    void Start()
    {
        CreateMaterials();
    }

    void CreateMaterials()
    {
        // Charger la texture de pierre
        Texture2D stoneTexture = LoadTexture(stoneTextureName);
        Material stoneMaterial = new Material(Shader.Find("Standard"));
        if (stoneTexture != null)
        {
            stoneMaterial.mainTexture = stoneTexture;
            stoneMaterial.SetTextureScale("_MainTex", new Vector2(5, 5)); // Ajuste la répétition de la texture
            stoneMaterial.SetFloat("_Glossiness", 0.2f); // Réduit la brillance
        }
        stoneMaterial.color = Color.gray;
        SaveMaterial(stoneMaterial, "StoneMaterial");

        // Charger la texture d’eau
        Texture2D waterTexture = LoadTexture(waterTextureName);
        Material waterMaterial = new Material(Shader.Find("Standard"));
        if (waterTexture != null)
        {
            waterMaterial.mainTexture = waterTexture;
            waterMaterial.SetFloat("_Glossiness", 0.9f); // Rend l'eau brillante
            waterMaterial.color = new Color(0, 0.3f, 0.8f);
        }
        SaveMaterial(waterMaterial, "WaterMaterial");

        // Appliquer les matériaux aux objets
        ApplyMaterialToObject("Ground", stoneMaterial);
        ApplyMaterialToObject("Water", waterMaterial);

        Debug.Log("✅ Matériaux créés et appliqués !");
    }

    Texture2D LoadTexture(string fileName)
    {
        Texture2D texture = Resources.Load<Texture2D>(fileName);
        if (texture == null)
        {
            Debug.LogWarning("⚠️ Texture " + fileName + " non trouvée dans Resources !");
        }
        return texture;
    }

    void SaveMaterial(Material material, string materialName)
    {
        string path = "Assets/GeneratedMaterials/" + materialName + ".mat";
        if (!Directory.Exists("Assets/GeneratedMaterials"))
        {
            Directory.CreateDirectory("Assets/GeneratedMaterials");
        }
        UnityEditor.AssetDatabase.CreateAsset(material, path);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("💾 Matériau sauvegardé : " + materialName);
    }

    void ApplyMaterialToObject(string objectName, Material material)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null && obj.GetComponent<Renderer>() != null)
        {
            obj.GetComponent<Renderer>().material = material;
            Debug.Log("🎨 Matériau appliqué à : " + objectName);
        }
        else
        {
            Debug.LogWarning("⚠️ Objet " + objectName + " non trouvé !");
        }
    }
}
