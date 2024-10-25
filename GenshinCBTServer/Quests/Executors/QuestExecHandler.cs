using GenshinCBTServer.Data;
using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests.Executors
{
    public abstract class QuestExecHandler
    {
        public abstract bool Execute(GameQuest quest, QuestExecuteCondition condition, params string[] paramStr);
    }
    public class ExecAddQuestProgress : QuestExecHandler
    {
        public override bool Execute(GameQuest quest, QuestExecuteCondition condition, params string[] paramStr)
        {
            var param = paramStr
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(uint.Parse)
                .ToArray();

            quest.GetOwner().AddQuestProgress(param[0], param[1]);

            return true;
        }
    }
    public class ExecUnlockPoint : QuestExecHandler
    {
        public override bool Execute(GameQuest quest, QuestExecuteCondition condition, params string[] paramStr)
        {
            // Unlock the trans point for the player.
            uint sceneId = uint.Parse(paramStr[0]);
            string[] ids = paramStr[1].Split(',');

           
            bool isStatue = quest.mainId == 303 || quest.mainId == 352;
            foreach(string id in ids)
            {
                uint pointId = uint.Parse(id);
                quest.GetOwner().UnlockTransPoint(sceneId, pointId, isStatue);
            }
            // Done.
            return true;
        }
    }
    public class ExecUnlockArea : QuestExecHandler
    {
        public override bool Execute(GameQuest quest, QuestExecuteCondition condition, params string[] paramStr)
        {
            // Unlock the trans point for the player.
            uint sceneId = uint.Parse(paramStr[0]);
            uint areaId = uint.Parse(paramStr[1]);
            quest.GetOwner().UnlockSceneArea(sceneId, areaId);

            // Done.
            return true;
        }
    }
    public class ExecChangeAvatarElement : QuestExecHandler
    {
        public override bool Execute(GameQuest quest, QuestExecuteCondition condition, params string[] paramStr)
        {
            var targetElement = (ElementType)int.Parse(paramStr[0]);
            var owner = quest.GetOwner();
            var mainAvatar = owner.GetMainAvatar();
            
            if (mainAvatar == null)
            {
                Server.Print($"Failed to get main avatar for use {quest.GetOwner().uid}");
                return false;
            }

            Server.Print($"Changing avatar element to {targetElement.ToString()} for quest {quest.subId}");
            return mainAvatar.ChangeElement(targetElement);
        }
    }
}
