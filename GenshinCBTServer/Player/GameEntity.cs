﻿using GenshinCBTServer.Controllers;
using GenshinCBTServer.Protocol;
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GenshinCBTServer.ResourceManager;

namespace GenshinCBTServer.Player
{
    public class GameEntity
    {
        public ProtEntityType EntityType = ProtEntityType.ProtEntityNone;
        public uint entityId;
        public uint id;
        public MotionInfo motionInfo;
        public MapField<uint, float> fightprops = new MapField<uint, float>();
        public MapField<uint, PropValue> props = new MapField<uint, PropValue>();
        public uint configId, groupId,owner,state,drop_id,sceneId;
        public int amount;
        public List<uint> inRegions = new List<uint>();

        public GameEntity(uint entityId, uint id, MotionInfo motionInfo, ProtEntityType entityType = ProtEntityType.ProtEntityNone)
        {
            EntityType = entityType;
            this.entityId = entityId;
            this.id = id;
            this.motionInfo = motionInfo;
        }

        public GameEntity(YPlayer client,CreateEntityInfo entity)
        {
            if(entity.EntityCase== CreateEntityInfo.EntityOneofCase.NpcId)
            {
                this.id = entity.NpcId;
                this.EntityType = ProtEntityType.ProtEntityNpc;
                this.motionInfo = new MotionInfo() { Pos = entity.Pos, Rot = entity.Rot };
                

            }
            uint entityId = ((uint)EntityType << 24) + (uint)client.random.Next();
            this.entityId = entityId;
            this.owner = (uint)client.gamePeer;
            sceneId = entity.SceneId;
        }

        public void FightPropUpdate(FightPropType key, float value)
        {
            fightprops[(uint)key] = value;
        }
        public bool died = false;
        public void Die()
        {
            YPlayer client = GetClientOwner();
            if (!died)
            {
               
                Server.Print("Calling monster lua");
                died = true;
                new Thread(new ThreadStart(dieStart)).Start();

               
            }
            
        }
        public virtual bool onInteract(YPlayer session, GadgetInteractReq req)
        {

            return false;
        }
        public SceneGroup GetGroup()
        {
            return GetClientOwner().world.currentBlock.groups.Find(g => g.id == groupId);
        }
        private void dieStart()
        {
            YPlayer client = GetClientOwner();
            SendUpdatedProps();
            /*
            EvtEntityStartDieEndNotify evtEntityStartDieEndNotify = new EvtEntityStartDieEndNotify()
            {
                ForwardType = ForwardType.ForwardToAll,
                Immediately = false,
                EntityId = entityId,
                DieStateFlag = 1,
            };
            client.SendPacket((uint)CmdType.EvtEntityStartDieEndNotify, evtEntityStartDieEndNotify);
            */
            Task.Delay(TimeSpan.FromSeconds(0.3));
            client.world.KillEntities(new() { this });

            DropList dropList = Server.getResources().GetRandomDrops(GetClientOwner(), this.drop_id, motionInfo);
            foreach (GameEntity en in dropList.entities)
            {
                GetClientOwner().world.SpawnEntity(en, true, VisionType.VisionReborn);
            }
            if (EntityType == ProtEntityType.ProtEntityMonster)
            {
                LuaManager.executeTriggersLua(
                    GetClientOwner(),
                    GetClientOwner().world.currentBlock.groups.Find(g => g.id == groupId),
                    new ScriptArgs((int)groupId, (int)EventType.EVENT_ANY_MONSTER_DIE)
                );

                client.world.monsterDieCount++;
            } else if (EntityType == ProtEntityType.ProtEntityGadget)
            {
                LuaManager.executeTriggersLua(
                    GetClientOwner(),
                    GetClientOwner().world.currentBlock.groups.Find(g => g.id == groupId),
                    new ScriptArgs((int)groupId, (int)EventType.EVENT_ANY_GADGET_DIE)
                );
            }
        }
        public virtual void InitProps()
        {
            FightPropUpdate(FightPropType.FIGHT_PROP_BASE_HP, 1);
            FightPropUpdate(FightPropType.FIGHT_PROP_BASE_DEFENSE, 1);
            FightPropUpdate(FightPropType.FIGHT_PROP_BASE_ATTACK, 1);
            FightPropUpdate(FightPropType.FIGHT_PROP_ATTACK, 1);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_ATTACK, 1); //TODO calculate total attack
            FightPropUpdate(FightPropType.FIGHT_PROP_HP, 1);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_HP, 1);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_HP, 1); //TODO calculate total hp
            FightPropUpdate(FightPropType.FIGHT_PROP_HP_PERCENT, 0);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_DEFENSE, 123456.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_SPEED, 0.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_FIRE_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_ELEC_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_WATER_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_GRASS_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_WIND_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_ICE_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_CUR_ROCK_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_FIRE_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_ELEC_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_WATER_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_GRASS_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_WIND_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_ICE_ENERGY, 100.0f);
            FightPropUpdate(FightPropType.FIGHT_PROP_MAX_ROCK_ENERGY, 100.0f);
            props[(uint)PropType.PROP_EXP] = new PropValue() { Ival = 1, Val = 1, Type = (uint)PropType.PROP_EXP };
            props[(uint)PropType.PROP_LEVEL] = new PropValue() { Ival = 1, Val = (long)1, Type = (uint)PropType.PROP_LEVEL };

        }
        //TODO get all clients in a world (for future multiplayer)
        public YPlayer GetClientOwner()
        {
            return Server.clients.Find(client => client.gamePeer == owner);
        }
        public void SendUpdatedProps()
        {
            YPlayer client = GetClientOwner();
            //UpdateProps();
            client.SendPacket((uint)CmdType.EntityFightPropUpdateNotify, new EntityFightPropUpdateNotify()
            {
                EntityId = entityId,
                FightPropMap = { fightprops }
                
            });
            client.SendPacket((uint)CmdType.EntityFightPropChangeReasonNotify, new EntityFightPropChangeReasonNotify()
            {
                EntityId = entityId,
                PropType=(uint)FightPropType.FIGHT_PROP_CUR_HP,
                PropDelta=GetFightProp(FightPropType.FIGHT_PROP_CUR_HP),
                Reason=PropChangeReason.PropChangeAbility,
                
            });
        }
        public float GetFightProp(FightPropType propType)
        {
            return fightprops[(uint)propType];
        }
        public void MoveEntity(MotionInfo motionInfo, bool notify = false)
        {
            this.motionInfo = motionInfo;
            if (notify)
            {

                SceneEntityMoveNotify n = new() { EntityId = this.entityId, MotionInfo = motionInfo };
                Server.clients.Find(client => client.gamePeer == owner).SendPacket((uint)CmdType.SceneEntityMoveNotify, n);
            }

        }
        public virtual SceneEntityInfo asInfo()
        {
            SceneEntityInfo info = new SceneEntityInfo()
            {
                EntityType = EntityType,
                EntityId = entityId,
                MotionInfo = motionInfo,
                LifeState = 1,

               // EntityCase = SceneEntityInfo.EntityOneofCase.Gadget
            };
            if (EntityType == ProtEntityType.ProtEntityNpc)
            {
                info.Npc = new SceneNpcInfo()
                {
                    NpcId=id,
                };
            }

            return info;
        }
    }

    public enum EntityType
    {
        None = 0,
        Avatar = 1,
        Monster = 2,
        Bullet = 3,
        AttackPhyisicalUnit = 4,
        AOE = 5,
        Camera = 6,
        EnviroArea = 7,
        Equip = 8,
        MonsterEquip = 9,
        Grass = 10,
        Level = 11,
        NPC = 12,
        TransPointFirst = 13,
        TransPointFirstGadget = 14,
        TransPointSecond = 15,
        TransPointSecondGadget = 16,
        DropItem = 17,
        Field = 18,
        Gadget = 19,
        Water = 20,
        GatherPoint = 21,
        GatherObject = 22,
        AirflowField = 23,
        SpeedupField = 24,
        Gear = 25,
        Chest = 26,
        EnergyBall = 27,
        ElemCrystal = 28,
        Timeline = 29,
        Worktop = 30,
        Team = 31,
        Platform = 32,
        AmberWind = 33,
        EnvAnimal = 34,
        SealGadget = 35,
        Tree = 36,
        Bush = 37,
        PlaceHolder = 99,
    }
}
