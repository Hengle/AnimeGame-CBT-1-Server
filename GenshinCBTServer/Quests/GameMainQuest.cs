using GenshinCBTServer.Data;
using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests
{
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
            List<QuestData> datas = Server.getResources().questDict.Values.ToList().FindAll(q => q.mainId == parentQuestId);
            foreach (QuestData data in datas)
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
                        QuestId = quest.subId,
                        State = (uint)quest.state
                    });
                }
            });
            return parent;
        }

        public void TryFinishSubQuests(QuestContent condType, string paramStr, uint[] parameters)
        {

            List<GameQuest> subQuestsWithCond = childQuests.FindAll(q => q.state == QuestState.UNFINISHED && q.GetQuestData().acceptCond != null)
                .FindAll(q => q.GetQuestData().finishCond.Any(q => q.type == (uint)condType));

            QuestManager questManager = GetOwner().GetQuestManager();
            subQuestsWithCond.ForEach(quest =>
            {
                List<QuestCondition> finishCond = quest.GetQuestData().finishCond;
                uint finishCondComb = quest.GetQuestData().finishCondComb;
                uint[] finishProgressList = quest.finishProgressList;


                bool shouldFinish = questManager.CheckAndUpdateContent(quest, finishProgressList, finishCond, finishCondComb, condType, paramStr, parameters);

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
}
