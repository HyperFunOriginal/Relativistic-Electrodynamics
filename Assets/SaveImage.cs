using System.IO;
using UnityEngine;

public class SaveImage
{
    public static void SaveImageToFile(RenderTexture img, string directory, string filename)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        Texture2D temp = ReturnImg(img);
        File.WriteAllBytes(directory + filename + ".png", temp.EncodeToPNG());
        Object.DestroyImmediate(temp, true);
    }
    static Texture2D ReturnImg(RenderTexture rt)
    {
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D outputImage = new Texture2D(rt.width, rt.height, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
        outputImage.ReadPixels(new Rect(0.0f, 0.0f, rt.width, rt.height), 0, 0);
        outputImage.Apply();
        RenderTexture.active = active;
        return outputImage;
    }
}
