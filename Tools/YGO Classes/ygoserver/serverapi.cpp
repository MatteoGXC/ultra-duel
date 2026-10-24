#include "serverapi.h"
#include "ocgapi.h"
#include <atomic>
#include <future>
#include <memory>

namespace ygo {
    struct AdminRequest {
        std::wstring name; int operation, target, value;
        std::promise<int> result; std::atomic<bool> cancelled{false};
    };
    extern "C" DECL_DLLEXPORT int admin_duel_action(const char* name, int operation, int target, int value) {
        if(!NetServer::net_evbase || !name || strlen(name)>96) return 1;
        auto request=std::make_shared<AdminRequest>();
        wchar_t decoded[100]{}; BufferIO::DecodeUTF8(name,decoded);
        request->name=decoded; request->operation=operation; request->target=target; request->value=value;
        auto result=request->result.get_future();
        auto payload=new std::shared_ptr<AdminRequest>(request);
        timeval immediately{};
        if(event_base_once(NetServer::net_evbase,-1,EV_TIMEOUT,[](evutil_socket_t,short,void* argument) {
            std::unique_ptr<std::shared_ptr<AdminRequest>> holder(static_cast<std::shared_ptr<AdminRequest>*>(argument));
            auto r=*holder;
            r->result.set_value(r->cancelled ? 5 : NetServer::AdminAction(r->name,r->operation,r->target,r->value));
        },payload,&immediately)) {delete payload;return 1;}
        if(result.wait_for(std::chrono::seconds(4))!=std::future_status::ready) {request->cancelled=true;return 5;}
        return result.get();
    }
	bool remote_room = false;
	std::string remote_secret;
	extern "C" DECL_DLLEXPORT void configure_remote_room(const char* secret) {
		remote_secret = secret ? secret : "";
		remote_room = remote_secret.size() == 16;
	}
	static std::atomic<unsigned int> local_point_cap{100};
	extern "C" DECL_DLLEXPORT void set_local_genesys_point_cap(unsigned int cap) {
		local_point_cap.store(cap);
	}
	extern "C" unsigned int get_local_genesys_point_cap() {
		return local_point_cap.load();
	}
	extern "C" DECL_DLLEXPORT int get_server_format_capabilities() {
		return static_cast<int>(ocg_format_capabilities());
	}
	extern "C" DECL_DLLEXPORT int start_server(const char* args) {
		int argc = 1;
		char** argv = new char* [13];
		argv[0] = "ygoserver";
		size_t argLength = strlen(args);
		for (size_t i = 1, j = 0; j < argLength; ) {
			while (args[j] == ' ' && j < argLength) { ++j; }
			if (j < argLength) {
				size_t tokenLength = 0;
				while (args[j + tokenLength] != ' ' && args[j + tokenLength] != '\0') { ++tokenLength; }

				char* currentToken = new char[tokenLength + 1];
				strncpy(currentToken, args + j, tokenLength);
				currentToken[tokenLength] = '\0';

				argv[i] = currentToken;
				i++;
				j += tokenLength;
				argc++;
			}
		}

		int result = main(argc, argv);
		delete[] argv;

		return result;
	}
	extern "C" DECL_DLLEXPORT void stop_server() {
		NetServer::StopServer();
	}
}

