using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;
using ObscurisCore.Features;

namespace ObscurisCore.Commands;

/// <summary>
/// Represents a command for managing dynamic map locations in the application.
/// </summary>
[Command("location", "Manages dynamic map locations.")]
public class LocationCommand : CommandBase, IServerSideCommand
{
    [CommandOverload("list", "Lists all locations.", null)]
    private void List()
    {
        if (MapLocations.Locations.Count == 0)
        {
            Fail("No locations found.");
        }
        else
        {
            Ok(x =>
            {
                x.AppendLine($"Locations (&3{MapLocations.Locations.Count}&r):");

                foreach (var kvp in MapLocations.Locations)
                {
                    var available = MapLocations.TryFind(kvp.Key, out var position, out var rotation);

                    if (available)
                    {
                        x.AppendLine($"- &2{kvp.Key}&r: &3{position.ToPreciseString()}&r");
                    }
                    else
                    {
                        x.AppendLine($"- &1{kvp.Key}&r");
                    }
                }
            });
        }
    }

    [CommandOverload("current", "Shows the current location.", null)]
    private void Current()
    {
        if (Sender.Position.Room == null)
        {
            Fail("You are not in a room.");
        }
        else
        {
            Ok(x =>
            {
                var room = Sender.Position.Room;
                
                var roomPos = room.transform.position;
                var roomMainPos = room.MainCoords;
                
                var roomBoundsSize = room.WorldspaceBounds.size;
                var roomBoundsCenter = room.WorldspaceBounds.center;
                var roomBoundsExtents = room.WorldspaceBounds.extents;
                
                var position = Sender.Position.Position;
                var relative = room.transform.InverseTransformPoint(position);

                x.AppendLine($"&3Room&r:\n" +
                             $"- &1Name&r: &6{room.Name}&r\n" +
                             $"- &1Object:&r &6{room.name}&r\n" +
                             $"- &1Zone&r: &6{room.Zone}&r\n" +
                             $"- &1Shape&r: &6{room.Shape}&r\n" +
                             $"- &1Index&r: &6{room.GetComponentIndex()}&r\n" +
                             $"- &1Parent&r: &6{room.transform.parent?.name ?? "(null)"}&r\n" +
                             $"&3Position&r:\n" +
                             $"- &1Current&r: &6X&r=&3{position.x}&r &6Y&r=&3{position.y}&r &6Z&r=&3{position.z}&r\n" +
                             $"- &1Room&r: &6X&r=&3{roomPos.x}&r &6Y&r=&3{roomPos.y}&r &6Z&r=&3{roomPos.z}&r\n" +
                             $"- &1Room Main&r: &6X&r=&3{roomMainPos.x}&r &6Y&r=&3{roomMainPos.y}&r &6Z&r=&3{roomMainPos.z}&r\n" +
                             $"- &1Room Bounds Size&r: &6X&r=&3{roomBoundsSize.x}&r &6Y&r=&3{roomBoundsSize.y}&r &6Z&r=&3{roomBoundsSize.z}&r\n" +
                             $"- &1Room Bounds Center&r: &6X&r=&3{roomBoundsCenter.x}&r &6Y&r=&3{roomBoundsCenter.y}&r &6Z&r=&3{roomBoundsCenter.z}&r\n" +
                             $"- &1Room Bounds Extents&r: &6X&r=&3{roomBoundsExtents.x}&r &6Y&r=&3{roomBoundsExtents.y}&r &6Z&r=&3{roomBoundsExtents.z}&r\n" +
                             $"- &1Relative&r: &6X&r=&3{relative.x}&r &6Y&r=&3{relative.y}&r &6Z&r=&3{relative.z}&r");
            });
        }
    }

    [CommandOverload("angle", "Sets the angle of a location.", null)]
    private void Angle(
        [CommandParameter("Name", "Name of the location.")] string name, 
        [CommandParameter("Angle", "Angle to use when spawning objects at this location.")] float angle = 0f)
    {
        if (MapLocations.TryFind(name, out var info))
        {
            info.Angle = angle;

            MapLocations.Save();
            
            Ok($"Saved angle of location &1{name}&r.");
        }
        else
        {
            Fail($"Location &1{name}&r does not exist.");
        }
    }

    [CommandOverload("save", "Saves a location.", null)]
    private void Save(
        [CommandParameter("Name", "Name of the location.")] string name, 
        [CommandParameter("Angle", "Angle to use when spawning objects at this location.")] float angle = 0f)
    {
        if (Sender.Position.Room == null)
        {
            Fail("You are not in a room.");
        }
        else
        {
            MapLocations.Save(name, Sender, angle == 0f ? null : angle);
            
            Ok($"Saved location &1{name}&r.");
        }
    }

    [CommandOverload("remove", "Removes a location.", null)]
    private void Remove(
        [CommandParameter("Name", "Name of the location.")] string name)
    {
        if (!MapLocations.Remove(name))
        {
            Fail($"Location &1{name}&r does not exist.");
        }
        else
        {
            Ok($"Removed location &1{name}&r.");
        }
    }
}