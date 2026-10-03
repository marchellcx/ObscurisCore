using LabExtended.Extensions;

using ObscurisCore.Features.Loadouts.Items;

using PlayerRoles;

namespace ObscurisCore.Features.Loadouts;

/// <summary>
/// The config definition of a loadout.
/// </summary>
public class LoadoutDefinition
{
    /// <summary>
    /// Gets or sets the name of the loadout.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the loadout's starting health.
    /// </summary>
    public float? StartHealth { get; set; }

    /// <summary>
    /// Gets or sets the loadout's maximum health.
    /// </summary>
    public float? MaxHealth { get; set; }

    /// <summary>
    /// Gets or sets the role associated with the loadout.
    /// </summary>
    public RoleTypeId? Role { get; set; }

    /// <summary>
    /// Gets or sets a list of ammo.
    /// </summary>
    public List<LoadoutAmmo> Ammo { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of items.
    /// </summary>
    public List<LoadoutItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets a custom dictionary of properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Sets the name property.
    /// </summary>
    public LoadoutDefinition WithName(string name)
    {
        Name = name;
        return this;
    }

    /// <summary>
    /// Adds a custom property.
    /// </summary>
    public LoadoutDefinition WithProperty(string key, string value)
    {
        Properties[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="StartHealth"/> and <see cref="MaxHealth"/> properties.
    /// </summary>
    public LoadoutDefinition WithHealth(float? startingHealth, float? maxHealth)
    {
        StartHealth = startingHealth;
        MaxHealth = maxHealth;

        return this;
    }

    /// <summary>
    /// Adds vanilla items.
    /// </summary>
    public LoadoutDefinition WithItems(params ItemType[] items)
    {
        for (var x = 0; x < items.Length; x++)
        {
            var type = items[x];

            if (type is ItemType.None)
                continue;

            if (type.IsAmmo())
                continue;

            Items.Add(new()
            {
                BaseType = type
            });
        }

        return this;
    }

    /// <summary>
    /// Adds an item.
    /// </summary>
    public LoadoutDefinition WithItem(ItemType type)
    {
        if (type != ItemType.None && !type.IsAmmo())
        {
            Items.Add(new()
            {
                BaseType = type
            });
        }

        return this;
    }

    /// <summary>
    /// Adds a custom item.
    /// </summary>
    public LoadoutDefinition WithCustomItem(string itemId)
    {
        Items.Add(new()
        {
            CustomType = itemId,
        });

        return this;
    }

    /// <summary>
    /// Adds vanilla ammo.
    /// </summary>
    public LoadoutDefinition WithAmmo(ItemType ammoType, ushort amount)
    {
        if (ammoType.IsAmmo())
        {
            Ammo.Add(new()
            {
                BaseType = ammoType,
                Amount = amount
            });
        }

        return this;
    }

    /// <summary>
    /// Adds custom ammo.
    /// </summary>
    public LoadoutDefinition WithCustomAmmo(string ammoId, ushort amount)
    {
        Ammo.Add(new()
        {
            CustomType = ammoId,
            Amount = amount
        });

        return this;
    }
}