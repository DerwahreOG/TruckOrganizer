using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Thin wrapper around PhotonNetwork.RaiseEvent that also works in
    /// singleplayer by dispatching the message locally.
    /// </summary>
    public static class NetworkEvents
    {
        // Single custom event code for the whole mod; the opcode is the first
        // element of the payload. 199 is the highest code available to games.
        private const byte EventCode = 174;

        private enum Op : byte
        {
            SpawnChest = 1,
            SnapshotPush = 2,
            SnapshotRequest = 3,
            TakeRequest = 4,
            UseRequest = 5,
            EquipHint = 6,
        }

        private sealed class Handler : IOnEventCallback
        {
            public void OnEvent(EventData photonEvent)
            {
                if (photonEvent.Code != EventCode) return;
                try
                {
                    Dispatch((object[])photonEvent.CustomData);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to handle network event: {e}");
                }
            }
        }

        private static Handler _handler;

        public static void Initialize()
        {
            _handler = new Handler();
            PhotonNetwork.AddCallbackTarget(_handler);
        }

        public static void Shutdown()
        {
            if (_handler != null) PhotonNetwork.RemoveCallbackTarget(_handler);
            _handler = null;
        }

        // ------------------------------------------------------------------
        // Senders
        // ------------------------------------------------------------------

        public static void BroadcastChestSpawn(Vector3 position, Quaternion rotation)
        {
            Broadcast(new object[] { (byte)Op.SpawnChest, position, rotation });
        }

        public static void BroadcastSnapshot(string payload)
        {
            Broadcast(new object[] { (byte)Op.SnapshotPush, payload });
        }

        public static void BroadcastEquipHint(string steamId, int photonViewId)
        {
            Broadcast(new object[] { (byte)Op.EquipHint, steamId, photonViewId });
        }

        public static void SendTakeRequest(string itemName, string steamId)
        {
            SendToHost(new object[] { (byte)Op.TakeRequest, itemName, steamId });
        }

        public static void SendUseRequest(string itemName, string steamId)
        {
            SendToHost(new object[] { (byte)Op.UseRequest, itemName, steamId });
        }

        public static void SendSnapshotRequest()
        {
            SendToHost(new object[] { (byte)Op.SnapshotRequest });
        }

        // ------------------------------------------------------------------
        // Transport
        // ------------------------------------------------------------------

        private static void Broadcast(object[] payload)
        {
            // Handle locally right away, then inform everyone else.
            Dispatch(payload);
            if (SemiFunc.IsMultiplayer())
            {
                PhotonNetwork.RaiseEvent(EventCode, payload,
                    new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                    SendOptions.SendReliable);
            }
        }

        private static void SendToHost(object[] payload)
        {
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                Dispatch(payload);
            }
            else
            {
                PhotonNetwork.RaiseEvent(EventCode, payload,
                    new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
                    SendOptions.SendReliable);
            }
        }

        private static void Dispatch(object[] payload)
        {
            var op = (Op)(byte)payload[0];
            switch (op)
            {
                case Op.SpawnChest:
                    ChestSpawner.SpawnLocalChest((Vector3)payload[1], (Quaternion)payload[2]);
                    break;
                case Op.SnapshotPush:
                    StorageService.ApplySnapshot((string)payload[1]);
                    break;
                case Op.SnapshotRequest:
                    if (SemiFunc.IsMasterClientOrSingleplayer())
                    {
                        BroadcastSnapshot(StorageService.Serialize());
                    }
                    break;
                case Op.TakeRequest:
                    StorageService.HostHandleTakeRequest((string)payload[1], (string)payload[2]);
                    break;
                case Op.UseRequest:
                    StorageService.HostHandleUseRequest((string)payload[1], (string)payload[2]);
                    break;
                case Op.EquipHint:
                    ItemSpawner.HandleEquipHint((string)payload[1], (int)payload[2]);
                    break;
            }
        }
    }
}
