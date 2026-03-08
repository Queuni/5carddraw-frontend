using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceManager
{
    public static Sprite backSprite = Resources.Load<Sprite>("images/card_back/back2");
    public static Dictionary<string, Sprite> frontSpriteMap = new();
    public static List<Sprite> avatarSpriteList = new();
    public static Sprite cpuAvatarSprite = Resources.Load<Sprite>("images/avatar/cpu_avatar");

    public static void LoadAll()
    {
        LoadCardSprites();
        LoadAvatarSprites();
    }

    public static void LoadCardSprites()
    {
        Sprite[] frontSprites = Resources.LoadAll<Sprite>("images/card_front");
        if (frontSprites.Length != Constants.TOTAL_CARDS_NUM)
        {
            Debug.LogError($"⚠ Expected {Constants.TOTAL_CARDS_NUM} front cards, found {frontSprites.Length}");
            if (frontSprites.Length == 0)
            {
                Debug.LogError("❌ No front sprites found! Check Resources/images/card_front/");
                return;
            }
        }

        foreach (var sprite in frontSprites)
        {
            string[] parts = sprite.name.Split('_');
            if (parts.Length != Constants.CARD_NAME_PARTS)
            {
                Debug.LogError("⚠ Invalid card name: " + sprite.name);
                continue;
            }
            frontSpriteMap[sprite.name] = sprite;
        }
    }

    public static void LoadAvatarSprites()
    {
        Sprite[] avatarSprites = Resources.LoadAll<Sprite>("images/avatar");
        foreach (var sprite in avatarSprites)
        {
            if (sprite.name == "cpu_avatar")
            {
                continue;
            }
            avatarSpriteList.Add(sprite);
        }
    }
}
