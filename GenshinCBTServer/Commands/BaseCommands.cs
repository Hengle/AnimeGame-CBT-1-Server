using GenshinCBTServer.Protocol;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Commands
{
    public static class BaseCommands
    {

        [Server.Command("unlockall", "Unlock all")]
        public static void UnlockAllCmd(string cmd, string[] args)
        {
            if(args.Length > 0)
            {
                uint uid = uint.Parse(args[0]);
                Client client = Server.clients.Find(c => c.uid == uid);

                if(client != null)
                {
                    for(int i=0; i < 200; i++)
                    {
                        client.unlockedPoints.Add((uint)i);
                        client.SendPacket(CmdType.ScenePointUnlockNotify, new ScenePointUnlockNotify() { SceneId=3,PointList = { (uint)i } });
                    }
                }
            }
        }
        [Server.Command("quest", "Quest management")]
        public static void QuestCmd(string cmd, string[] args)
        {
            if (args.Length > 2)
            {
                uint uid = uint.Parse(args[0]);
                Client client = Server.clients.Find(c=>c.uid == uid);
                if (client != null)
                {
                    uint questId = uint.Parse(args[2]);
                    string subcmd = args[1];
                    if(subcmd == "start")
                    {
                        if(client.GetQuestManager().AddQuest(questId) != null)
                        {
                            Server.Print("Quest started");
                        }
                        else
                        {
                            Server.Print("Quest already started");
                        }
                    }
                }
                else
                {
                    Server.Print("Player not found");
                }
            }
            else
            {
                Server.Print("Usage: quest (uid) start,finish (id)");
            }
        }
        [Server.Command("account", "Account management")]
        public static void onDispatchCmd(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "new")
                {
                    if (args.Length > 1)
                    {
                        Server.dispatch.NewAccount(args[1], args[2]);
                    }
                    else
                    {
                        Server.Print("Usage: account new (name) (password)");
                    }
                }
            }
            else
            {
                Server.Print("Usage: account new (name) (password)");
            }
        }
    }
}