using GenshinCBTServer.Network.Send;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Receive
{
    public static class HandlePlayerLoginReq
    {
        [Server.Handler(CmdType.PlayerLoginReq)]
        public static void Handle(YPlayer session, CmdType cmdId,Packet packet)
        {
            PlayerLoginReq req = packet.DecodeBody<PlayerLoginReq>();
            session.InitiateAccount(req.Token);
            PacketPlayerLoginRsp resp = new PacketPlayerLoginRsp(session.uid);
            if (session.avatars.Count < 1)
            {
                DoSetPlayerBornDataNotify start = new()
                {

                };
                session.SendPacket((uint)CmdType.DoSetPlayerBornDataNotify, start);
            }
            else
            {
                session.TeleportToScene(3);
            }
            session.SendPacket(resp);
        }
    }
}
