
-- Beyond the Brave: modern script helpers implemented with classic core APIs.
-- Only missing helper names are added; existing scripts keep their behavior.
Synchro=Synchro or {NonTuner=function(...) return aux.NonTuner(...) end,AddProcedure=function(c,tf,tmin,tmax,nf,nmin,nmax) aux.AddSynchroProcedure(c,tf,nf,nmin,nmax) end}
Fusion=Fusion or {AddProcMix=function(...) return aux.AddFusionProcMix(...) end}
Link=Link or {AddProcedure=function(...) return aux.AddLinkProcedure(...) end}
Xyz=Xyz or {InfiniteMats=99,AddProcedure=function(c,f,lv,ct,af,ad,max) aux.AddXyzProcedure(c,f,lv,ct,af,ad,max) end}
CATEGORY_SET=0 -- Modern informational category exceeds the classic 32-bit category field.
TIMINGS_CHECK_MONSTER_E=0x1e0
LOCATION_STZONE=LOCATION_SZONE
PLAYER_EITHER=PLAYER_ALL
ZONES_MMZ=0x1f
HINTMSG_NEGATE=HINTMSG_FACEUP
SET_ADAMANCIPATOR=0x140
SET_ANGELECHY=0x1e2 -- native archetype namespace, not Babel's colliding 0x1dc
EFFECT_OPPO_CHOOSES_SPSUMMON_ZONE=267
LOCATION_REASON_SPSUMMON=0x11
Chain=Chain or {}
Chain.IsTriggeringLocation=Chain.IsTriggeringLocation or function(ev,loc) return Duel.GetChainInfo(ev,CHAININFO_TRIGGERING_LOCATION)&loc~=0 end
aux.FilterBoolFunctionEx=aux.FilterBoolFunctionEx or aux.FilterBoolFunction
aux.AND=aux.AND or function(...) local fs={...};return function(c,...) for _,f in ipairs(fs) do if not f(c,...) then return false end end return true end end
aux.SelectUnselectGroup=aux.SelectUnselectGroup or function(g,e,tp,min,max,rescon,mode,sp,hint)
 if mode==0 then return g:CheckSubGroup(function(sg) return rescon(sg,e,tp) end,min,max) end
 Duel.Hint(HINT_SELECTMSG,tp,hint)
 return g:SelectSubGroup(tp,function(sg) return rescon(sg,e,tp) end,false,min,max) or Group.CreateGroup()
end
Card.IsMonster=Card.IsMonster or function(c) return c:IsType(TYPE_MONSTER) end
Card.IsMonsterCard=Card.IsMonsterCard or function(c) return c:GetOriginalType()&TYPE_MONSTER~=0 end
Card.IsTrap=Card.IsTrap or function(c) return c:IsType(TYPE_TRAP) end
Card.IsSpellTrap=Card.IsSpellTrap or function(c) return c:IsType(TYPE_SPELL|TYPE_TRAP) end
Card.IsNormalTrap=Card.IsNormalTrap or function(c) return c:IsType(TYPE_TRAP) and not c:IsType(TYPE_CONTINUOUS|TYPE_COUNTER) end
Card.IsContinuousSpell=Card.IsContinuousSpell or function(c) return c:IsType(TYPE_SPELL) and c:IsType(TYPE_CONTINUOUS) end
Card.IsSynchroMonster=Card.IsSynchroMonster or function(c) return c:IsType(TYPE_MONSTER) and c:IsType(TYPE_SYNCHRO) end
Card.IsLinkMonster=Card.IsLinkMonster or function(c) return c:IsType(TYPE_MONSTER) and c:IsType(TYPE_LINK) end
Card.IsXyzMonster=Card.IsXyzMonster or function(c) return c:IsType(TYPE_MONSTER) and c:IsType(TYPE_XYZ) end
Card.IsFusionSummoned=Card.IsFusionSummoned or function(c) return c:IsSummonType(SUMMON_TYPE_FUSION) end
Card.IsOwner=Card.IsOwner or function(c,p) return c:GetOwner()==p end
Card.IsReasonPlayer=Card.IsReasonPlayer or function(c,p) return c:GetReasonPlayer()==p end
Card.IsPreviousAttributeOnField=Card.IsPreviousAttributeOnField or function(c,a) return c:GetPreviousAttributeOnField()&a~=0 end
Card.IsPreviousTypeOnField=Card.IsPreviousTypeOnField or function(c,t) return c:GetPreviousTypeOnField()&t~=0 end
Card.IsAttributeExcept=Card.IsAttributeExcept or function(c,a) return c:GetAttribute()&~a~=0 end
Card.HasFlagEffect=Card.HasFlagEffect or function(c,id) return c:GetFlagEffect(id)>0 end
Card.IsEquipCard=Card.IsEquipCard or function(c) return c:GetEquipTarget()~=nil end
Card.IsNegatable=Card.IsNegatable or aux.NegateAnyFilter
Effect.IsSpellTrapEffect=Effect.IsSpellTrapEffect or function(e) return e:IsActiveType(TYPE_SPELL|TYPE_TRAP) end
Group.Iter=Group.Iter or aux.Next
Duel.IsDamageStep=Duel.IsDamageStep or function() local p=Duel.GetCurrentPhase();return p==PHASE_DAMAGE or p==PHASE_DAMAGE_CAL end
Duel.CallCoin=Duel.CallCoin or function(tp) Duel.Hint(HINT_SELECTMSG,tp,HINTMSG_COIN);local call=Duel.AnnounceCoin(tp);return Duel.TossCoin(tp,1)==call end
Duel.SetPossibleOperationInfo=Duel.SetPossibleOperationInfo or Duel.SetOperationInfo
Duel.SelectEffect=Duel.SelectEffect or function(tp,...)
 local ds,map={},{};for i,option in ipairs({...}) do if option[1] then table.insert(ds,option[2]);table.insert(map,i) end end
 return map[Duel.SelectOption(tp,table.unpack(ds))+1]
end
local chainData=setmetatable({},{__mode="k"})
Effect.GetChainData=Effect.GetChainData or function(e) if not chainData[e] then chainData[e]={} end return chainData[e] end
Cost=Cost or {}
Cost.SelfBanish=Cost.SelfBanish or aux.bfgcost
Cost.AND=Cost.AND or function(...)
 local costs={...};return function(e,tp,eg,ep,ev,re,r,rp,chk)
  if chk==0 then for _,f in ipairs(costs) do if not f(e,tp,eg,ep,ev,re,r,rp,0) then return false end end return true end
  for _,f in ipairs(costs) do f(e,tp,eg,ep,ev,re,r,rp,1) end
 end
end
Cost.DetachFromSelf=Cost.DetachFromSelf or function(min,max,after)
 return function(e,tp,eg,ep,ev,re,r,rp,chk)
  local c=e:GetHandler();if chk==0 then return c:CheckRemoveOverlayCard(tp,min,REASON_COST) end
  local g=c:GetOverlayGroup():Select(tp,min,max,nil);g:KeepAlive()
  local data=e:GetChainData();if data.cost_detached_materials then data.cost_detached_materials:DeleteGroup() end
  data.cost_detached_materials=g;Duel.SendtoGrave(g,REASON_COST)
  if after then after(e,g) end
 end
end
Card.NegateEffects=Card.NegateEffects or function(tc,c,reset,negates_cards,ct)
 reset=(reset or 0)|RESET_EVENT|RESETS_STANDARD
 local trap_monster_chk=negates_cards and tc:IsType(TYPE_TRAPMONSTER)
 if trap_monster_chk then reset=reset&~(RESET_TOFIELD|RESET_LEAVE|RESET_TURN_SET) end
 Duel.NegateRelatedChain(tc,RESET_TURN_SET)
 local e1=Effect.CreateEffect(c);e1:SetType(EFFECT_TYPE_SINGLE);e1:SetProperty(EFFECT_FLAG_CANNOT_DISABLE);e1:SetCode(EFFECT_DISABLE)
 e1:SetReset(reset,ct or 1);tc:RegisterEffect(e1)
 local e2=e1:Clone();e2:SetCode(EFFECT_DISABLE_EFFECT);e2:SetValue(RESET_TURN_SET);tc:RegisterEffect(e2)
 if trap_monster_chk then local e3=e1:Clone();e3:SetCode(EFFECT_DISABLE_TRAPMONSTER);tc:RegisterEffect(e3) end
end
aux.RegisterClientHint=aux.RegisterClientHint or function(c,reset,tp,a,b,desc)
 local e=Effect.CreateEffect(c);e:SetDescription(desc);e:SetType(EFFECT_TYPE_FIELD);e:SetProperty(EFFECT_FLAG_PLAYER_TARGET|EFFECT_FLAG_CLIENT_HINT)
 e:SetTargetRange(a,b);e:SetReset(reset or (RESET_PHASE|PHASE_END));Duel.RegisterEffect(e,tp);return e
end
