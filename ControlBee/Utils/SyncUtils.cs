using ControlBee.Models;

namespace ControlBee.Utils;

using Dict = Dictionary<string, object?>;

public class SyncUtils
{
    public static bool SyncRequestsCheck(
        Actor actor,
        Dict grants,
        RequestSource[] requests,
        string grantName
    )
    {
        var guids = new List<Guid>();
        foreach (var request in requests)
        {
            var guid = GuidUtils.FromObject(
                actor.GetPeerStatusByActor(request.Actor, request.RequestName)
            );
            guids.Add(guid);

            if (grants.TryGetValue(request.Actor.Name, out var lastGrant))
            {
                var (lastRequestName, lastGuid) = (ValueTuple<string, Guid>)lastGrant!;
                if (
                    actor.GetPeerStatusByActor(request.Actor, lastRequestName)?.Equals(lastGuid)
                    is true
                )
                    return false;
            }
        }

        if (guids.Any(x => x == Guid.Empty))
            return false;

        var lastSet = grants.GetValueOrDefault(grantName) as HashSet<Guid> ?? [];
        var newSet = new HashSet<Guid>(guids);
        newSet.IntersectWith(lastSet);
        if (newSet.Count > 0)
            return false;

        grants[grantName] = new HashSet<Guid>(guids);
        foreach (var request in requests)
        {
            var guid = GuidUtils.FromObject(
                actor.GetPeerStatusByActor(request.Actor, request.RequestName)
            );
            grants[request.Actor.Name] = (request.RequestName, guid);
        }

        return true;
    }
}
