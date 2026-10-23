workspace "YGO Classes"
    location "build"
    objdir "build/obj"
    language "C++"
    platforms { "x64" }
    configurations { "Debug", "Release" }
    cppdialect "C++17"
    systemversion "latest"
    targetdir "build/bin/%{cfg.platform}/%{cfg.buildcfg}"
    objdir "build/obj/%{prj.name}/%{cfg.platform}/%{cfg.buildcfg}"

    defines { "WIN32", "_WIN32", "BUILD_LUA", "_CRT_SECURE_NO_WARNINGS"}
    include "../../lua"
    include "../../event"
    include "../../sqlite3"
    include "../../irrlicht/premake5-only-zipreader.lua"
    include "../../ocgcore"
    include "../../ygoserver"

    vectorextensions "SSE2"
    buildoptions { "/utf-8" }
    disablewarnings { "4334", "4244", "4267", "4838", "4577", "4819", "4018", "4996", "4477", "4091", "4828", "4800", "6011", "6031", "6054", "6262" }
