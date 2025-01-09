using GenshinCBTServer.Network.Send;
using GenshinCBTServer.Player;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Receive
{
    public static class HandleUseItemReq
    {
        [Server.Handler(CmdType.UseItemReq)]
        public static void Handle(YPlayer session, CmdType cmdId,Packet packet)
        {
            UseItemReq req = packet.DecodeBody<UseItemReq>();
            GameItem item = session.GetItemByGuid(req.Guid);
            
            if(item != null) 
            {
                session.UseItem(item, req.TargetGuid, req.Count);
            }

        }
    }
}
