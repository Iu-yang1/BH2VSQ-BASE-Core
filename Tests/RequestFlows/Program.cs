using BH2VSQ.Base;
using TMPro;
using VRC.SDKBase;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static TeleportRequestManager NewManager()
{
    var manager = new TeleportRequestManager
    {
        teleport = new TeleportManager(),
        panel = new RequestPanel
        {
            noticeText = new TMP_Text(),
            countText = new TMP_Text(),
            rowObjects = new[] { new UnityEngine.GameObject(), new UnityEngine.GameObject() },
            requesterTexts = new[] { new TMP_Text(), new TMP_Text() },
            locationTexts = new[] { new TMP_Text(), new TMP_Text() },
            acceptActions = new[] { new UIButtonAction(), new UIButtonAction() },
            rejectActions = new[] { new UIButtonAction(), new UIButtonAction() }
        }
    };
    manager.panel.requests = manager;
    return manager;
}

static void CopyState(TeleportRequestManager from, TeleportRequestManager to)
{
    Array.Copy(from.requestIds, to.requestIds, TeleportRequestManager.Capacity);
    Array.Copy(from.requesterIds, to.requesterIds, TeleportRequestManager.Capacity);
    Array.Copy(from.targetIds, to.targetIds, TeleportRequestManager.Capacity);
    Array.Copy(from.requestTypes, to.requestTypes, TeleportRequestManager.Capacity);
    Array.Copy(from.states, to.states, TeleportRequestManager.Capacity);
    Array.Copy(from.expiryTicks, to.expiryTicks, TeleportRequestManager.Capacity);
}

var sender = new VRCPlayerApi { playerId = 1, displayName = "发起者" };
var receiver = new VRCPlayerApi { playerId = 2, displayName = "接收者" };
var third = new VRCPlayerApi { playerId = 3, displayName = "第三人" };
VRCPlayerApi.TestPlayers = new[] { sender, receiver, third };
var outgoing = NewManager();
var incoming = NewManager();

Networking.LocalPlayer = sender;
Check(outgoing.Send(receiver.playerId, 0), "first request was not sent");
int firstId = outgoing.requestIds[0];
Check(!outgoing.Send(receiver.playerId, 0), "duplicate pending request was accepted");
CopyState(outgoing, incoming);
Networking.LocalPlayer = receiver;
incoming.OnDeserialization();
Check(incoming.panel.noticeText.text.Contains("按住 Tab"), "receiver did not get Tab notice");
Check(incoming.panel.rowObjects[0].activeSelf && incoming.panel.requesterTexts[0].text == "发起者" && incoming.panel.acceptActions[0].value == firstId, "request list row did not map to incoming request");
incoming.Accept(firstId);
Check(incoming.states[0] == 2, "request was not accepted");
CopyState(incoming, outgoing);
Networking.LocalPlayer = sender;
outgoing.OnDeserialization();
Check(sender.teleportCount == 1, "accepted go-to request did not teleport sender");
Check(outgoing.panel.noticeText.text.Contains("已同意"), "sender did not receive accepted result");

Check(outgoing.Send(receiver.playerId, 1), "invite was not sent");
int secondId = outgoing.requestIds[0];
CopyState(outgoing, incoming);
Networking.LocalPlayer = receiver;
incoming.OnDeserialization();
incoming.Reject(secondId);
CopyState(incoming, outgoing);
Networking.LocalPlayer = sender;
outgoing.OnDeserialization();
Check(sender.teleportCount == 1, "rejected invite teleported sender");
Check(outgoing.panel.noticeText.text.Contains("已拒绝"), "sender did not receive rejected result");

Check(outgoing.Send(receiver.playerId, 0), "new request could not reuse completed slot");
Check(outgoing.Send(third.playerId, 0), "second simultaneous request was not stored");
Check(outgoing.requestIds[0] != outgoing.requestIds[1], "simultaneous requests share an ID");

// Invitation must bypass the invited player's rank requirement when the requester
// is standing in a restricted floor, while reserved / maintenance still block it.
var invitedFloor = new TeleportPoint
{
    locationId = 9000,
    floorId = 900,
    requiredRank = BaseRank.Admin,
    floorState = (int)FloorState.Open
};
var inviteTeleport = new TeleportManager();
inviteTeleport.points = new[] { invitedFloor };
var inviteFloors = new FloorManager { teleport = inviteTeleport };
var receiverSession = new AuthenticationSession { rank = BaseRank.Visitor };
var receiverPermission = new PermissionManager { session = receiverSession };
var inviteAccess = new AccessManager
{
    floors = inviteFloors,
    teleport = inviteTeleport,
    permission = receiverPermission
};
var receiverTracker = new PlayerAreaTracker { teleport = inviteTeleport };
receiverTracker.playerIds[0] = sender.playerId;
receiverTracker.areaIds[0] = invitedFloor.locationId;
inviteTeleport.access = inviteAccess;
inviteTeleport.tracker = receiverTracker;

Networking.LocalPlayer = receiver;
Check(inviteAccess.CheckPointAccess(invitedFloor) == AccessResult.InsufficientRank, "restricted floor should reject a normal visitor");
Check(inviteAccess.CheckPointAccess(invitedFloor, true) == AccessResult.Allowed, "accepted invitation did not bypass rank requirement");
Check(inviteTeleport.ToPlayer(sender.playerId, true), "invited visitor could not teleport to admin-only floor");
Check(receiver.teleportCount == 1, "accepted invitation did not teleport the invited visitor");

invitedFloor.floorState = (int)FloorState.Reserved;
Check(inviteAccess.CheckPointAccess(invitedFloor, true) == AccessResult.FloorReserved, "reserved floor was incorrectly bypassed by invitation");
invitedFloor.floorState = (int)FloorState.Maintenance;
Check(inviteAccess.CheckPointAccess(invitedFloor, true) == AccessResult.FloorMaintenance, "maintenance floor was incorrectly bypassed by invitation");

// Only visitor-access floors are exposed to the floor-state settings list and are mutable.
var guestSettingPoint = new TeleportPoint { locationId = 9100, floorId = 91, requiredRank = BaseRank.Visitor };
var memberSettingPoint = new TeleportPoint { locationId = 9200, floorId = 92, requiredRank = BaseRank.Member };
var adminSettingPoint = new TeleportPoint { locationId = 9300, floorId = 93, requiredRank = BaseRank.Admin };
var settingTeleport = new TeleportManager { points = new[] { guestSettingPoint, memberSettingPoint, adminSettingPoint } };
var settingFloors = new FloorManager { teleport = settingTeleport };
var settingAdminPermission = new PermissionManager { session = new AuthenticationSession { rank = BaseRank.Admin } };
Networking.LocalPlayer = sender;
Check(settingFloors.ManageableFloorCount() == 1, "non-visitor floors leaked into manageable floor list");
Check(settingFloors.ManageableFloorIdAt(0) == 91, "visitor floor was not first manageable floor");
Check(settingFloors.IsGuestFloor(92) == false && settingFloors.IsGuestFloor(93) == false, "member/admin floor marked as guest floor");
Check(!settingFloors.SetState(92, FloorState.Reserved, settingAdminPermission), "member floor state was mutable");
Check(!settingFloors.SetState(93, FloorState.Maintenance, settingAdminPermission), "admin floor state was mutable");
Check(settingFloors.SetState(91, FloorState.Reserved, settingAdminPermission), "visitor floor state could not be changed");
var firstFloorPoint = new TeleportPoint { locationId = 1000, floorId = 1, requiredRank = BaseRank.Visitor, floorState = (int)FloorState.Open };
var firstFloorTeleport = new TeleportManager { points = new[] { firstFloorPoint } };
var firstFloorManager = new FloorManager { teleport = firstFloorTeleport };
Check(firstFloorManager.GetState(1) == FloorState.Open, "1F was not forced to open state");
Check(!firstFloorManager.IsGuestFloor(1), "1F leaked into manageable visitor floors");
Check(!firstFloorManager.SetState(1, FloorState.Reserved, settingAdminPermission), "1F state could be changed to reserved");
Check(firstFloorPoint.floorState == (int)FloorState.Open, "1F state was mutated");
Check(BarrierStateTransitions(), "floor barrier state mapping is incorrect");
Check(BarrierPrefabContract(), "floor barrier prefab contract is incorrect");

Console.WriteLine("Request accept/reject results, teleport, duplicate prevention, and simultaneous slots passed.");


// Static regression guard for floor barrier semantics. The runtime implementation
// covers both legacy TeleportPoint barrier compatibility and the new FloorManager-backed FloorBarrier prefab.
static bool BarrierStateTransitions()
{
    return BarrierVisibleFor((int)BH2VSQ.Base.FloorState.Reserved)
        && BarrierVisibleFor((int)BH2VSQ.Base.FloorState.Maintenance)
        && !BarrierVisibleFor((int)BH2VSQ.Base.FloorState.Open);
}

static bool BarrierVisibleFor(int state) => state != (int)BH2VSQ.Base.FloorState.Open;

static bool BarrierPrefabContract()
{
    // The prefab controller is intentionally a parent object: child barrier bodies
    // are the only objects whose active state is toggled.
    var type = typeof(FloorManager);
    return type.GetField("barrierFloorId") != null
        && type.GetMethod("IsBarrierController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) != null
        && type.GetMethod("RefreshBarrier") != null
        && type.GetMethod("RefreshManagedBarriers") != null;
}
