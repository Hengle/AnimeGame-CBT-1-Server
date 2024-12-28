using GenshinCBTServer.Network.Send;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Receive
{
    public static class HandleGetPlayerTokenReq
    {
        [Server.Handler(CmdType.GetPlayerTokenReq)]
        public static void Handle(YPlayer session, CmdType cmdId, Packet packet)
        {
            GetPlayerTokenReq req = packet.DecodeBody<GetPlayerTokenReq>();
            session.SendPacket(new PacketGetPlayerTokenRsp(req, session));
        }
    }
}
