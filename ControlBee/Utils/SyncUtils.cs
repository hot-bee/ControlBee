using ControlBee.Interfaces;
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
        var peek = SyncRequestsPeek(actor, grants, requests, grantName);
        if (!peek.approvable)
            return false;

        SyncRequestsApprove(actor, grants, requests, grantName, peek.requestIds!);
        return true;
    }

    private static Guid GetRequestGuid(Actor actor, IActor requestActor, string requestName)
    {
        if (actor == requestActor)
        {
            if (actor.GetStatus(requestName) is Guid guid)
                return guid;
        }
        else
        {
            if (actor.GetPeerStatusByActor(requestActor, requestName) is Guid guid)
                return guid;
        }
        return Guid.Empty;
    }

    public static (bool approvable, List<Guid>? requestIds) SyncRequestsPeek(
        Actor actor,
        Dict grants,
        RequestSource[] requests,
        string grantName
    )
    {
        if (requests.Length == 0)
            return (false, null);
        var guids = new List<Guid>();
        foreach (var request in requests)
        {
            var guid = GetRequestGuid(actor, request.Actor, request.RequestName);
            guids.Add(guid);

            if (grants.TryGetValue(request.Actor.Name, out var lastGrant))
            {
                var (lastRequestName, lastGuid) = (ValueTuple<string, Guid>)lastGrant!;
                if (GetRequestGuid(actor, request.Actor, lastRequestName).Equals(lastGuid))
                    return (false, null);
            }
        }

        if (guids.Any(x => x == Guid.Empty))
            return (false, null);

        var lastSet = grants.GetValueOrDefault(grantName) as HashSet<Guid> ?? [];
        var newSet = new HashSet<Guid>(guids);
        newSet.IntersectWith(lastSet);
        if (newSet.Count > 0)
            return (false, null);

        return (true, guids);
    }

    public static void SyncRequestsApprove(
        Actor actor,
        Dict grants,
        RequestSource[] requests,
        string grantName,
        List<Guid> requestIds
    )
    {
        grants[grantName] = new HashSet<Guid>(requestIds!);
        foreach (var request in requests)
        {
            var guid = GetRequestGuid(actor, request.Actor, request.RequestName);
            grants[request.Actor.Name] = (request.RequestName, guid);
        }
    }
}
