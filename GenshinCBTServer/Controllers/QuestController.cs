using GenshinCBTServer.Excel;
using GenshinCBTServer.Player;
using GenshinCBTServer.Protocol;
using GenshinCBTServer.Quests;
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
        [Server.Handler(CmdType.NpcTalkReq)]
        public static void OnNpcTalkReq(Client session, CmdType cmdId, Network.Packet packet)
        {
            NpcTalkReq req = packet.DecodeBody<NpcTalkReq>();

            uint talkId = req.TalkId;
            QuestManager questManager = session.GetQuestManager();

           
            
            uint mainQuestId =  talkId / 100;
            MainQuestData mainQuestData = Server.getResources().mainQuestDict[mainQuestId];
           

            if (mainQuestData != null)
            {
                // This talk is associated with a quest. Handle it.
                // If the quest has no talk data defined on it, create one.
                var talkForQuest = new TalkData() {id=talkId };
                
                    var talks = mainQuestData.GetTalks().FindAll(p=>p.id == talkId);

                    if (talks.Count > 0)
                    {
                        talkForQuest = talks[0];
                    }
                

                // Add to the list of done talks for this quest.
                var mainQuest = questManager.GetMainQuestByTalkId(talkId);
                if (mainQuest != null)
                {
                    mainQuest.talks.Add(talkId, talkForQuest);
                }

            }
            questManager.TriggerEvent(QuestContent.QUEST_CONTENT_COMPLETE_ANY_TALK, talkId, 0, 0);
            questManager.TriggerEvent(QuestContent.QUEST_CONTENT_COMPLETE_TALK, talkId, 0);
            // questManager.Trigg(QuestCond.QUEST_COND_COMPLETE_TALK, talkId, 0);
            session.SendPacket(CmdType.NpcTalkRsp, new NpcTalkRsp() { CurTalkId=talkId,NpcEntityId=req.NpcEntityId,Retcode=0});
        }
        [Server.Handler(CmdType.LogCutsceneNotify)]
        public static void OnLogCutsceneNotify(Client session, CmdType cmdId, Network.Packet packet)
        {
            LogCutsceneNotify req = packet.DecodeBody<LogCutsceneNotify>();
            
        }
        [Server.Handler(CmdType.AddQuestContentProgressReq)]
        public static void OnAddQuestContentProgressReq(Client session, CmdType cmdId, Network.Packet packet)
        {
            
            AddQuestContentProgressReq req = packet.DecodeBody<AddQuestContentProgressReq>();
            Server.Print(req.Param + ", " + req.AddProgress + ", " + ((QuestContent)req.ContentType).ToString());
            session.GetQuestManager().TriggerProgress((QuestContent)req.ContentType,"", req.Param);
           
            session.SendPacket(CmdType.AddQuestContentProgressRsp, new AddQuestContentProgressRsp() { ContentType = req.ContentType,Retcode=0 });
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
