using GenshinCBTServer.Controllers;
using GenshinCBTServer.Excel;
using GenshinCBTServer.Network;
using GenshinCBTServer.Player;
using GenshinCBTServer.Data;
using GenshinCBTServer.Protocol;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Pastel;
using SQLite;
using SQLiteNetExtensions.Attributes;
using System.Drawing;
using static GenshinCBTServer.ENet;
using GenshinCBTServer.Quests;
using System.Linq;
using Org.BouncyCastle.Ocsp;
using System.Numerics;
using MongoDB.Bson.Serialization.Attributes;
using System.Reflection;

namespace GenshinCBTServer
{
    

    public class GuidRandomizer
    {
        public int v = 0;
        public int Next()
        {
            v++;
            return v;
        }
    }

    public class YPlayer
    {
        public GuidRandomizer random = new GuidRandomizer();
        [BsonIgnore]
        private QuestManager questManager;
        [BsonIgnore]
        public IntPtr peer;
        [BsonIgnore]
        public int gamePeer = 0;
        public MapField<uint, uint> openStateMap = new MapField<uint, uint>();
        public uint currentSceneId = 3;
        public uint prevSceneId = 0;
        public uint returnPointId = 1;
        public uint[] team = { 10000016, 10000015, 10000002, 10000022 };
        public uint teamEntityId;
        public int selectedAvatar = 0;
       
        public List<Avatar> avatars = new List<Avatar>();
        
        public List<GameItem> inventory = new List<GameItem>();
        public uint uid;
        public string name;
        public string token;
        public MotionInfo motionInfo = new MotionInfo() { Pos = new Vector() { X = 2136.926f, Y = 208, Z = -1172 }, Rot = new(), Speed = new(), State = MotionState.MotionStandby };
        [BsonIgnore]
        public World world;
        public List<uint> unlockedPoints = new();
        public Dictionary<uint, uint> unlockedAreas = new();
        [BsonIgnore]
        public List<uint> inRegions = new List<uint>();

        public int adventureLevel = 1;
        public int GetExp
        {
            get
            {
                GameItem item = inventory.Find(i => i.id == 102);
                if (item != null)
                {
                    return item.amount;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int GetPrimo
        {
            get
            {
                GameItem item = inventory.Find(i => i.id == 201);
                if (item != null)
                {
                    return item.amount;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int GetMora
        {
            get
            {
                GameItem item = inventory.Find(i => i.id == 202);
                if (item != null)
                {
                    return item.amount;
                }
                else
                {
                    return 0;
                }
            }
        }
        public MapField<uint, PropValue> GetPlayerProps()
        {
            MapField<uint, PropValue> props = new MapField<uint, PropValue>();
            props.Add((uint)PropType.PROP_LAST_CHANGE_AVATAR_TIME, new PropValue() { Val = 0 });
            addProp((uint)PropType.PROP_IS_FLYABLE, 1, props);
            props.Add((uint)PropType.PROP_IS_WEATHER_LOCKED, new PropValue() { Val = 0 });
            props.Add((uint)PropType.PROP_IS_GAME_TIME_LOCKED, new PropValue() { Val = 0 });
            addProp((uint)PropType.PROP_IS_TRANSFERABLE, 1, props);
            addProp((uint)PropType.PROP_MAX_STAMINA, 15000, props);
            addProp((uint)PropType.PROP_CUR_PERSIST_STAMINA, 15000, props);
            addProp((uint)PropType.PROP_CUR_TEMPORARY_STAMINA, 15000, props);
            addProp((uint)PropType.PROP_PLAYER_LEVEL, adventureLevel, props);
            addProp((uint)PropType.PROP_PLAYER_EXP, GetExp, props);
            addProp((uint)PropType.PROP_IS_SPRING_AUTO_USE, 1, props);
            addProp((uint)PropType.PROP_SPRING_AUTO_USE_PERCENT, 50, props);
            addProp((uint)PropType.PROP_PLAYER_HCOIN, GetPrimo, props);
            addProp((uint)PropType.PROP_PLAYER_SCOIN, GetMora, props);
            addProp((uint)PropType.PROP_IS_WORLD_ENTERABLE, 1, props);
            return props;
        }
        public void AddItem(GameItem item, ItemAddReasonType type= ItemAddReasonType.ItemAddReasonTrifle)
        {
            if (item.GetExcel().itemType == ItemType.ITEM_MATERIAL || item.GetExcel().itemType == ItemType.ITEM_VIRTUAL)
            {
                bool found = inventory.Find(i => i.id == item.id) != null;
                ItemAddHintNotify addHintNotify = new ItemAddHintNotify()
                {
                    Reason = (uint)type,
                    ItemList = { new ItemHint() { Count = (uint)item.amount, IsNew = !found, ItemId = item.id } }
                };
                if (found)
                {
                    inventory.Find(i => i.id == item.id).amount += item.amount;
                }
                else
                {
                    inventory.Add(item);
                }
                SendPacket(CmdType.ItemAddHintNotify, addHintNotify);
            }
            else
            {
                bool found = inventory.Find(i => i.id == item.id) != null;
                ItemAddHintNotify addHintNotify = new ItemAddHintNotify()
                {
                    Reason = (uint)type,
                    ItemList = { new ItemHint() { Count = (uint)item.amount, IsNew = !found, ItemId = item.id } }
                };
                inventory.Add(item);
                SendPacket(CmdType.ItemAddHintNotify, addHintNotify);
            }
            SendInventory();
        }

        public void addProp(uint type, int value, MapField<uint, PropValue> map)
        {
            PropValue prop = new PropValue();
            prop.Val = value;
            prop.Type = type;
            prop.Ival = value;
            map.Add(type, prop);
        }

        public QuestManager GetQuestManager()
        {
            return questManager;
        }
        public void SendInventory()
        {
            PlayerStoreNotify n = new()
            {
                StoreType = StoreType.StorePack,
                WeightLimit = 999999999,
            };
            foreach (GameItem item in inventory)
            {
                n.ItemList.Add(item.toProtoItem());
            }
            SendPacket(CmdType.PlayerStoreNotify, n);
            RecalculateAdventureLevel();
            PlayerPropNotify props = new()
            {
                PropMap = { GetPlayerProps() }
            };
            SendPacket(CmdType.PlayerPropNotify, props);
        }
        public void RecalculateAdventureLevel()
        {
            int oldLevel = adventureLevel;

            PlayerLevelData levelData = Server.getResources().GetPlayerLevel(adventureLevel);

            
            if(GetExp >= levelData.exp && adventureLevel != 20)
            {
                adventureLevel += 1;
                
            }
            
        }
        public Avatar GetMainAvatar()
        {
            return avatars.Find(av => av.id == 10000005 || av.id == 10000007);
        }
        public void MergeUsingReflection(YPlayer other)
        {
            PropertyInfo[] properties = typeof(YPlayer).GetProperties();
            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    property.SetValue(this, property.GetValue(other));
                }
            }
        }
        public void InitiateAccount(string token)
        {
            world = new World(this);
            OpenStateUpdateNotify openStateNotify = new OpenStateUpdateNotify();
            AllSeenMonsterNotify allSeenMonsterNotify = new AllSeenMonsterNotify();
            //TODO loading account
            this.teamEntityId = ((uint)ProtEntityType.ProtEntityTeam << 24) + (uint)random.Next();
            this.uid = 1;
            this.token = token;
            name = "Traveler";
            
            PlayerDataNotify playerDataNotify = new PlayerDataNotify()
            {
                NickName = name,
                ServerTime = 0,

            };
            playerDataNotify.PropMap.Add(GetPlayerProps());
            foreach (OpenStateType state in Enum.GetValues(typeof(OpenStateType)))
            {
                openStateMap[(uint)state] = 1;
            }
            SendPacket(CmdType.PlayerDataNotify, playerDataNotify);

            foreach (KeyValuePair<uint, uint> state in openStateMap)
            {
                openStateNotify.OpenStateMap.Add(state.Key, state.Value);
            }
           /* foreach (ItemData itemData in Server.getResources().itemData.Values)
            {
                if (itemData.itemType == ItemType.ITEM_MATERIAL || itemData.itemType == ItemType.ITEM_WEAPON)
                {
                    GameItem it = new GameItem(this, (uint)itemData.id);
                    it.level = 20;
                    it.promoteLevel = 0;
                    inventory.Add(it);
                }

            }*/
            foreach (uint monsterId in Server.getResources().monsterDataDict.Keys)
            {
                allSeenMonsterNotify.MonsterIdList.Add(monsterId);
            }
            //For testing fast
            // avatars.Add(new Avatar(this, 10000016));

            /*foreach (var avatar in Server.getResources().avatarsData)
            {
                if (avatar.id == 10000005 || avatar.id == 10000007 || avatar.id >= 11000000)
                {
                    continue;
                }
                avatars.Add(new Avatar(this, avatar.id));
            }

            // Find the avatar with the id of the first avatar in the team, and get its guid
            selectedAvatar = (int)avatars.FirstOrDefault((avatar) => avatar.id == team.First()).guid;*/
            // selectedAvatar = (int)avatars[0].guid;
            // for cooking stuff
            CookDataNotify cookDataNotify = new();
            foreach (CookRecipeExcel recipe in Server.getResources().cookRecipeDict.Values)
            {
                cookDataNotify.RecipeDataList.Add(new CookRecipeData()
                {
                    RecipeId = recipe.id,
                    Proficiency = recipe.maxProficiency
                });
            }
            CompoundDataNotify compoundDataNtf = new();
            foreach (CompoundExcel compound in Server.getResources().compoundDict.Values)
            {
                compoundDataNtf.UnlockCompoundList.Add(compound.id);
            }
            questManager = new(this);
            if (questManager.mainQuests.Find(q=>q.id==351) == null && Server.config.QuestEnabled)
            {
                GameQuest quest = questManager.AddQuest(35104);
                if(quest != null)
                {
                    quest.finish();
                }
                //Go to Paimon
                questManager.AddQuest(35100);
                
            }
            SendPacket(CmdType.CookDataNotify, cookDataNotify);
            SendPacket(CmdType.CompoundDataNotify, compoundDataNtf);
            SendInventory();
            SendAllAvatars();
           
            SendPacket(CmdType.OpenStateUpdateNotify, openStateNotify);
            SendPacket(CmdType.AllSeenMonsterNotify, allSeenMonsterNotify);
        }

       

        public void TeleportToScene(uint scene, Vector newPos = null, Vector newRot = null, EnterType enterType = EnterType.EnterJump)
        {
            prevSceneId = currentSceneId;
            Vector prevPos = motionInfo.Pos;
            if (newPos != null)
            {
                motionInfo.Pos = newPos;
                if (newRot != null) motionInfo.Rot = newRot;
            }
            else
            {
                ResourceLoader loader = new(Server.getResources());
                SceneExcel sceneEx = loader.LoadSceneLua(scene);
                motionInfo.Pos = sceneEx.bornPos;
                motionInfo.Rot = sceneEx.bornRot;
            }

            SendPacket(CmdType.PlayerEnterSceneNotify, new PlayerEnterSceneNotify() { SceneId = scene, TargetUid = uid, PrevPos = prevPos, Pos = motionInfo.Pos, PrevSceneId = prevSceneId, Type = enterType, SceneBeginTime = 0 });
            currentSceneId = scene;
            world.LoadNewScene(currentSceneId);
        }

        public uint GetCurrentAvatar()
        {
            return (uint)selectedAvatar;
        }

        public void SendAllAvatars()
        {
            AvatarDataNotify notify = new AvatarDataNotify();
            AvatarTeam team = new AvatarTeam();
            foreach (uint avatarId in this.team)
            {
                Avatar av = avatars.Find(av => av.id == avatarId);
                if (av == null) continue;
                team.AvatarGuidList.Add(av.guid);
            }
            notify.AvatarTeamMap.Add(1, team);
            notify.CurAvatarTeamId = 1;
            notify.ChooseAvatarGuid = GetCurrentAvatar();
            foreach (Avatar avatar in avatars)
            {
                notify.AvatarList.Add(avatar.toProto());
            }

            SendPacket(CmdType.AvatarDataNotify, notify);

        }

        public void Update()
        {

        }
        public void SendPacket(Packet packet)
        {
            SendPacket((uint)packet.cmdId, packet.ori);
        }
        public void SendPacket(CmdType cmdId, IMessage protoMessage)
        {
            SendPacket((uint)cmdId, protoMessage);
        }
        public void SendPacket(uint cmdId, IMessage protoMessage)
        {
            IntPtr packet = Packet.EncodePacket((ushort)cmdId, protoMessage);
            if (enet_peer_send(peer, 0, packet) == 0)
            {
                if (!Server.hideLog.Contains((CmdType)cmdId) && Server.showLogs == true)
                {
                    Server.Print($"[{Server.ColoredText("server", "03fc4e")}->{Server.ColoredText("client", "fcc603")}] {((CmdType)cmdId).ToString()} body: {protoMessage.ToString().Pastel(Color.FromArgb(165, 229, 250))}");
                }
                Logger.Log($"[server->client] {((CmdType)cmdId).ToString()} body: {protoMessage.ToString()}");
            }
        }

        public void SpawnElfie()
        {
            SceneEntityAppearNotify appear = new()
            {
                AppearType = VisionType.VisionMeet,
            };
            appear.EntityList.Add(new SceneEntityInfo()
            {
                EntityId = ((uint)ProtEntityType.ProtEntityNpc << 24) + (uint)random.Next(),
                EntityType = ProtEntityType.ProtEntityNpc,
                LifeState = (uint)LifeState.LIFE_ALIVE,
                MotionInfo = this.motionInfo,
                Npc = new SceneNpcInfo()
                {
                    NpcId = 1469
                }
            });
            SendPacket((uint)CmdType.SceneEntityAppearNotify, appear);
        }

        public void OnEnterRegion(SceneRegion region)
        {
            var enterRegionName = "ENTER_REGION_" + region.config_id;
            this.GetQuestManager().GetAllQuests().FindAll(q=>q.state==QuestState.UNFINISHED).ForEach(quest =>
            {
                if (quest.triggerData != null &&
                    quest.triggers.ContainsKey(enterRegionName))
                {
                    // If the trigger hasn't been fired yet
                    if (quest.triggers[enterRegionName] != true)
                    {
                        quest.triggers[enterRegionName] = true;
                        GetQuestManager().TriggerEvent(QuestContent.QUEST_CONTENT_TRIGGER_FIRE, quest.triggerData[enterRegionName].id);
                    }
                }
            });
        }

        public bool UnlockTransPoint(uint sceneId, uint pointId, bool isStatue)
        {
            unlockedPoints.Add(pointId);
            GetQuestManager().TriggerEvent(QuestContent.QUEST_CONTENT_UNLOCK_TRANS_POINT, sceneId, pointId);
           // this.world.callEvent(new ScriptArgs(0, EventType., sceneId, pointId));
           

            ScenePointUnlockNotify notify = new() { SceneId = sceneId,PointList = { pointId } };
            SendPacket(CmdType.ScenePointUnlockNotify, notify);
            return true;
        }

        public void UnlockSceneArea(uint sceneId, uint areaId)
        {
           if(!unlockedAreas.ContainsKey(areaId)) unlockedAreas.Add(areaId, sceneId);
            GetQuestManager().TriggerEvent(QuestContent.QUEST_CONTENT_UNLOCK_AREA, sceneId, areaId);
            SendPacket(CmdType.SceneAreaUnlockNotify,new SceneAreaUnlockNotify() { SceneId = sceneId, AreaList = { areaId } });
        }

        //Need to create a separated class?
        public void AddQuestProgress(uint id, uint newCount)
        {
            //TODO save in a dic or mapfield
            GetQuestManager().TriggerEvent(QuestContent.QUEST_CONTENT_ADD_QUEST_PROGRESS, id, newCount);
        }

        public YPlayer(IntPtr iD)
        {
            this.peer = iD;
            // this.gamePeer = (int)peer;
        }
    }
}
