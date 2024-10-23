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
        public Dictionary<uint, uint> finishProgressList = new();
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

            //questManager.queueEvent(QuestCond.QUEST_COND_STATE_EQUAL, questId, state);
            //questManager.queueEvent(QuestCond.QUEST_COND_STATE_NOT_EQUAL, questId, state);
           // questManager.TriggerEvent(QuestContent.QUEST_CONTENT_QUEST_STATE_EQUAL, questId, state);
           // questManager.TriggerEvent(QuestContent.QUEST_CONTENT_QUEST_STATE_NOT_EQUAL, questId, state);

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

            GetQuestData().finishExec.ForEach(e=>GetOwner().GetQuestManager().TriggerExec(this, e, e.param));
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

        public void TryFinishSubQuests(uint condType, string paramStr, uint[] parameters)
        {
           /* List<GameQuest> subQuestsWithCond = childQuests.FindAll(q => q.state == QuestState.UNFINISHED && q.GetQuestData().acceptCond!=null)
                .FindAll(q=>q.GetQuestData().finishCond.Any(q => q.type == condType));
            foreach (GameQuest subQuestWithCond in subQuestsWithCond)
            {
                var finishCond = subQuestWithCond.GetQuestData().finishCond;

                for (int i = 0; i < finishCond.Count; i++)
                {
                    var condition = finishCond[i];
                    if (condition.type == condType)
                    {
                         bool result = this.GetOwner()
                                           .GetQuestManager()
                                           .TriggerContent(subQuestWithCond, condition, paramStr, parameters);
                       // bool result = true;

                        subQuestWithCond.SetFinishProgress((uint)i, (uint)(result ? 1 : 0));


                        Server.Print($"Quest finish progress {(QuestContent)condType} {paramStr} with params: {parameters.ToString()} finished: {subQuestWithCond.state==QuestState.FINISHED}");
                        if (result)
                        {
                            
                            GetOwner().SendPacket(CmdType.QuestListUpdateNotify, new QuestListUpdateNotify() { QuestList = {subQuestWithCond.ToProto()}});
                        }
                    }
                }
            }*/
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

        public QuestManager(Client client)
        {
            this.client = client;
            //TODO LOAD QUESTS


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

        public void TriggerExec(GameQuest gameQuest, QuestExecuteCondition e, string[] param)
        {
           
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
    }
}
