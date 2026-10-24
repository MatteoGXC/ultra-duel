#include "data_manager.h"
#include "CFileSystem.h"
#include <filesystem>
#include <fstream>

static bool failed = false;
static uint32_t log_message(intptr_t duel, uint32_t) {
    char message[4096]{};
    get_log_message(duel, message);
    std::cerr << message << '\n';
    failed = true;
    return 0;
}

int main(int argc, char** argv) {
    if (argc != 2) return 2;
    const auto root = std::filesystem::absolute(argv[1]);
    // Run in a directory with no loose scripts: production packages only script.zip.
    const auto sandbox = std::filesystem::current_path() / "archive-effect-sandbox";
    std::filesystem::create_directories(sandbox);
    std::filesystem::current_path(sandbox);
    ygo::DataManager::FileSystem = new irr::io::CFileSystem();
    if (!ygo::DataManager::FileSystem->addFileArchive(
            (root / "Data/script.zip").string().c_str(), true, false, irr::io::EFAT_ZIP)) return 3;
    if (!ygo::dataManager.LoadDB((root / "Data/locales/en-US/cards.cdb").wstring().c_str())) return 4;
    set_script_reader(ygo::DataManager::ScriptReaderEx);
    set_card_reader(ygo::DataManager::CardReader);
    set_message_handler(log_message);
    std::ofstream("effect_probe.lua") << R"LUA(
Debug.ReloadFieldBegin(DUEL_TEST_MODE,5)
Debug.SetPlayerInfo(0,8000,0,0)
Debug.SetPlayerInfo(1,8000,0,0)
local white=Debug.AddCard(34995106,0,0,LOCATION_HAND,0,POS_FACEDOWN)
Debug.AddCard(25451383,0,0,LOCATION_HAND,1,POS_FACEDOWN)
local aluber=Debug.AddCard(62962630,0,0,LOCATION_MZONE,0,POS_FACEUP_ATTACK)
Debug.AddCard(36637374,0,0,LOCATION_DECK,0,POS_FACEDOWN)
Debug.AddCard(72272462,0,0,LOCATION_EXTRA,0,POS_FACEDOWN)
Debug.ReloadFieldEnd()
assert(type(c62962630.thfilter)=="function", "Aluber effect script was not loaded")
assert(c62962630.thtg(nil,0,nil,0,0,nil,0,0,0), "Aluber summon search must be available")
assert(c62962630.thfilter(Duel.GetFirstMatchingCard(Card.IsCode,0,LOCATION_DECK,0,nil,36637374)), "Aluber must find Branded Opening")
local effect=white:GetActivateEffect()
assert(effect, "Branded in White has no activation effect")
assert(c34995106.target(effect,0,nil,0,0,nil,0,0,0), "White must fuse hand Albion and field Aluber into Quaeritis")
)LUA";
    const auto duel = create_duel(1);
    const auto loaded = preload_script(duel, "effect_probe.lua");
    end_duel(duel);
    if (!loaded || failed) return 5;
    std::cout << "PASS server ZIP reader loads native Aluber search and Branded in White effects\n";
    return 0;
}
