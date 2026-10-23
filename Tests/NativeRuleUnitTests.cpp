#include <iostream>
#include <stdexcept>
#include "duel.h"
#include "field.h"
#include "card.h"
#include "effect.h"

static int checks = 0;
static void require(bool value, const char* name) {
    if (!value) throw std::runtime_error(name);
    ++checks;
    std::cout << "PASS " << name << '\n';
}

int main() {
    try {
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
