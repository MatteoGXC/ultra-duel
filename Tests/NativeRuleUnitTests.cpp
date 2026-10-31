#include <iostream>
#include <stdexcept>
#include "duel.h"
#include "field.h"
#include "card.h"
#include "effect.h"
#include "ocgapi.h"

static int checks = 0;
static void require(bool value, const char* name) {
    if (!value) throw std::runtime_error(name);
    ++checks;
    std::cout << "PASS " << name << '\n';
}

static void admin_rule_checks() {
    auto handle = create_duel(7);
    auto* d = reinterpret_cast<duel*>(handle);
    auto& f = *d->game_field;
    f.core.duel_rule = 5;
    f.infos.phase = PHASE_MAIN1;
    f.infos.turn_player = 0;
    auto prompt = [&] {
        f.core.units.clear(); f.core.subunits.clear();
        processor_unit selection{}; selection.type = PROCESSOR_SELECT_IDLECMD; selection.step = 1;
        processor_unit command{}; command.type = PROCESSOR_IDLE_COMMAND; command.step = 1;
        f.core.units.push_back(selection); f.core.units.push_back(command);
    };
    auto drain = [&] {
        for(int n=0;n<10000;++n) {
            if(f.core.subunits.empty() && !f.core.units.empty() && f.core.units.front().type == PROCESSOR_IDLE_COMMAND) return;
            f.process();
        }
        throw std::runtime_error("Admin rule operation stalled");
    };
    auto spawn = [&](int owner,int side,int location,int zone,int type,int position) {
        auto* c = d->new_card(0); c->owner = owner;
        c->data.type = type; c->data.attack = 2000; c->data.defense = 1000;
        f.add_card(side,c,location,zone); c->current.position = position;
        c->enable_field_effect(true);
        return c;
    };
    auto* normal = spawn(1,1,LOCATION_MZONE,0,TYPE_MONSTER|TYPE_NORMAL,POS_FACEUP_ATTACK);
    auto* link = spawn(1,1,LOCATION_MZONE,1,TYPE_MONSTER|TYPE_LINK,POS_FACEUP_ATTACK);
    auto* hidden = spawn(1,1,LOCATION_MZONE,2,TYPE_MONSTER|TYPE_NORMAL,POS_FACEDOWN_DEFENSE);
    prompt();
    require(ocg_admin_command(handle,0,7,1,1)==3,"Link monster rejects Defense command");
    require(ocg_admin_command(handle,0,8,1,2)==3,"Hidden monster rejects 0 ATK without revealing it");
    require(ocg_admin_command(handle,0,8,1,6)==3,"Empty monster zone rejects ATK command");
    require(ocg_admin_command(handle,0,7,1,-1)==0,"All-monster Defense command accepted");
    drain();
    require(normal->current.position==POS_FACEUP_DEFENSE,"Normal monster becomes Defense");
    require(link->current.position==POS_FACEUP_ATTACK,"All Defense preserves Link attack position");
    require(hidden->current.position==POS_FACEDOWN_DEFENSE,"All Defense preserves hidden monster identity");
    prompt();
    require(ocg_admin_command(handle,0,8,1,0)==0,"Selected monster accepts 0 ATK");
    require(normal->get_attack()==0,"Engine ATK becomes exactly zero");
    normal->reset(RESET_TURN_SET,RESET_EVENT);
    require(normal->get_attack()==2000,"Turning monster face-down resets admin ATK effect");
    auto* stolen = spawn(0,1,LOCATION_MZONE,3,TYPE_MONSTER|TYPE_NORMAL,POS_FACEUP_ATTACK);
    auto* token = spawn(1,1,LOCATION_MZONE,4,TYPE_MONSTER|TYPE_TOKEN,POS_FACEUP_ATTACK);
    auto* material = spawn(1,1,LOCATION_HAND,0,TYPE_MONSTER|TYPE_NORMAL,POS_FACEDOWN_DEFENSE);
    normal->xyz_add(material);
    auto* spell = spawn(1,1,LOCATION_SZONE,0,TYPE_SPELL,POS_FACEDOWN_DEFENSE);
    auto* field = spawn(1,1,LOCATION_SZONE,5,TYPE_SPELL|TYPE_FIELD,POS_FACEUP_ATTACK);
    auto* hand = spawn(1,1,LOCATION_HAND,0,TYPE_MONSTER|TYPE_NORMAL,POS_FACEDOWN_DEFENSE);
    prompt();
    require(ocg_admin_command(handle,0,6,1,0)==0,"Combined hand and field clear accepted");
    drain();
    require(f.player[1].list_hand.empty(),"Combined clear empties entire hand");
    for(auto* c : f.player[1].list_mzone) require(!c,"Combined clear empties monster zone");
    for(auto* c : f.player[1].list_szone) require(!c,"Combined clear empties spell/trap/field zone");
    require(stolen->current.controler==0 && stolen->current.location==LOCATION_GRAVE,"Borrowed monster returns to its actual owner's GY");
    require(token->current.location==0,"Cleared token disappears instead of entering GY");
    require(material->current.location==LOCATION_GRAVE && !material->overlay_target,"Xyz materials are detached into GY");
    require(spell->current.location==LOCATION_GRAVE && field->current.location==LOCATION_GRAVE && hand->current.location==LOCATION_GRAVE,"Spell, Field Spell and hand card enter GY");
    end_duel(handle);
}

int main() {
    try {
        admin_rule_checks();
        duel instance;
        field& f = *instance.game_field;
        f.core.duel_rule = 5;
        f.core.duel_options = DUEL_TCG_SEGOC | DUEL_GENESYS_FIELD;
        require(f.is_location_useable(0, LOCATION_MZONE, 0), "Genesys permits Main Monster Zones");
        require(!f.is_location_useable(0, LOCATION_MZONE, 5), "Genesys rejects left Extra Monster Zone");
        require(!f.is_location_useable(1, LOCATION_MZONE, 6), "Genesys rejects right Extra Monster Zone for either player");
        require(!f.is_location_useable(0, LOCATION_PZONE, 0), "Genesys removes Pendulum Zones");
        require(f.is_location_useable(0, LOCATION_FZONE, 0), "Genesys preserves Field Spell Zone");
        require(f.get_rule_zone_fromex(0, nullptr) == 0x1f, "Genesys Extra Deck summons use five Main Monster Zones");

        card* handler = instance.new_card(90000001);
        handler->current.controler = 0;
        handler->current.location = LOCATION_HAND;
        handler->current.position = POS_FACEDOWN_DEFENSE;
        effect* trigger = instance.new_effect();
        trigger->owner = handler;
        trigger->handler = handler;
        trigger->type = EFFECT_TYPE_SINGLE | EFFECT_TYPE_TRIGGER_O;
        chain ch{};
        ch.chain_id = 123;
        ch.triggering_effect = trigger;
        ch.triggering_player = 0;
        ch.triggering_location = LOCATION_HAND;
        handler->create_relation(ch);

        f.core.duel_options = DUEL_TCG_SEGOC;
        require(f.check_nonpublic_trigger(ch), "TCG hand trigger remains in simultaneous trigger ordering");
        require(f.core.new_ochain_h.empty(), "TCG hand trigger is not deferred to the OCG fast-effect window");
        require(f.is_location_useable(0, LOCATION_MZONE, 5), "TCG preserves Extra Monster Zones");

        f.core.duel_options = 0;
        require(!f.check_nonpublic_trigger(ch), "Original OCG hidden-hand trigger behavior remains available");
        require(f.core.new_ochain_h.size() == 1, "OCG private-location trigger queue is populated");
        trigger->status |= EFFECT_STATUS_SPSELF;
        ch.flag |= CHAIN_HAND_TRIGGER;
        f.core.current_chain.push_back(ch);
        require(!f.check_spself_from_hand_trigger(ch), "Original OCG one-hand-special-trigger restriction remains");
        f.core.duel_options = DUEL_TCG_SEGOC;
        require(f.check_spself_from_hand_trigger(ch), "TCG allows another eligible hand special-summon trigger in the chain");
        // Regression: a normal 400 ATK direct attacker damages only its opponent,
        // whether it occupies engine seat zero or one, under either local format.
        for (uint32_t options : { uint32_t(DUEL_TCG_SEGOC), uint32_t(DUEL_TCG_SEGOC | DUEL_GENESYS_FIELD) }) {
            duel battle;
            auto& bf = *battle.game_field;
            bf.core.duel_rule = 5;
            bf.core.duel_options = options;
            card* attacker = battle.new_card(90000002);
            attacker->data.type = TYPE_MONSTER | TYPE_NORMAL;
            attacker->data.attack = 400;
            attacker->data.defense = 400;
            attacker->current.location = LOCATION_MZONE;
            attacker->current.position = POS_FACEUP_ATTACK;
            bf.core.attacker = attacker;
            bf.core.attack_target = nullptr;
            for (uint8_t seat : { 0, 1 }) {
                attacker->current.controler = seat;
                bf.calculate_battle_damage(nullptr, nullptr, nullptr);
                require(bf.core.battle_damage[seat] == 0, "400 ATK direct attacker takes no damage");
                require(bf.core.battle_damage[1 - seat] == 400, "Direct opponent takes exactly 400 damage");
            }
        }
        std::cout << checks << " native rule checks passed.\n";
        return 0;
    } catch (const std::exception& error) {
        std::cerr << "FAILED: " << error.what() << '\n';
        return 1;
    }
}
