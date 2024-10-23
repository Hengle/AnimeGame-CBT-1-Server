using GenshinCBTServer.Excel;
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
    public class QuestController
    {
        [Server.Handler(CmdType.LogCutsceneNotify)]
        public static void OnLogCutsceneNotify(Client session, CmdType cmdId, Network.Packet packet)
        {
            LogCutsceneNotify req = packet.DecodeBody<LogCutsceneNotify>();
           
        }
        [Server.Handler(CmdType.AddQuestContentProgressReq)]
        public static void OnAddQuestContentProgressReq(Client session, CmdType cmdId, Network.Packet packet)
        {
            AddQuestContentProgressReq req = packet.DecodeBody<AddQuestContentProgressReq>();
           // session.GetQuestManager().TriggerEvent((QuestContent)req.ContentType,"", req.Param);
           
            session.SendPacket(CmdType.AddQuestContentProgressRsp, new AddQuestContentProgressRsp() { ContentType = req.ContentType });
        }
        [Server.Handler(CmdType.QuestCreateEntityReq)]
        public static void OnQuestCreateEntityReq(Client session, CmdType cmdId, Network.Packet packet)
        {
            QuestCreateEntityReq req = packet.DecodeBody<QuestCreateEntityReq>();
            GameEntity entity = null;
            if (req.Entity.EntityCase == CreateEntityInfo.EntityOneofCase.NpcId)
            {


                entity = new GameEntity(session, req.Entity);

            }
                
            if(entity!=null)
            {
                session.world.SpawnEntity(entity, true);
                QuestCreateEntityRsp rsp = new()
                {
                    Entity = req.Entity,
                    EntityId = entity.entityId,
                    IsRewind = req.IsRewind,
                    QuestId = req.QuestId,
                    Retcode = 0
                };
                session.SendPacket(CmdType.QuestCreateEntityRsp, rsp);
            }
           
        }
    }
}
