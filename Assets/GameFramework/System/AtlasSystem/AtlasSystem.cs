using GameFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class AtlasSystem
{
    private static Dictionary<string, UnityEngine.U2D.SpriteAtlas> spriteAtlasDict = new Dictionary<string, UnityEngine.U2D.SpriteAtlas>();

    static AtlasSystem()
    {
        UnityEngine.U2D.SpriteAtlasManager.atlasRequested += RequestAtlas;
    }

    private static void RequestAtlas(string atlasName, Action<UnityEngine.U2D.SpriteAtlas> callback)
    {
        var atlas = AssetSystem.Load<UnityEngine.U2D.SpriteAtlas>(atlasName);
        callback(atlas);
    }

    public static Sprite GetSprite(string spriteName, string atlasName)
    {
        if (spriteAtlasDict.TryGetValue(atlasName, out var atlas) == false)
        {
            atlas = AssetSystem.Load<UnityEngine.U2D.SpriteAtlas>(atlasName);
            spriteAtlasDict.Add(atlasName, atlas);
        }

        Sprite sprite = atlas.GetSprite(spriteName);
        return sprite;
    }

    public static UnityEngine.U2D.SpriteAtlas GetSpriteAtlas(string atlasName)
    {
        if (spriteAtlasDict.TryGetValue(atlasName, out var atlas) == false)
        {
            atlas = AssetSystem.Load<UnityEngine.U2D.SpriteAtlas>(atlasName);
            spriteAtlasDict.Add(atlasName, atlas);
        }

        return atlas;
    }
}
