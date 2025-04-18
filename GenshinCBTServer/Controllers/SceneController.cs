﻿using GenshinCBTServer.Network;
using GenshinCBTServer.Player;
using GenshinCBTServer.Protocol;
using GenshinCBTServer.Data;
using GenshinCBTServer.Excel;
using System.Diagnostics;

namespace GenshinCBTServer.Controllers
{
    public class SceneController
    {
        [Server.Handler(CmdType.WorldPlayerReviveReq)]
        public static void OnWorldPlayerReviveReq(YPlayer session, CmdType cmdId, Packet packet)
        {
            WorldPlayerReviveReq req = packet.DecodeBody<WorldPlayerReviveReq>();

            for (int i = 0; i < session.team.Length; i++)
            {
                Avatar av = session.avatars.Find(av => av.id == session.team[i]);
                if (av != null)
                {
                    av.curHp = av.GetFightProp(FightPropType.FIGHT_PROP_MAX_HP) / 3;
                    av.SendUpdatedProps();
                    session.SendAllAvatars();

                }
            }
            ScenePoint point = Server.getResources().scenePointDict[session.currentSceneId].points.Values.First();
            session.TeleportToScene(session.currentSceneId, point.tranPos, point.tranRot, EnterType.EnterGoto);
        }
        [Server.Handler(CmdType.ClientScriptEventNotify)]
        public static void OnClientScriptEventNotify(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            try
            {
                ClientScriptEventNotify req = packet.DecodeBody<ClientScriptEventNotify>();
                ScriptArgs args = new ScriptArgs(0, (int)req.EventType);
                args.source_eid = (int)req.SourceEntityId;
                args.target_eid = (int)req.TargetEntityId;
                for (int i = 0; i < req.ParamList.Count; i++)
                {
                    switch (i)
                    {
                        case 0: args.param1 = req.ParamList[i]; break;
                        case 1: args.param1 = req.ParamList[i]; break;
                        case 2: args.param1 = req.ParamList[i]; break;
                    }
                }
                GameEntity entity = session.world.entities.Find(e => e.entityId == req.SourceEntityId);
                Server.Print($"Test type: {req.EventType}, {req.SourceEntityId}, entity {(entity != null ? "Exist" : "Not exist")}");
                if (req.EventType == (uint)EventType.EVENT_AVATAR_NEAR_PLATFORM)
                {
                    if (req.ParamList[0] == 0)
                    {

                        if (entity != null)
                        {
                            args.param1 = (int)entity.configId;
                        }
                    }
                    Server.Print("Near platform");
                }
                if (entity != null)
                    LuaManager.executeTriggersLua(session, session.world.currentBlock.groups.Find(g => g.id == entity.groupId), args);
                if (entity != null && req.EventType == (uint)EventType.EVENT_AVATAR_NEAR_PLATFORM)
                    if (entity is GameEntityGadget)
                    {
                        GameEntityGadget gameEntityGadget = (GameEntityGadget)entity;
                        if (!gameEntityGadget.Route.IsStarted) gameEntityGadget.StartPlatform();
                    }
            }
            catch (Exception e)
            {
                Server.Print($"Failed to execute script event: {e.Message}");
            }
        }

        [Server.Handler(CmdType.MonsterAlertChangeNotify)]
        public static void OnMonsterAlertChangeNotify(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            MonsterAlertChangeNotify req = packet.DecodeBody<MonsterAlertChangeNotify>();
            foreach (GameEntity entity in session.world.entities.FindAll(e => req.MonsterEntityList.Contains(e.entityId)))
            {
                ScriptArgs args = new ScriptArgs((int)entity.groupId, (int)EventType.EVENT_MONSTER_BATTLE, (int)entity.configId);
                session.world.callEvent(args);
            }
        }
        [Server.Handler(CmdType.ExecuteGadgetLuaReq)]
        public static void OnExecuteGadgetLuaReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            ExecuteGadgetLuaReq req = packet.DecodeBody<ExecuteGadgetLuaReq>();
            GameEntity entity = session.world.entities.Find(e => e.entityId == req.SourceEntityId);
            if (entity != null)
            {
                if (entity is GameEntityGadget)
                {
                    GameEntityGadget gadget = (GameEntityGadget)entity;
                    //  session.world.onClientExecuteRequest(gadget, req.Param1, req.Param2, req.Param3);
                    session.SendPacket((uint)CmdType.ExecuteGadgetLuaRsp, new ExecuteGadgetLuaRsp() { Retcode = 0 });
                }
            }
        }
        [Server.Handler(CmdType.SceneEntityDrownReq)]
        public static void OnSceneEntityDrownReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            SceneEntityDrownReq req = packet.DecodeBody<SceneEntityDrownReq>();
        }
        [Server.Handler(CmdType.PersonalSceneJumpReq)]
        public static void OnPersonalSceneJumpReq(YPlayer session, CmdType cmdId, Packet packet)
        {
            PersonalSceneJumpReq req = packet.DecodeBody<PersonalSceneJumpReq>();
            ScenePointRow pointRow = Server.getResources().scenePointDict[session.currentSceneId];
            if (pointRow.points.ContainsKey(req.PointId))
            {
                session.TeleportToScene(pointRow.points[req.PointId].tranSceneId, pointRow.points[req.PointId].tranPos, pointRow.points[req.PointId].tranRot, EnterType.EnterJump);
                PersonalSceneJumpRsp rsp = new()
                {
                    DestPos = pointRow.points[req.PointId].tranPos,
                    DestSceneId = pointRow.points[req.PointId].tranSceneId,
                    Retcode = 0
                };

                session.SendPacket((uint)CmdType.PersonalSceneJumpRsp, rsp);
            }
        }
        [Server.Handler(CmdType.UnlockTransPointReq)]
        public static void OnUnlockTransPointReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            UnlockTransPointReq req = packet.DecodeBody<UnlockTransPointReq>();
           
            UnlockTransPointRsp rsp_ = new() { Retcode = 0};

            session.UnlockTransPoint(req.SceneId, req.PointId, false);
            session.SendPacket((uint)CmdType.UnlockTransPointRsp, rsp_);
        }
        [Server.Handler(CmdType.EnterSceneReadyReq)]
        public static void OnEnterSceneReadyReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            EnterSceneReadyReq req = packet.DecodeBody<EnterSceneReadyReq>();
            EnterScenePeerNotify enterscenepeernotify = new EnterScenePeerNotify()
            {
                DestSceneId = session.currentSceneId,
                HostPeerId = (uint)session.gamePeer,
                PeerId = (uint)session.gamePeer
            };

            session.SendPacket((uint)CmdType.EnterScenePeerNotify, enterscenepeernotify);
            session.SendPacket((uint)CmdType.EnterSceneReadyRsp, new EnterSceneReadyRsp() { Retcode = 0 });
        }


        [Server.Handler(CmdType.SceneEntityMoveReq)]
        public static void OnSceneEntityMoveReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            SceneEntityMoveReq req = packet.DecodeBody<SceneEntityMoveReq>();

            Avatar avatar = session.avatars.Find(av => av.entityId == req.EntityId);
            if (avatar != null)
            {
                if (req.MotionInfo.Pos.X == 0 && req.MotionInfo.Pos.Y == 0 && req.MotionInfo.Pos.Z == 0)
                {
                    //Client bug when jump
                }
                else
                {
                    session.motionInfo = req.MotionInfo;
                    session.world.UpdateBlocks();
                }
                // Server.Print($"Client Pos: {session.motionInfo.Pos.X}, {session.motionInfo.Pos.Y}, {session.motionInfo.Pos.Z}");
            }
            else
            {
                GameEntity entity = session.world.entities.Find(entity => entity.entityId == req.EntityId);
                if (entity is GameEntityMonster)
                {
                    entity.MoveEntity(req.MotionInfo);
                }
            }
            session.SendPacket((uint)CmdType.SceneEntityMoveRsp, new SceneEntityMoveRsp() { Retcode = 0, EntityId = req.EntityId, FailMotion = req.MotionInfo, SceneTime = req.SceneTime, ReliableSeq = req.ReliableSeq });
        }

        [Server.Handler(CmdType.ChangeAvatarReq)]
        public static void OnChangeAvatarReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {

            ChangeAvatarReq req = packet.DecodeBody<ChangeAvatarReq>();
            SwitchAvatar(session, (uint)req.Guid);
            ChangeAvatarRsp rsp = new ChangeAvatarRsp() { CurGuid = req.Guid, Retcode = 0 };
            session.SendPacket((uint)CmdType.ChangeAvatarRsp, rsp);

        }
        [Server.Handler(CmdType.GadgetInteractReq)]
        public static void OnGadgetInteractReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {

            GadgetInteractReq req = packet.DecodeBody<GadgetInteractReq>();
            GameEntity entity_ = session.world.entities.Find(entity => entity.entityId == req.GadgetEntityId);

            if (entity_ != null)
            {
                if (entity_.onInteract(session, req))
                {
                    Server.Print("Gadget interacted successfully");
                }
            }
            //To be removed
            /*switch(entity_) {
                case GameEntityGadget entity:
                {
                    //TODO GameEntityGadget.OnInteract()
                    Server.Print("Type: " + entity.gadgetType);
                    if (entity.chest_drop > 0)
                    {
                        entity.ChangeState(GadgetState.ChestOpened);
                        session.world.KillEntities(new List<GameEntity>() { entity }, VisionType.VisionNone);
                        DropList dropList = Server.getResources().GetRandomDrops(session, entity.chest_drop, entity.motionInfo);
                        foreach (GameEntity en in dropList.entities)
                        {
                            session.world.SpawnEntity(en, true, VisionType.VisionReborn);
                        }
                        session.SendPacket((uint)CmdType.GadgetInteractRsp, new GadgetInteractRsp() { Retcode = (int)0, GadgetEntityId = req.GadgetEntityId, GadgetId = entity.id, InteractType = InteractType.InteractOpenChest, OpType = InterOpType.InterOpFinish });
                                LuaManager.executeTriggersLua(session, entity.GetGroup(), new ScriptArgs((int)entity.groupId, (int)EventType.EVENT_GATHER,(int)entity.configId));
                    }
                    break;
                }
                case GameEntityItem entity:
                {
                    session.world.KillEntities(new List<GameEntity>() { entity }, VisionType.VisionNone);
                    session.AddItem(entity.item);
                    session.SendPacket((uint)CmdType.GadgetInteractRsp, new GadgetInteractRsp() { Retcode = (int)0, GadgetEntityId = req.GadgetEntityId, GadgetId = entity.id, InteractType = InteractType.InteractPickItem, OpType = InterOpType.InterOpStart });
                    break;
                }
                case GameEntityMonster entity:
                {
                    session.world.KillEntities(new List<GameEntity>() { entity }, VisionType.VisionNone);
                    DropList dropList = Server.getResources().GetRandomDrops(session, entity.drop_id, entity.motionInfo);
                    foreach (GameEntity en in dropList.entities)
                    {
                        if(en is GameEntityItem)
                        {
                            GameEntityItem eni = (GameEntityItem)en;
                            session.AddItem(eni.item);
                        }
                        
                    }
                    session.SendPacket((uint)CmdType.GadgetInteractRsp, new GadgetInteractRsp() { Retcode = (int)0, GadgetEntityId = req.GadgetEntityId, GadgetId = entity.id, InteractType = InteractType.InteractGather, OpType = InterOpType.InterOpStart });
                }
                break;
            }*/
        }

        [Server.Handler(CmdType.SceneTransToPointReq)]
        public static void OnSceneTransToPointReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            SceneTransToPointReq req = packet.DecodeBody<SceneTransToPointReq>();
            ScenePointRow pointRow = Server.getResources().scenePointDict[3];
            if (pointRow == null)
            {
                Server.Print($"Point {req.PointId} not found");
                session.SendPacket((uint)CmdType.SceneTransToPointRsp, new SceneTransToPointRsp() { Retcode = (int)Retcode.RetFail });
                return;
            }
            MotionInfo newMotion;
            Dictionary<uint, ScenePoint> points = pointRow.points;
            ScenePoint point = points[req.PointId];
            Vector prevPos = session.motionInfo.Pos;
            switch (point.JsonObjType)
            {
                case "DungeonEntry":
                case "DungeonExit":
                    newMotion = new MotionInfo()
                    {
                        Pos = new Vector() { X = point.pos.X, Y = point.pos.Y, Z = point.pos.Z },
                        Rot = point.rot,
                        State = MotionState.MotionFallOnGround,
                        Speed = new Vector(),
                    };
                    break;
                case "SceneTransPoint":
                    newMotion = new MotionInfo()
                    {
                        Pos = new Vector() { X = point.tranPos.X, Y = point.tranPos.Y, Z = point.tranPos.Z },
                        Rot = point.tranRot,
                        State = MotionState.MotionFallOnGround,
                        Speed = new Vector(),
                    };
                    if (point.tranPos == null || point.tranPos.X == 0 && point.tranPos.Y == 0 && point.tranPos.Z == 0)
                    {
                        Server.Print($"Point {req.PointId} has no tranPos, using pos instead");
                        newMotion.Pos = new Vector() { X = point.pos.X, Y = point.pos.Y, Z = point.pos.Z };
                    }
                    break;
                default:
                    Server.Print($"Unhandled ScenePoint type {point.JsonObjType} for scene point {req.PointId}");
                    session.SendPacket((uint)CmdType.SceneTransToPointRsp, new SceneTransToPointRsp() { Retcode = (int)Retcode.RetFail });
                    return;
            }
            Server.Print($"Teleporting to {req.PointId} at {session.motionInfo.Pos.X},{session.motionInfo.Pos.Y},{session.motionInfo.Pos.Z}");
            Server.Print($"Teleporting to {req.PointId} at {newMotion.Pos.X},{newMotion.Pos.Y},{newMotion.Pos.Z}");
            session.TeleportToScene(3, newMotion.Pos, newMotion.Rot, EnterType.EnterGoto);
            // session.SendPacket((uint)CmdType.PlayerEnterSceneNotify, new PlayerEnterSceneNotify() { SceneId = session.currentSceneId,PrevPos= prevPos, Pos=session.motionInfo.Pos,PrevSceneId= 0, Type=EnterType.EnterGoto,SceneBeginTime=0 });
            // session.SendPacket((uint)CmdType.ScenePlayerLocationNotify, new ScenePlayerLocationNotify() { PlayerLocList = { new PlayerLocationInfo() { Uid = session.uid, Pos = session.motionInfo.Pos, Rot = session.motionInfo.Rot } } });
            session.SendPacket((uint)CmdType.SceneTransToPointRsp, new SceneTransToPointRsp() { PointId = req.PointId, SceneId = 3, Retcode = 0 });
            // session.world.UpdateBlocks();
        }

        [Server.Handler(CmdType.EnterSceneDoneReq)]
        public static void OnEnterSceneDoneReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            EnterSceneDoneReq req = packet.DecodeBody<EnterSceneDoneReq>();

            Avatar NewAv = session.avatars.Find(av => av.guid == session.GetCurrentAvatar());
            SceneEntityAppearNotify appearNotify = new SceneEntityAppearNotify()
            {
                Param = NewAv.asInfo().EntityId,
                EntityList = { NewAv.asInfo() },
                AppearType = VisionType.VisionNone

            };
            session.SendPacket((uint)CmdType.SceneEntityAppearNotify, appearNotify);
            ScenePlayerLocationNotify locNotify = new() { PlayerLocList = { new PlayerLocationInfo() { Uid = session.uid, Pos = session.motionInfo.Pos, Rot = session.motionInfo.Rot } } };
            session.SendPacket((uint)CmdType.ScenePlayerLocationNotify, locNotify);

            session.SendPacket((uint)CmdType.EnterSceneDoneRsp, new EnterSceneDoneRsp() { Retcode = 0 });
            session.world.UpdateBlocks();
            //session.world.SendAllEntities();
        }

        [Server.Handler(CmdType.PlayerSetPauseReq)]
        public static void OnPlayerSetPauseReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            // later should implement the pause on server side (for specific props, countdowns etc)
            PlayerSetPauseReq req = packet.DecodeBody<PlayerSetPauseReq>();
            PlayerSetPauseRsp rsp = new PlayerSetPauseRsp() { Retcode = 0 };
            session.SendPacket((uint)CmdType.PlayerSetPauseRsp, rsp);
        }

        [Server.Handler(CmdType.EvtDestroyGadgetNotify)]
        public static void OnEvtDestroyGadgetNotify(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            EvtDestroyGadgetNotify req = packet.DecodeBody<EvtDestroyGadgetNotify>();
            GameEntity? entity = session.world.entities.Find(entity => entity.entityId == req.EntityId);
            if (entity != null)
            {
                session.world.KillEntities(new List<GameEntity>() { entity }, VisionType.VisionNone);
            }
        }

        [Server.Handler(CmdType.EvtCreateGadgetNotify)]
        public static void OnEvtCreateGadgetNotify(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            EvtCreateGadgetNotify req = packet.DecodeBody<EvtCreateGadgetNotify>();
        }

        [Server.Handler(CmdType.SetOpenStateReq)]
        public static void OnSetOpenStateReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {

            SetOpenStateReq req = packet.DecodeBody<SetOpenStateReq>();
            SetOpenStateRsp rsp = new SetOpenStateRsp() { Key = req.Key, Value = req.Value, Retcode = 0 };
            session.SendPacket((uint)CmdType.SetOpenStateRsp, rsp);

        }

        [Server.Handler(CmdType.EnterWorldAreaReq)]
        public static void OnEnterWorldAreaReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {

            EnterWorldAreaReq req = packet.DecodeBody<EnterWorldAreaReq>();
            EnterWorldAreaRsp rsp = new EnterWorldAreaRsp() { AreaId = req.AreaId, AreaType = req.AreaType, Retcode = 0 };
            session.SendPacket((uint)CmdType.EnterWorldAreaRsp, rsp);

        }

        [Server.Handler(CmdType.SceneGetAreaExplorePercentReq)]
        public static void OnSceneGetAreaExplorePercentReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {

            SceneGetAreaExplorePercentReq req = packet.DecodeBody<SceneGetAreaExplorePercentReq>();
            SceneGetAreaExplorePercentRsp rsp = new SceneGetAreaExplorePercentRsp() { AreaId = req.AreaId, ExplorePercent = 1f, Retcode = 0 };
            session.SendPacket((uint)CmdType.SceneGetAreaExplorePercentRsp, rsp);

        }

        [Server.Handler(CmdType.GetScenePointReq)]
        public static void OnGetScenePointReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            GetScenePointReq req = packet.DecodeBody<GetScenePointReq>();
            GetScenePointRsp rsp = new GetScenePointRsp()
            {
                SceneId = req.SceneId,
                BelongUid = req.BelongUid,
                Retcode = 0,
            };
            for (int i = 0; i < 200; i++) // todo: get from scene{player.pos.scenepoint}_point.json
            {
               // rsp.UnlockAreaList.Add((uint)i);
               // rsp.UnlockedPointList.Add((uint)i);
            }
            rsp.UnlockAreaList.Add(session.unlockedAreas.Keys.ToList());
            rsp.UnlockedPointList.Add(session.unlockedPoints);
            session.SendPacket((uint)CmdType.GetScenePointRsp, rsp);
        }
        [Server.Handler(CmdType.GetSceneAreaReq)]
        public static void OnGetSceneAreaReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            GetSceneAreaReq req = packet.DecodeBody<GetSceneAreaReq>();
            GetSceneAreaRsp rsp = new GetSceneAreaRsp()
            {
                SceneId = req.SceneId,
                Retcode = 0,

            };
            rsp.AreaIdList.Add(session.unlockedAreas.Keys.ToList());
            rsp.CityInfoList.Add(new CityInfo()
            {
                CityId = 1,
                Level = 1,
                CrystalNum = 10
            });
            session.SendPacket((uint)CmdType.GetSceneAreaRsp, rsp);
        }
        [Server.Handler(CmdType.SceneInitFinishReq)]
        public static void OnSceneInitFinishReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {

            SceneInitFinishReq req = packet.DecodeBody<SceneInitFinishReq>();
            ScenePlayerInfoNotify sceneplayerinfonotify = new()
            {

            };
            sceneplayerinfonotify.PlayerInfoList.Add(new ScenePlayerInfo()
            {
                IsConnected = true,
                Uid = session.uid,
                Name = session.name,
                PeerId = (uint)session.gamePeer
            });

            session.SendPacket((uint)CmdType.ScenePlayerInfoNotify, sceneplayerinfonotify);

            // SendSceneTeamUpdate(session);

            SendEnterSceneInfo(session);
            //Send GameTime
            PlayerGameTimeNotify playerGameTimeNotify = new()
            {
                GameTime = 5 * 60 * 60,
                Uid = session.uid,
            };
            session.SendPacket((uint)CmdType.PlayerGameTimeNotify, playerGameTimeNotify);
            //Send SceneTime
            SceneTimeNotify sceneTimeNotify = new()
            {
                SceneId = session.currentSceneId,
                SceneTime = 9000
            };

            session.SendPacket((uint)CmdType.SceneTimeNotify, sceneTimeNotify);
            HostPlayerNotify hostplayernotify = new() { HostUid = session.uid, HostPeerId = (uint)session.gamePeer };
            session.SendPacket((uint)CmdType.HostPlayerNotify, hostplayernotify);
            session.SendPacket((uint)CmdType.SceneInitFinishRsp, new SceneInitFinishRsp() { Retcode = 0 });

        }
        [Server.Handler(CmdType.SetUpAvatarTeamReq)]
        public static void OnSetUpAvatarTeamReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            SetUpAvatarTeamReq req = packet.DecodeBody<SetUpAvatarTeamReq>();

            uint[] team = new uint[req.AvatarTeam.AvatarGuidList.Count];
            int i = 0;
            foreach (uint g in req.AvatarTeam.AvatarGuidList)
            {
                team[i] = session.avatars.Find(av => av.guid == g).id;
                i++;
            }
            session.team = team;
            SendSceneTeamUpdate(session);

        }

        [Server.Handler(CmdType.GetCompoundDataReq)]
        public static void OnGetCompoundDataReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            GetCompoundDataRsp rsp = new GetCompoundDataRsp();
            foreach (CompoundExcel compound in Server.getResources().compoundDict.Values)
            {
                rsp.UnlockCompoundList.Add(compound.id);
            }
            session.SendPacket((uint)CmdType.GetCompoundDataRsp, rsp);
        }

        [Server.Handler(CmdType.PlayerCookReq)]
        public static void OnPlayerCookReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            PlayerCookReq req = packet.DecodeBody<PlayerCookReq>();
            CookRecipeExcel excel = Server.getResources().cookRecipeDict[req.RecipeId];
            PlayerCookRsp rsp = new()
            {
                Retcode = 0,
                QteQuality = req.QteQuality,
                RecipeData = new CookRecipeData()
                {
                    RecipeId = req.RecipeId,
                    Proficiency = excel.maxProficiency
                },
                Item = new ItemParam()
                {
                    ItemId = excel.qualityOutputVec.Last().id,
                    Count = excel.qualityOutputVec.Last().count
                }
            };
            session.SendPacket((uint)CmdType.GetCompoundDataRsp, rsp);
        }

        [Server.Handler(CmdType.PlayerCompoundMaterialReq)]
        public static void OnPlayerCompoundMaterialReq(YPlayer session, CmdType cmdId, Network.Packet packet)
        {
            PlayerCompoundMaterialReq req = packet.DecodeBody<PlayerCompoundMaterialReq>();
            CompoundExcel compound = Server.getResources().compoundDict[req.CompoundId];
            PlayerCompoundMaterialRsp rsp = new()
            {
                Retcode = 0,
                CompoundQueData = new CompoundQueueData()
                {
                    CompoundId = req.CompoundId,
                    OutputCount = compound.outputVec[0].count,
                    WaitCount = compound.costTime,
                    OutputTime = 9000 + compound.costTime, // TODO: sync with server time
                }
            };
            CompoundDataNotify ntf = new()
            {
                CompoundQueDataList = { rsp.CompoundQueData }
            };
            session.SendPacket((uint)CmdType.GetCompoundDataRsp, rsp);
            session.SendPacket((uint)CmdType.CompoundDataNotify, ntf);
        }

        public static void SendSceneTeamUpdate(YPlayer session)
        {

            AvatarTeamUpdateNotify sceneTeamUpdate = new();
            AvatarTeam team = new AvatarTeam();

            foreach (uint avatarId in session.team)
            {
                team.AvatarGuidList.Add(session.avatars.Find(av => av.id == avatarId).guid);
                sceneTeamUpdate.AvatarEntityIdMap.Add(session.avatars.Find(av => av.id == avatarId).guid, session.avatars.Find(av => av.id == avatarId).asInfo().EntityId);
            }
            // sceneTeamUpdate.AvatarTeamMap.Add(0, team);
            sceneTeamUpdate.AvatarTeamMap.Add(1, team);
            session.SendPacket((uint)CmdType.AvatarTeamUpdateNotify, sceneTeamUpdate);
            // session.SendAllAvatars();
            SetUpAvatarTeamRsp setUpAvatarTeamRsp = new SetUpAvatarTeamRsp()
            {
                AvatarTeam = team,
                Retcode = 0,
                TeamId = 1,
                CurAvatarGuid = session.avatars.Find(av => av.guid == session.GetCurrentAvatar()).guid
            };
            SwitchAvatar(session, session.GetCurrentAvatar());
            session.SendPacket((uint)CmdType.SetUpAvatarTeamRsp, setUpAvatarTeamRsp);
        }

        public static void SwitchAvatar(YPlayer session, uint guid)
        {
            Avatar prevAv = session.avatars.Find(av => av.guid == session.GetCurrentAvatar());
            session.selectedAvatar = (int)guid;
            Avatar NewAv = session.avatars.Find(av => av.guid == guid);

            SceneEntityDisappearNotify disappearNotify = new SceneEntityDisappearNotify()
            {
                EntityList = { prevAv.asInfo().EntityId },
                DisappearType = VisionType.VisionReplace
            };
            SceneEntityAppearNotify appearNotify = new SceneEntityAppearNotify()
            {
                EntityList = { NewAv.asInfo() },
                AppearType = VisionType.VisionReplace
            };
            session.SendPacket(CmdType.SceneEntityDisappearNotify, disappearNotify);
            session.SendPacket(CmdType.SceneEntityAppearNotify, appearNotify);
        }

        public static void SendEnterSceneInfo(YPlayer session)
        {
            PlayerEnterSceneInfoNotify playerEnterSceneInfoNotify = new()
            {
                CurAvatarEntityId = session.avatars.Find(av => av.guid == session.GetCurrentAvatar()).asInfo().EntityId,

                TeamEnterInfo = new()
                {
                    TeamEntityId = session.teamEntityId,
                    TeamAbilityInfo = new() { IsInited = false },

                }
            };
            foreach (uint avatarId in session.team)
            {
                Avatar avatar = session.avatars.Find(av => av.id == avatarId);
                if (avatar != null)

                    playerEnterSceneInfoNotify.AvatarEnterInfo.Add(new AvatarEnterSceneInfo()
                    {
                        AvatarEntityId = avatar.asInfo().EntityId,
                        WeaponEntityId = avatar.asInfo().Avatar.Weapon.EntityId,
                        AvatarGuid = avatar.guid,
                        WeaponGuid = avatar.weaponGuid,
                        WeaponAbilityInfo = new() { IsInited = false },
                        AvatarAbilityInfo = new() { IsInited = false },
                    });
            }

            session.SendPacket((uint)CmdType.PlayerEnterSceneInfoNotify, playerEnterSceneInfoNotify);
            // OnEnterSceneDoneReq(session, CmdType.EnterSceneDoneReq, new Packet() { cmdId=(uint) CmdType.EnterSceneDoneReq,finishedBody=new byte[] { } });
        }
    }
}
