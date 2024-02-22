﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Verse;

namespace GameClient
{
    public static class PlanetManager
    {
        public static List<Settlement> playerSettlements = new List<Settlement>();

        public static List<Site> playerSites = new List<Site>();

        public static void ParseSettlementPacket(Packet packet)
        {
            SettlementDetailsJSON settlementDetailsJSON = (SettlementDetailsJSON)Serializer.ConvertBytesToObject(packet.contents);

            switch (int.Parse(settlementDetailsJSON.settlementStepMode))
            {
                case (int)CommonEnumerators.SettlementStepMode.Add:
                    SpawnSingleSettlement(settlementDetailsJSON);
                    break;

                case (int)CommonEnumerators.SettlementStepMode.Remove:
                    RemoveSingleSettlement(settlementDetailsJSON);
                    break;
            }
        }

        public static void BuildPlayerPlanetFeatures()
        {
            FactionValues.FindPlayerFactionsInWorld();

            RemoveOldSettlements();
            RemoveOldSites();

            SpawnPlayerSettlements();
            SpawnPlayerSites();
        }

        private static void RemoveOldSettlements()
        {
            playerSettlements.Clear();

            DestroyedSettlement[] destroyedSettlements = Find.WorldObjects.DestroyedSettlements.ToArray();
            foreach (DestroyedSettlement destroyedSettlement in destroyedSettlements)
            {
                Find.WorldObjects.Remove(destroyedSettlement);
            }

            Settlement[] settlements = Find.WorldObjects.Settlements.ToArray();
            foreach (Settlement settlement in settlements)
            {
                if (settlement.Faction == FactionValues.allyPlayer)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }

                if (settlement.Faction == FactionValues.neutralPlayer)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }

                if (settlement.Faction == FactionValues.enemyPlayer)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }

                if (settlement.Faction == FactionValues.yourOnlineFaction)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }
            }
        }

        private static void SpawnPlayerSettlements()
        {
            for (int i = 0; i < PlanetManagerHelper.tempSettlementTiles.Count(); i++)
            {
                try
                {
                    Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    settlement.Tile = int.Parse(PlanetManagerHelper.tempSettlementTiles[i]);
                    settlement.Name = $"{PlanetManagerHelper.tempSettlementOwners[i]}'s settlement";
                    settlement.SetFaction(GetPlayerFaction(int.Parse(PlanetManagerHelper.tempSettlementLikelihoods[i])));

                    playerSettlements.Add(settlement);
                    Find.WorldObjects.Add(settlement);
                }

                catch (Exception e)
                {
                    Logs.Error($"Failed to build settlement at {PlanetManagerHelper.tempSettlementTiles[i]}. " +
                        $"Reason: {e}");
                }
            }
        }

        private static void RemoveOldSites()
        {
            playerSites.Clear();

            Site[] sites = Find.WorldObjects.Sites.ToArray();
            foreach (Site site in sites)
            {
                if (site.Faction == FactionValues.enemyPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == FactionValues.neutralPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == FactionValues.allyPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == FactionValues.yourOnlineFaction)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == Faction.OfPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }
            }
        }

        private static void SpawnPlayerSites()
        {
            for (int i = 0; i < PlanetManagerHelper.tempSiteTiles.Count(); i++)
            {
                try
                {
                    SitePartDef siteDef = SiteManager.GetDefForNewSite(int.Parse(PlanetManagerHelper.tempSiteTypes[i]),
                        PlanetManagerHelper.tempSiteIsFromFactions[i]);

                    Site site = SiteMaker.MakeSite(sitePart: siteDef,
                        tile: int.Parse(PlanetManagerHelper.tempSiteTiles[i]),
                        threatPoints: 1000,
                        faction: GetPlayerFaction(int.Parse(PlanetManagerHelper.tempSiteLikelihoods[i])));

                    playerSites.Add(site);
                    Find.WorldObjects.Add(site);
                }

                catch (Exception e)
                {
                    Logs.Error($"Failed to spawn site at {PlanetManagerHelper.tempSiteTiles[i]}. Reason: {e}");
                };
            }
        }

        public static void SpawnSingleSettlement(SettlementDetailsJSON newSettlementJSON)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    Settlement newSettlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    newSettlement.Tile = int.Parse(newSettlementJSON.tile);
                    newSettlement.Name = $"{newSettlementJSON.owner}'s settlement";
                    newSettlement.SetFaction(GetPlayerFaction(int.Parse(newSettlementJSON.value)));

                    playerSettlements.Add(newSettlement);
                    Find.WorldObjects.Add(newSettlement);
                }
                catch (Exception e) { Logs.Error($"Failed to spawn settlement at {newSettlementJSON.tile}. Reason: {e}"); }
            }
        }

        public static void RemoveSingleSettlement(SettlementDetailsJSON newSettlementJSON)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    Settlement toGet = playerSettlements.Find(x => x.Tile.ToString() == newSettlementJSON.tile);

                    playerSettlements.Remove(toGet);
                    Find.WorldObjects.Remove(toGet);
                }
                catch (Exception e) { Logs.Error($"Failed to remove settlement at {newSettlementJSON.tile}. Reason: {e}"); }
            }
        }

        public static void SpawnSingleSite(SiteDetailsJSON siteDetailsJSON)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    SitePartDef siteDef = SiteManager.GetDefForNewSite(int.Parse(siteDetailsJSON.type),
                        siteDetailsJSON.isFromFaction);

                    Site site = SiteMaker.MakeSite(sitePart: siteDef,
                        tile: int.Parse(siteDetailsJSON.tile),
                        threatPoints: 1000,
                        faction: GetPlayerFaction(int.Parse(siteDetailsJSON.likelihood)));

                    playerSites.Add(site);
                    Find.WorldObjects.Add(site);
                }

                catch (Exception e)
                {
                    Logs.Error($"Failed to spawn site at {siteDetailsJSON.tile}. Reason: {e}");
                };
            }
        }

        public static void RemoveSingleSite(SiteDetailsJSON siteDetailsJSON)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    Site toGet = playerSites.Find(x => x.Tile.ToString() == siteDetailsJSON.tile);

                    playerSites.Remove(toGet);
                    Find.WorldObjects.Remove(toGet);
                }
                catch (Exception e) { Logs.Message($"Failed to remove site at {siteDetailsJSON.tile}. Reason: {e}"); }
            }
        }

        public static Faction GetPlayerFaction(int value)
        {
            Faction factionToUse = null;
            switch (value)
            {
                case (int)CommonEnumerators.Likelihoods.Enemy:
                    factionToUse = FactionValues.enemyPlayer;
                    break;

                case (int)CommonEnumerators.Likelihoods.Neutral:
                    factionToUse = FactionValues.neutralPlayer;
                    break;

                case (int)CommonEnumerators.Likelihoods.Ally:
                    factionToUse = FactionValues.allyPlayer;
                    break;

                case (int)CommonEnumerators.Likelihoods.Faction:
                    factionToUse = FactionValues.yourOnlineFaction;
                    break;

                case (int)CommonEnumerators.Likelihoods.Personal:
                    factionToUse = Faction.OfPlayer;
                    break;
            }

            return factionToUse;
        }
    }

    public static class PlanetManagerHelper
    {
        public static string[] tempSettlementTiles;

        public static string[] tempSettlementOwners;

        public static string[] tempSettlementLikelihoods;

        public static string[] tempSiteTiles;

        public static string[] tempSiteOwners;

        public static string[] tempSiteLikelihoods;

        public static string[] tempSiteTypes;

        public static bool[] tempSiteIsFromFactions;

        public static void SetWorldFeatures(ServerOverallJSON serverOverallJSON)
        {
            tempSettlementTiles = serverOverallJSON.settlementTiles.ToArray();
            tempSettlementOwners = serverOverallJSON.settlementOwners.ToArray();
            tempSettlementLikelihoods = serverOverallJSON.settlementLikelihoods.ToArray();

            tempSiteTiles = serverOverallJSON.siteTiles.ToArray();
            tempSiteOwners = serverOverallJSON.siteOwners.ToArray();
            tempSiteLikelihoods = serverOverallJSON.siteLikelihoods.ToArray();
            tempSiteTypes = serverOverallJSON.siteTypes.ToArray();
            tempSiteIsFromFactions = serverOverallJSON.isFromFactions.ToArray();
        }
    }
}