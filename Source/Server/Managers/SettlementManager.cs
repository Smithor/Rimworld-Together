﻿using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;
using RimworldTogether.Shared.Serializers;

namespace RimworldTogether.GameServer.Managers
{
    public static class SettlementManager
    {
        public enum SettlementStepMode { Add, Remove }

        public static void ParseSettlementPacket(ServerClient client, Packet packet)
        {
            SettlementDetailsJSON settlementDetailsJSON = (SettlementDetailsJSON)ObjectConverter.ConvertBytesToObject(packet.contents);

            switch (int.Parse(settlementDetailsJSON.settlementStepMode))
            {
                case (int)SettlementStepMode.Add:
                    AddSettlement(client, settlementDetailsJSON);
                    break;

                case (int)SettlementStepMode.Remove:
                    RemoveSettlement(client, settlementDetailsJSON);
                    break;
            }
        }

        public static bool CheckIfTileIsInUse(string tileToCheck)
        {
            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach(string settlement in settlements)
            {
                SettlementFile settlementJSON = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementJSON.tile == tileToCheck) return true;
            }

            return false;
        }

        public static SettlementFile GetSettlementFileFromTile(string tileToGet)
        {
            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.tile == tileToGet) return settlementFile;
            }

            return null;
        }

        public static SettlementFile GetSettlementFileFromUsername(string usernameToGet)
        {
            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.owner == usernameToGet) return settlementFile;
            }

            return null;
        }

        public static SettlementFile[] GetAllSettlements()
        {
            List<SettlementFile> settlementList = new List<SettlementFile>();

            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                settlementList.Add(Serializer.SerializeFromFile<SettlementFile>(settlement));
            }

            return settlementList.ToArray();
        }

        public static SettlementFile[] GetAllSettlementsFromUsername(string usernameToCheck)
        {
            List<SettlementFile> settlementList = new List<SettlementFile>();

            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.owner == usernameToCheck) settlementList.Add(settlementFile);
            }

            return settlementList.ToArray();
        }

        public static void AddSettlement(ServerClient client, SettlementDetailsJSON settlementDetailsJSON)
        {
            if (CheckIfTileIsInUse(settlementDetailsJSON.tile)) ResponseShortcutManager.SendIllegalPacket(client);
            else
            {
                settlementDetailsJSON.owner = client.username;

                SettlementFile settlementFile = new SettlementFile();
                settlementFile.tile = settlementDetailsJSON.tile;
                settlementFile.owner = client.username;
                Serializer.SerializeToFile(Path.Combine(Program.settlementsPath, settlementFile.tile + ".json"), settlementFile);

                settlementDetailsJSON.settlementStepMode = ((int)SettlementStepMode.Add).ToString();
                foreach (ServerClient cClient in Network.Network.connectedClients.ToArray())
                {
                    if (cClient == client) continue;
                    else
                    {
                        settlementDetailsJSON.value = LikelihoodManager.GetSettlementLikelihood(cClient, settlementFile).ToString();

                        Packet rPacket = Packet.CreatePacketFromJSON("SettlementPacket", settlementDetailsJSON);
                        client.clientListener.SendData(rPacket);
                    }
                }

                Logger.WriteToConsole($"[Added settlement] > {settlementFile.tile} > {client.username}", Logger.LogMode.Warning);
            }
        }

        public static void RemoveSettlement(ServerClient client, SettlementDetailsJSON settlementDetailsJSON, bool sendRemoval = true)
        {
            if (!CheckIfTileIsInUse(settlementDetailsJSON.tile)) ResponseShortcutManager.SendIllegalPacket(client);

            SettlementFile settlementFile = GetSettlementFileFromTile(settlementDetailsJSON.tile);

            if (sendRemoval)
            {
                if (settlementFile.owner != client.username) ResponseShortcutManager.SendIllegalPacket(client);
                else
                {
                    File.Delete(Path.Combine(Program.settlementsPath, settlementFile.tile + ".json"));

                    SendSettlementRemoval(client, settlementDetailsJSON);
                }
            }

            else
            {
                File.Delete(Path.Combine(Program.settlementsPath, settlementFile.tile + ".json"));

                Logger.WriteToConsole($"[Remove settlement] > {settlementFile.tile}", Logger.LogMode.Warning);
            }
        }

        private static void SendSettlementRemoval(ServerClient client, SettlementDetailsJSON settlementDetailsJSON)
        {
            settlementDetailsJSON.settlementStepMode = ((int)SettlementStepMode.Remove).ToString();
            Packet rPacket = Packet.CreatePacketFromJSON("SettlementPacket", settlementDetailsJSON);
            foreach (ServerClient cClient in Network.Network.connectedClients.ToArray())
            {
                if (cClient == client) continue;
                else client.clientListener.SendData(rPacket);
            }

            Logger.WriteToConsole($"[Remove settlement] > {settlementDetailsJSON.tile} > {client.username}", Logger.LogMode.Warning);
        }
    }
}
