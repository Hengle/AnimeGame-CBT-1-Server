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

namespace GenshinCBTServer.Quests
{
    public enum QuestState
    {
        NONE=0,
        UNSTARTED=1,
        UNFINISHED=2,
        FINISHED=3,
        FAILED=4,
    }
    public enum ParentQuestState
    {
        PARENT_QUEST_STATE_NONE=0,
        PARENT_QUEST_STATE_FINISHED=1,
        PARENT_QUEST_STATE_FAILED=2,
        PARENT_QUEST_STATE_CANCELED=3
    }
    public class GameQuest
    {
        public uint mainId;
        public uint subId;
        public QuestState state;

        public uint startTime;
        public uint finishTime;
        public uint startGameTime;
        public uint ownerUid;
        public Dictionary<uint, uint> finishProgressList = new() { {0,0}, { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } };
        public QuestData GetQuestData()
        {
            return Server.getResources().questDict[subId];
        }
        public GameMainQuest GetMainQuest()
        {
            return GetOwner().GetQuestManager().mainQuests.Find(m => m.parentQuestId == mainId);
        }
        public GameQuest(uint ownerUid,QuestData data)
        {
            mainId = data.mainId;
            subId = data.subId;
            state = QuestState.UNSTARTED;
            this.ownerUid=ownerUid;
        }
        public Client GetOwner()
        {
            return Server.clients.Find(c => c.uid == ownerUid);
        }
        public void start()
        {
            if(state==QuestState.UNSTARTED)
            {
                startTime = Server.GetCurrentSeconds();
                startGameTime = 1;
                //TODO trigger cond

                state = QuestState.UNFINISHED;
                QuestListUpdateNotify notify = new();
                notify.QuestList.Add(ToProto());

                GetOwner().SendPacket(CmdType.QuestListUpdateNotify, notify);
                
            }
        }
        public void triggerStateEvents()
        {
            QuestManager questManager = GetOwner().GetQuestManager();
            uint questId = this.subId;
            uint state = (uint)this.state;

            questManager.TriggerEvent(QuestCond.QUEST_COND_STATE_EQUAL, questId, state);
            questManager.TriggerEvent(QuestCond.QUEST_COND_STATE_NOT_EQUAL, questId, state);

            //questManager.TriggerEvent(QuestContent.QUEST_CONTENT_QUEST_STATE_EQUAL, questId, state);
            //questManager.TriggerEvent(QuestContent.QUEST_CONTENT_QUEST_STATE_NOT_EQUAL, questId, state);

        }
        public void finish()
        {
            state = QuestState.FINISHED;
            finishTime=Server.GetCurrentSeconds();
            QuestListUpdateNotify notify = new();
            notify.QuestList.Add(ToProto());

            GetOwner().SendPacket(CmdType.QuestListUpdateNotify, notify);

            if (GetQuestData().finishParent)
            {
                GetMainQuest().finish();
            }

            //GetQuestData().finishExec.ForEach(e=>GetOwner().GetQuestManager().TriggerExec(this, e, e.param));
            triggerStateEvents();  
        }

        public Quest ToProto()
        {
            return new() {
                ParentQuestId = mainId,
                QuestId = subId,
                State = (uint)state,
                StartTime = startTime,
                StartGameTime = startGameTime,
                
            };
        }

        internal void SetFinishProgress(uint index, uint val)
        {
            if(finishProgressList.ContainsKey(index))
            {
                finishProgressList[index] = val;
            }
            else
            {
                finishProgressList.Add(index, val);
            }
        }
    }

    public class GameMainQuest
    {
        public uint id;
        public uint ownerUid;
        public uint parentQuestId;
        public List<GameQuest> childQuests = new();
        public uint[] questVars;
        public long[] timeVar;
        public ParentQuestState state;
        public bool isFinished = false;
        public Dictionary<uint, TalkData> talks = new();
        public GameMainQuest(Client player, uint parentQuestId)
        {
            this.ownerUid = player.uid;
            
           
            this.parentQuestId = parentQuestId;
            
            //this.talks = new HashMap<>();
            // official server always has a list of 5 questVars, with default value 0
            this.questVars = new uint[] { 0, 0, 0, 0, 0 };
            this.timeVar =
                    new long[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }; // theoretically max is 10 here
            this.state = ParentQuestState.PARENT_QUEST_STATE_NONE;
            //this.questGroupSuites = new ArrayList<>();
            addAllChildQuests();
        }
        public void addAllChildQuests()
        {
            List<QuestData> datas = Server.getResources().questDict.Values.ToList().FindAll(q => q.mainId == parentQuestId) ;
            foreach(QuestData data in datas)
            {
                GameQuest quest = new(ownerUid, data);
                childQuests.Add(quest);
            }
        }
        public Client GetOwner()
        {
            return Server.clients.Find(c => c.uid == ownerUid);
        }
        public void finish()
        {

        }
        public ParentQuest toProto()
        {
            ParentQuest parent = new()
            {
                ParentQuestId = parentQuestId,
                IsFinished = isFinished,
                
                
            };
            childQuests.ForEach(quest =>
            {
                if (quest.state != QuestState.UNSTARTED)
                {
                    parent.ChildQuestList.Add(new ChildQuest()
                    {
                        QuestId=quest.subId,
                        State=(uint)quest.state
                    });
                }
            });
            return parent;
        }

        public void TryFinishSubQuests(QuestContent condType, string paramStr, uint[] parameters)
        {

            List<GameQuest> subQuestsWithCond = childQuests.FindAll(q => q.state == QuestState.UNFINISHED && q.GetQuestData().acceptCond != null)
                .FindAll(q => q.GetQuestData().finishCond.Any(q => q.type ==(uint) condType));

            QuestManager questManager = GetOwner().GetQuestManager();
            subQuestsWithCond.ForEach(quest =>
            {
                List<QuestCondition> finishCond = quest.GetQuestData().finishCond;
                uint finishCondComb = quest.GetQuestData().finishCondComb;
                Dictionary<uint,uint> finishProgressList = quest.finishProgressList;


                bool shouldFinish = questManager.CheckAndUpdateContent(quest,finishProgressList,finishCond,finishCondComb,condType,paramStr,parameters);

                if (shouldFinish)
                {
                    quest.finish();
                }
            });
        }

        public void TryFailSubQuests(uint condType, string paramStr, int[] parameters)
        {
            //throw new NotImplementedException();
        }

        public GameQuest GetChildQuestById(uint v)
        {
            return childQuests.Find(q => q.subId == v);
        }
    }
    public class QuestManager
    {
        private Client client;

        public List<GameMainQuest> mainQuests = new();
        public List<uint> finishedList = new();
        public Dictionary<QuestContent, BaseContent> contHandlers = new();

        
        public QuestManager(Client client)
        {
            this.client = client;
            //TODO LOAD QUESTS

            //Adding here for now
            contHandlers.Add(QuestContent.QUEST_CONTENT_UNKNOWN, new ContentUnknown());
            contHandlers.Add(QuestContent.QUEST_CONTENT_NONE, new ContentNone());
            contHandlers.Add(QuestContent.QUEST_CONTENT_FINISH_PLOT, new ContentFinishPlot());
            contHandlers.Add(QuestContent.QUEST_CONTENT_TRIGGER_FIRE, new ContentTriggerFire());
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
        public bool CheckAndUpdateContent(GameQuest quest, Dictionary<uint, uint> curProgress, List<QuestCondition> conditions, uint logictype, QuestContent condType, string paramStr, uint[] parameters)
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
    public class ContentFinishPlot : BaseContent
    {

    }
    public class ContentTriggerFire : BaseContent
    {

    }
    public class ContentNone : BaseContent
    {

      
        public override uint UpdateProgress(GameQuest quest, uint currentProgress, QuestCondition condition, string paramStr, params uint[] parameters)
        {
            return 1;
        }

   
        public override bool CheckProgress(GameQuest quest, QuestCondition condition, uint currentProgress)
        {
            return true;
        }
    }
    public class ContentUnknown : BaseContent
    {
        
        public override uint UpdateProgress(GameQuest quest, uint currentProgress, QuestCondition condition,string paramStr, params uint[] parameters)
        {
            return 0;
        }

    
        public override bool CheckProgress(GameQuest quest, QuestCondition condition, uint currentProgress)
        {
            
            return false;
        }
    }
    public abstract class BaseContent
    {
        public virtual bool IsEvent(QuestData questData, QuestCondition condition, QuestContent type, string paramStr, params uint[] parameters)
        {
            return condition.type == (uint)type && condition.param[0] == parameters[0];
        }

        public virtual uint InitialCheck(GameQuest quest, QuestData questData, QuestCondition condition)
        {
            return 0;
        }

        public virtual uint UpdateProgress(GameQuest quest, uint currentProgress, QuestCondition condition, string paramStr, params uint[] parameters)
        {
            return currentProgress + 1;
        }

        public virtual bool CheckProgress(GameQuest quest, QuestCondition condition, uint currentProgress)
        {
            uint target = condition.count > 0 ? condition.count : 1;
            return currentProgress >= target;
        }
    }

}
