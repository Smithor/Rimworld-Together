﻿using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;
using RimworldTogether.Shared.Serializers;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public static class EventManager
    {
        public enum EventStepMode { Send, Receive, Recover }

        public static void ParseEventPacket(ServerClient client, Packet packet)
        {
            EventDetailsJSON eventDetailsJSON = (EventDetailsJSON)ObjectConverter.ConvertBytesToObject(packet.contents);

            switch (int.Parse(eventDetailsJSON.eventStepMode))
            {
                case (int)EventStepMode.Send:
                    SendEvent(client, eventDetailsJSON);
                    break;

                case (int)EventStepMode.Receive:
                    //Nothing goes here
                    break;

                case (int)EventStepMode.Recover:
                    //Nothing goes here
                    break;
            }
        }

        public static void SendEvent(ServerClient client, EventDetailsJSON eventDetailsJSON)
        {
            if (!SettlementManager.CheckIfTileIsInUse(eventDetailsJSON.toTile)) ResponseShortcutManager.SendIllegalPacket(client);
            else
            {
                SettlementFile settlement = SettlementManager.GetSettlementFileFromTile(eventDetailsJSON.toTile);
                if (!UserManager.CheckIfUserIsConnected(settlement.owner))
                {
                    eventDetailsJSON.eventStepMode = ((int)EventStepMode.Recover).ToString();
                    Packet packet = Packet.CreatePacketFromJSON("EventPacket", eventDetailsJSON);
                    client.clientListener.SendData(packet);
                }

                else
                {
                    ServerClient target = UserManager.GetConnectedClientFromUsername(settlement.owner);
                    if (target.inSafeZone)
                    {
                        eventDetailsJSON.eventStepMode = ((int)EventStepMode.Recover).ToString();
                        Packet packet = Packet.CreatePacketFromJSON("EventPacket", eventDetailsJSON);
                        client.clientListener.SendData(packet);
                    }

                    else
                    {
                        target.inSafeZone = true;

                        Packet packet = Packet.CreatePacketFromJSON("EventPacket", eventDetailsJSON);
                        client.clientListener.SendData(packet);

                        eventDetailsJSON.eventStepMode = ((int)EventStepMode.Receive).ToString();
                        Packet rPacket = Packet.CreatePacketFromJSON("EventPacket", eventDetailsJSON);
                        client.clientListener.SendData(rPacket);
                    }
                }
            }
        }
    }
}
