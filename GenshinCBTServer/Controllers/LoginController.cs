using GenshinCBTServer.Excel;
using GenshinCBTServer.Network.Send;
using GenshinCBTServer.Player;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GenshinCBTServer.Controllers
{
    public class LoginController
    {

        [Server.Handler(CmdType.SetPlayerBornDataReq)]
        public static void OnSetPlayerBornDataReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            SetPlayerBornDataReq req = packet.DecodeBody<SetPlayerBornDataReq>();
            session.name = req.NickName;
            session.avatars.Add(new Avatar(session, req.AvatarId));
            //  session.avatars.Add(new Avatar(session, 10000016));

            session.selectedAvatar = (int)session.avatars[0].guid;
           /* foreach (AvatarData av in Server.getResources().avatarsData)
            {
                if (av.id != req.AvatarId) session.avatars.Add(new Avatar(session, av.id));
            }*/

             session.team = new uint[] { session.avatars[0].id };
            PlayerDataNotify playerDataNotify = new PlayerDataNotify()
            {
                NickName = session.name,
                ServerTime = 0,

            };
            playerDataNotify.PropMap.Add(session.GetPlayerProps());
            session.SendPacket((uint)CmdType.PlayerDataNotify, playerDataNotify);
            session.TeleportToScene(3);
            session.SendPacket((uint)CmdType.SetPlayerBornDataRsp, new SetPlayerBornDataRsp() { });
            session.SendInventory();
            session.SendAllAvatars();
        }
        
        [Server.Handler(CmdType.PingReq)]
        public static void OnPingReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {

            PingReq req = packet.DecodeBody<PingReq>();
            session.SendPacket((uint)CmdType.PingRsp, new PingRsp() { ClientTime = req.ClientTime, Retcode = 0, Seq = req.Seq });
        }
    }
}
