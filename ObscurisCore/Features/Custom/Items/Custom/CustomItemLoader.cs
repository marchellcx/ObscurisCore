using LabExtended.API.Custom.Items;

using LabExtended.Utilities;
using LabExtended.Extensions;
using LabExtended.Attributes;

namespace ObscurisCore.Features.Custom.Items.Custom;

/// <summary>
/// Loads custom items from the "items" folder.
/// </summary>
public static class CustomItemLoader
{
    private static void Initialize()
    {
        var dir = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "items");
        
        foreach (var type in typeof(CustomItemLoader).Assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;
            
            if (string.IsNullOrEmpty(type.Namespace) || !type.Namespace.StartsWith("ObscurisCore.Features.Items.Custom"))
                continue;
            
            if (type.HasAttribute<LoaderIgnoreAttribute>())
                continue;
            
            if (!typeof(CustomItem).IsAssignableFrom(type))
                continue;

            var name = type.Name;
            var path = Path.Combine(dir, name + ".yml");

            if (FileUtils.TryLoadYamlFile<CustomItem>(path, type, out var item))
            {
                item.Register();
            }
            else
            {
                if (Activator.CreateInstance(type) is CustomItem customItem)
                {
                    customItem.Register();

                    FileUtils.TrySaveYamlFile(path, customItem);
                }
            }
        }
    }
}
