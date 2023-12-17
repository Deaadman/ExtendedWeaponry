﻿using ExtendedWeaponry.Utilities;
using Il2CppInterop.Runtime.Attributes;

namespace ExtendedWeaponry.Components;

[RegisterTypeInIl2Cpp(false)]
public class AmmoManager : MonoBehaviour
{
    internal struct BulletInfo
    {
        public BulletType m_BulletType;
    }

    internal List<BulletInfo> m_Clip = [];

    internal void AddRoundsToClip(BulletType bulletType)
    {
        GunItem gunItem = GetComponent<GunItem>();
        if (gunItem != null && m_Clip.Count < gunItem.m_ClipSize)
        {
            m_Clip.Add(new BulletInfo { m_BulletType = bulletType });
        }
    }

    internal static Color GetColorForBulletType(BulletType bulletType)
    {
        return bulletType switch
        {
            BulletType.ArmorPiercing => Color.green,
            BulletType.Standard => Color.yellow,
            BulletType.Unspecified => Color.white,                          // Could use this for the Flare Gun items instead, would just need a way to get the game to run reload normally if the BulletType is Unspecified.
            _ => Color.white,
        };
    }

    internal int GetLoadedBulletCount()
    {
        return m_Clip.Count;
    }

    internal static Material? GetMaterialForBulletType(BulletType bulletType)
    {
        string? prefabName = GetPrefabNameForBulletType(bulletType);
        if (prefabName == null) return null;

        Material[]? materials = TextureSwapper.GetMaterialsFromGearItemPrefab(prefabName);
        if (materials != null && materials.Length > 0)
        {
            return materials[0];
        }
        else
        {
            return null;
        }
    }

    internal BulletType GetNextBulletType()
    {
        Inventory inventory = GameManager.GetInventoryComponent();
        if (inventory == null)
        {
            Logging.LogError("Inventory component not found.");
            return BulletType.Unspecified;
        }

        GunItem gunItem = GetComponent<GunItem>();
        if (gunItem == null)
        {
            Logging.LogError("GunItem component not found on AmmoManager.");
            return BulletType.Unspecified;
        }

        Il2CppSystem.Collections.Generic.List<GearItem> ammoItems = new();
        foreach (var gearItem in inventory.m_Items)
        {
            if (IsValidAmmo(gearItem, gunItem.m_GearItem))
            {
                ammoItems.Add(gearItem);
            }
        }

        foreach (var gearItem in ammoItems)
        {
            AmmoItemExtension ammoExtension = gearItem.gameObject.GetComponent<AmmoItemExtension>();
            if (ammoExtension != null)
            {
                Logging.Log($"Valid ammo found: {gearItem.name} with BulletType = {ammoExtension.m_BulletType}");
                return ammoExtension.m_BulletType;
            }
        }

        return BulletType.Unspecified;
    }

    private static string? GetPrefabNameForBulletType(BulletType bulletType)
    {
        return bulletType switch
        {
            BulletType.ArmorPiercing => "GEAR_RifleAmmoSingleAP",
            BulletType.Standard => "GEAR_RifleAmmoSingle",
            _ => null,
        };
    }

    internal static bool IsValidAmmo(GearItem gearItem, GearItem weapon)
    {
        bool isValid = gearItem != null && gearItem.m_AmmoItem && gearItem.m_StackableItem && gearItem.GetRoundedCondition() != 0 && gearItem.m_AmmoItem.m_AmmoForGunType == weapon.m_GunItem.m_GunType;
        return isValid;
    }

    [HideFromIl2Cpp]
    internal static void PrioritizeBulletType(GearItem gearItem, Dictionary<BulletType, int> bulletTypeCounts)
    {
        AmmoItemExtension ammoExtension = gearItem.GetComponent<AmmoItemExtension>();
        if (ammoExtension != null)
        {
            BulletType bulletType = ammoExtension.m_BulletType;
            if (!bulletTypeCounts.ContainsKey(bulletType))
            {
                bulletTypeCounts[bulletType] = 0;
            }
            bulletTypeCounts[bulletType] += gearItem.m_StackableItem.m_Units;
        }
    }

    [HideFromIl2Cpp]
    internal bool RemoveNextFromClip(out BulletInfo bulletInfo)
    {
        if (m_Clip.Count > 0)
        {
            bulletInfo = m_Clip[0];
            m_Clip.RemoveAt(0);
            return true;
        }
        else
        {
            bulletInfo = new BulletInfo();
            return false;
        }
    }

    internal static void UpdateBulletMaterials(Transform meshesTransform, AmmoManager ammoManager, Material nextBulletMaterial)
    {
        int loadedBullets = ammoManager.GetLoadedBulletCount();
        if (loadedBullets == 1)
        {
            TextureSwapper.SwapMaterial(meshesTransform, "mesh_bullet_a", nextBulletMaterial);
        }
        else if (loadedBullets > 1)
        {
            TextureSwapper.SwapMaterial(meshesTransform, "mesh_bullet_b", nextBulletMaterial);
        }

        TextureSwapper.SwapMaterial(meshesTransform, "mesh_StripperClipBullets", nextBulletMaterial);
    }
}