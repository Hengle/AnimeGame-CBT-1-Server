using GenshinCBTServer.Data;
using GenshinCBTServer.Excel;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests
{
    public class GameQuest
    {
        public uint mainId;
        public uint subId;
        public QuestState state;

        public uint startTime;
        public uint finishTime;
        public uint startGameTime;
        public uint ownerUid;
        public uint[] finishProgressList;
        public uint[] failProgressList;
        public Dictionary<string, TriggerData> triggerData = new();
        public Dictionary<string, bool> triggers = new();
        public QuestData GetQuestData()
        {
            return Server.getResources().questDict[subId];
        }
        public GameMainQuest GetMainQuest()
        {
            return GetOwner().GetQuestManager().mainQuests.Find(m => m.parentQuestId == mainId);
        }
        public GameQuest(uint ownerUid, QuestData data)
        {
            mainId = data.mainId;
            subId = data.subId;
            state = QuestState.UNSTARTED;
            finishProgressList = new uint[data.finishCond.Count];
            this.ownerUid = ownerUid;
        }
        public Client GetOwner()
        {
            return Server.clients.Find(c => c.uid == ownerUid);
        }
        public void start()
        {
            if (state == QuestState.UNSTARTED)
            {
                startTime = Server.GetCurrentSeconds();
                startGameTime = 1;
                //TODO trigger cond

                var triggerCond = GetQuestData().finishCond.Where(p => p.type == (uint)QuestContent.QUEST_CONTENT_TRIGGER_FIRE).ToList();

                if (triggerCond.Count > 0)
                {
                    foreach (var cond in triggerCond)
                    {
                        var newTrigger = Server.getResources().triggerData.Find(t => t.id == cond.param[0]);
                        if (newTrigger != null)
                        {
                            if (this.triggerData == null)
                            {
                                this.triggerData = new Dictionary<string, TriggerData>();
                            }

                            triggerData[newTrigger.triggerName] = newTrigger;
                            triggers[newTrigger.triggerName] = false;

                            /* var group = SceneGroup.Of(newTrigger.GetGroupId()).Load(newTrigger.GetSceneId());
                             this.GetOwner()
                                 .GetWorld()
                                 .GetSceneById(newTrigger.GetSceneId())
                                 .LoadTriggerFromGroup(group, newTrigger.GetTriggerName());*/
                        }
                    }
                }
                //
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
            finishTime = Server.GetCurrentSeconds();
            QuestListUpdateNotify notify = new();
            notify.QuestList.Add(ToProto());

            GetOwner().SendPacket(CmdType.QuestListUpdateNotify, notify);

            if (GetQuestData().finishParent)
            {
                GetMainQuest().finish();
            }

            GetQuestData().finishExec.ForEach(e => GetOwner().GetQuestManager().TriggerExec(this, e, e.param));
            triggerStateEvents();
        }

        public Quest ToProto()
        {
            return new()
            {
                ParentQuestId = mainId,
                QuestId = subId,
                State = (uint)state,
                StartTime = startTime,
                StartGameTime = startGameTime,

            };
        }

        internal void SetFinishProgress(uint index, uint val)
        {
            finishProgressList[index] = val;
        }
    }
}
