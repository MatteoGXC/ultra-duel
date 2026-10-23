#ifndef MDPRO3_FORMAT_OPTIONS_H
#define MDPRO3_FORMAT_OPTIONS_H
#include "common.h"
namespace ygo { extern "C" unsigned int get_local_genesys_point_cap(); }

// IDs 7 and 8 are local-server extensions. The core always retains MR2020
// timing and summoning rules; Genesys only changes available field zones.
inline unsigned int local_format_options(unsigned int rule) {
    if (rule == 7) return (5u << 16) | DUEL_TCG_SEGOC;
    if (rule == 8) return (5u << 16) | DUEL_TCG_SEGOC | DUEL_GENESYS_FIELD;
    return rule << 16;
}
#endif
