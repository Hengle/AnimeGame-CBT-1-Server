using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenshinCBTServer.Protocol;
using CsvHelper.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Google.Protobuf.WellKnownTypes;
using System.Reflection;
using static SQLite.SQLite3;
using System.Buffers;
using GenshinCBTServer.Player;
using Org.BouncyCastle.Utilities;
using GenshinCBTServer.Quests.Contents;
using GenshinCBTServer.Data;
using GenshinCBTServer.Quests.Executors;

namespace GenshinCBTServer.Quests
{
    public class QuestManager
    {
        private Client client;

        public List<GameMainQuest> mainQuests = new();
        public List<uint> finishedList = new();
        public Dictionary<QuestContent, BaseContent> contHandlers = new();
        public Dictionary<QuestExec, QuestExecHandler> execHandlers = new();


        public QuestManager(Client client)
        {
            this.client = client;
            //TODO LOAD QUESTS

            //Adding here for now
            contHandlers.Add(QuestContent.QUEST_CONTENT_UNKNOWN, new ContentUnknown());
            contHandlers.Add(QuestContent.QUEST_CONTENT_NONE, new ContentNone());
            contHandlers.Add(QuestContent.QUEST_CONTENT_FINISH_PLOT, new ContentFinishPlot());
            contHandlers.Add(QuestContent.QUEST_CONTENT_TRIGGER_FIRE, new ContentTriggerFire());
            contHandlers.Add(QuestContent.QUEST_CONTENT_UNLOCK_TRANS_POINT, new ContentUnlockTransPoint());
            contHandlers.Add(QuestContent.QUEST_CONTENT_COMPLETE_TALK, new ContentCompleteTalk());
            contHandlers.Add(QuestContent.QUEST_CONTENT_COMPLETE_ANY_TALK, new ContentCompleteAnyTalk());
            //Execs
            execHandlers.Add(QuestExec.QUEST_EXEC_CHANGE_AVATAR_ELEMENT, new ExecChangeAvatarElement());
            execHandlers.Add(QuestExec.QUEST_EXEC_UNLOCK_POINT, new ExecUnlockPoint());
            execHandlers.Add(QuestExec.QUEST_EXEC_UNLOCK_AREA, new ExecUnlockArea());
            SendAllQuests();
        }
        public GameMainQuest AddMainQuest(QuestData data)
        {
            GameMainQuest mainQuest = new GameMainQuest(client, data.mainId);
            mainQuests.Add(mainQuest);
            client.SendPacket(CmdType.FinishedParentQuestUpdateNotify,new FinishedParentQuestUpdateNotify() { ParentQuestList = { mainQuest.toProto() } });

            return mainQuest;
        }
        public GameQuest AddQuest(uint id)
        {
            QuestData data = Server.getResources().questDict[id];

            GameMainQuest mainQuest = mainQuests.Find(q=>q.id==data.mainId);
            if(mainQuest == null)
            {
                mainQuest = AddMainQuest(data);
            }
            GameQuest quest = mainQuest.childQuests.Find(q => q.subId == id);
            quest.start();

            //check already fullfilled
            return quest;
        }
        public void SendAllQuests()
        {
            QuestListNotify notify = new()
            {

            };
            mainQuests.ForEach(m =>
            {
                m.childQuests.ForEach(q => { notify.QuestList.Add(q.ToProto()); });
            });
            client.SendPacket(CmdType.QuestListNotify, notify);
        }
        public GameMainQuest GetMainQuestByTalkId(uint talkId)
        {
            uint mainQuestId =  talkId / 100;
            return mainQuests.Find(m=>m.id==mainQuestId);
        }

        public List<GameQuest> GetAllQuests()
        {
            List<GameQuest> quests = new();

            mainQuests.ForEach(m =>
            {
                m.childQuests.ForEach(q => { quests.Add(q); });
            });
            return quests;
        }
        public void TriggerProgress(QuestContent content,string paramStr, params uint[] param)
        {
            List<GameMainQuest> quests = mainQuests.FindAll(q => q.state != ParentQuestState.PARENT_QUEST_STATE_FINISHED);

            quests.ForEach(quest =>
            {
                quest.TryFinishSubQuests(content,paramStr,param);
            });
        }
        public void TriggerEvent(QuestContent content, params uint[] param)
        {
            TriggerProgress(content, "", param);
        }
        public void TriggerEvent(QuestCond condType, params uint[] parameters)
        {
            TriggerEvent(condType, "", parameters);
        }
        public bool WasSubQuestStarted(QuestData data)
        {
            return GetAllQuests().Find(q => q.subId == data.subId && q.state != QuestState.UNSTARTED) != null;
        }
        public void TriggerEvent(QuestCond condType,string paramStr, params uint[] parameters)
        {
            var potentialQuests = Server.getResources().GetQuestDataByConditions(condType, parameters[0], paramStr);
            if (potentialQuests == null)
            {
                return;
            }

            var questSystem = client.GetQuestManager() ;

            var owner = client;
            foreach (QuestData questData in potentialQuests)
            {
                if (WasSubQuestStarted(questData))
                {
                    continue;
                }

                var acceptCond = questData.acceptCond;
                uint[] accept = new uint[acceptCond.Count];

                for (int i = 0; i < acceptCond.Count; i++)
                {
                    var condition = acceptCond[i];
                    bool result = questSystem.TriggerCondition(owner, questData, condition, paramStr, parameters);
                    accept[i] = result ? (uint)1 : 0;
                }

                bool shouldAccept = ((LogicType)questData.acceptCondComb).Calculate(accept);

                if (shouldAccept)
                {
                    var quest = owner.GetQuestManager().AddQuest(questData.subId);
                    Server.Print($"Added quest {questData.subId} result {quest != null}");
                }
            }
        }
        public void TriggerExec(GameQuest quest, QuestExecuteCondition execParam, params string[] parameters)
        {
            if (execParam.type == null)
            {
                Server.Print($"execParam.Type is null for quest {quest.subId}");
                return;
            }
            
            if (!execHandlers.TryGetValue((QuestExec)execParam.type, out QuestExecHandler handler) || quest.GetQuestData() == null)
            {
                Server.Print(string.Format("Could not trigger exec {0} for quest {1}", execParam.type, quest.GetQuestData().subId));
                return;
            }

            
            if (!handler.Execute(quest, execParam, parameters))
            {
                Server.Print(string.Format("Exec trigger failed {0} for quest {1}", execParam.type, quest.GetQuestData().subId));
            }
            
        }
        public bool TriggerCondition(Client owner, QuestData questData, QuestCondition e, string paramStr, params uint[] parameters)
        {
            return true;
        }

        public GameQuest GetQuestById(uint v)
        {
            GameQuest quest= null;
            mainQuests.ForEach(main =>
            {
                if(main.GetChildQuestById(v) != null)
                {
                    quest= main.GetChildQuestById(v); 
                }

            });
            return quest;
        }
        private BaseContent GetContentHandler(QuestContent type, QuestData questData)
        {
            BaseContent handler ;
            contHandlers.TryGetValue(type, out handler);
            if (handler == null)
            {
                Server.Print($"Could not get handler for content {type} in quest {questData}");
                return contHandlers[QuestContent.QUEST_CONTENT_UNKNOWN];
            }
            return handler;
        }
        public bool CheckAndUpdateContent(GameQuest quest, uint[] curProgress, List<QuestCondition> conditions, uint logictype, QuestContent condType, string paramStr, uint[] parameters)
        {
            var owner = quest.GetOwner();
            bool changed = false;
            uint[] finished = new uint[conditions.Count];

            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];

                BaseContent handler = GetContentHandler((QuestContent)condition.type, quest.GetQuestData());
                // only update progress if it's actually affected by the current event
                if (handler.IsEvent(quest.GetQuestData(), condition, condType, paramStr, parameters))
                {
                    var startingProgress = curProgress[(uint)i];
                    uint result = handler.UpdateProgress(quest, startingProgress, condition, paramStr, parameters);
                    curProgress[(uint)i] = result;
                    if (startingProgress != result)
                    {
                        changed = true;
                    }
                }
                finished[i] = handler.CheckProgress(quest, condition, curProgress[(uint)i]) ? (uint)1 : 0;
            }

            if (changed)
            {
               //Save?
            }

            return ((LogicType)logictype).Calculate(finished);
        }


    }

    
  
}
