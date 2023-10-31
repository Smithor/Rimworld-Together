﻿using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Managers;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;
using RimworldTogether.Shared.Serializers;

namespace RimworldTogether.GameServer.Users
{
    public static class UserRegister
    {
        public static void TryRegisterUser(ServerClient client, Packet packet)
        {
            LoginDetailsJSON registerDetails = (LoginDetailsJSON)ObjectConverter.ConvertBytesToObject(packet.contents);
            client.username = registerDetails.username;
            client.password = registerDetails.password;

            if (!UserManager_Joinings.CheckLoginDetails(client, UserManager_Joinings.CheckMode.Register)) return;

            if (TryFetchAlreadyRegistered(client)) return;
            else
            {
                UserFile userFile = new UserFile();
                userFile.uid = GetNewUIDForUser(client);
                userFile.username = client.username;
                userFile.password = client.password;

                try
                {
                    UserManager.SaveUserFile(client, userFile);

                    UserManager_Joinings.SendLoginResponse(client, UserManager_Joinings.LoginResponse.RegisterSuccess);

                    Logger.WriteToConsole($"[Registered] > {client.username}");
                }

                catch 
                {
                    UserManager_Joinings.SendLoginResponse(client, UserManager_Joinings.LoginResponse.RegisterError);

                    return;
                }
            }
        }

        private static bool TryFetchAlreadyRegistered(ServerClient client)
        {
            string[] existingUsers = Directory.GetFiles(Program.usersPath);

            foreach (string user in existingUsers)
            {
                UserFile existingUser = Serializer.SerializeFromFile<UserFile>(user);
                if (existingUser.username.ToLower() != client.username.ToLower()) continue;
                else
                {
                    UserManager_Joinings.SendLoginResponse(client, UserManager_Joinings.LoginResponse.RegisterInUse);

                    return true;
                }
            }

            return false;
        }

        private static string GetNewUIDForUser(ServerClient client)
        {
            return Hasher.GetHash(client.username);
        }
    }
}
