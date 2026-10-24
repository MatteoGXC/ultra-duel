import Cocoa
import CryptoKit

struct RuntimePart: Codable { let name: String; let url: String; let size: Int64; let sha256: String }
struct RuntimeManifest: Codable { let version: String; let url: String; let size: Int64; let sha256: String; let parts: [RuntimePart]? }
enum InstallError: Error { case failed(String) }
final class RuntimeInstaller {
    let resources: URL
    let runtime: URL
    let manifest: RuntimeManifest
    let files = FileManager.default
    var report: (String, Double?) -> Void = { _, _ in }
    var active: [Process] = []
    let lock = NSLock()
    var cancelled = false
    init(resources: URL, runtime: URL) throws {
        self.resources=resources;self.runtime=runtime
        manifest=try JSONDecoder().decode(RuntimeManifest.self,from:Data(contentsOf:resources.appendingPathComponent("runtime.json")))
    }
    func cancel() { lock.lock();cancelled=true;let tasks=active;lock.unlock();for task in tasks where task.isRunning { task.terminate() } }
    func run(_ executable: URL,_ arguments: [String],input: FileHandle?=nil,output: FileHandle?=nil) throws {
        if cancelled { throw InstallError.failed("Paused. Your download has been saved.") }
        let task=Process();task.executableURL=executable;task.arguments=arguments
        if let input=input { task.standardInput=input };if let output=output { task.standardOutput=output }
        let log=runtime.appendingPathComponent("installation.log");if !files.fileExists(atPath:log.path){files.createFile(atPath:log.path,contents:nil)}
        let handle=try FileHandle(forWritingTo:log);handle.seekToEndOfFile();task.standardError=handle
        lock.lock();active.append(task)
        do { try task.run();lock.unlock() } catch { active.removeAll{$0 === task};lock.unlock();throw error }
        task.waitUntilExit();try handle.close()
        lock.lock();active.removeAll{$0 === task};lock.unlock()
        if task.terminationStatus != 0 { throw InstallError.failed(cancelled ? "Paused. Your download has been saved." : "Installation could not finish. Retry to resume the download. See installation.log in the Ultra Duel data folder.") }
    }
    func checksum(_ url: URL) throws -> String {
        let stream=try FileHandle(forReadingFrom:url);defer{try? stream.close()};var hash=SHA256()
        while let chunk=try stream.read(upToCount:4*1024*1024),!chunk.isEmpty { hash.update(data:chunk) }
        return hash.finalize().map{String(format:"%02x",$0)}.joined()
    }
    func contentVersionHeader() throws -> String {
        var request=URLRequest(url:URL(string:manifest.url)!);request.httpMethod="HEAD";request.timeoutInterval=30
        let completed=DispatchSemaphore(value:0);var etag: String?;var failure: Error?
        URLSession.shared.dataTask(with:request){_,response,error in
            failure=error
            if let http=response as? HTTPURLResponse,http.statusCode==200 {etag=http.value(forHTTPHeaderField:"ETag")}
            completed.signal()
        }.resume()
        guard completed.wait(timeout:.now()+35) == .success, failure==nil,let result=etag else {throw InstallError.failed("The game content server did not respond. Retry to continue.")}
        return result
    }
    func downloadParts(_ parts: [RuntimePart],cache: URL,archive: URL) throws {
        let work=DispatchGroup();let queue=OperationQueue();queue.maxConcurrentOperationCount=3
        var errors: [Error]=[];let errorLock=NSLock()
        let timer=DispatchSource.makeTimerSource(queue:DispatchQueue.global());timer.schedule(deadline:.now(),repeating:.milliseconds(300))
        timer.setEventHandler { [weak self] in
            guard let self=self else{return};let done=parts.reduce(0.0){sum,part in sum+(((try? self.files.attributesOfItem(atPath:cache.appendingPathComponent(part.name).path)[.size]) as? NSNumber)?.doubleValue ?? 0)}
            self.report(String(format:"Downloading game content: %.2f / %.2f GB",done/1_073_741_824,Double(self.manifest.size)/1_073_741_824),min(1,done/Double(self.manifest.size)))
        };timer.resume();defer{timer.cancel()}
        for part in parts {
            work.enter();queue.addOperation {
                defer{work.leave()}
                do {
                    let target=cache.appendingPathComponent(part.name)
                    let size=((try? self.files.attributesOfItem(atPath:target.path)[.size]) as? NSNumber)?.int64Value ?? 0
                    var verified=false
                    if size==part.size {verified=(try self.checksum(target))==part.sha256}
                    if !verified {
                        if size>=part.size {try? self.files.removeItem(at:target)}
                        try self.run(URL(fileURLWithPath:"/usr/bin/curl"),["--fail","--location","--silent","--show-error","--retry","5","--retry-delay","2","--continue-at","-","--output",target.path,part.url])
                    }
                    guard try self.checksum(target)==part.sha256 else {try? self.files.removeItem(at:target);throw InstallError.failed("A content download failed verification. Retry to download that part again.")}
                } catch {errorLock.lock();errors.append(error);errorLock.unlock()}
            }
        }
        work.wait();if let error=errors.first{throw error}
        report("Combining verified game content…",nil)
        files.createFile(atPath:archive.path,contents:nil);let output=try FileHandle(forWritingTo:archive);try output.truncate(atOffset:0);defer{try? output.close()}
        for part in parts {
            let input=try FileHandle(forReadingFrom:cache.appendingPathComponent(part.name));defer{try? input.close()}
            while let bytes=try input.read(upToCount:4*1024*1024),!bytes.isEmpty {try output.write(contentsOf:bytes)}
        }
    }
    func copyDefaults() throws {
        let source=resources.appendingPathComponent("Defaults")
        guard let entries=files.enumerator(at:source,includingPropertiesForKeys:[.isDirectoryKey]) else { throw InstallError.failed("Bundled game data is missing.") }
        let personal=Set(["Data/config.conf","Data/Settings.json","Data/profile.json","Data/friends-session.json","Data/mycard_apps.json"])
        for case let file as URL in entries {
            let name=String(file.path.dropFirst(source.path.count+1));let target=runtime.appendingPathComponent(name)
            if try file.resourceValues(forKeys:[.isDirectoryKey]).isDirectory == true { try files.createDirectory(at:target,withIntermediateDirectories:true);continue }
            if files.fileExists(atPath:target.path) {
                if personal.contains(name) || name.hasPrefix("Deck/") || name.hasPrefix("Replay/") || name.hasPrefix("Progress/"){continue}
                try files.removeItem(at:target)
            }
            try files.createDirectory(at:target.deletingLastPathComponent(),withIntermediateDirectories:true);try files.copyItem(at:file,to:target)
        }
    }
    func prepare(offline: URL?=nil) throws {
        try files.createDirectory(at:runtime,withIntermediateDirectories:true)
        // The upstream art archive omits the six bundles built from our project.
        if !files.fileExists(atPath:runtime.appendingPathComponent("StandaloneOSX/MasterDuel/Built-in/sprites").path) { try copyDefaults() }
        let marker=runtime.appendingPathComponent(".ultra-mac-content")
        let expected=manifest.sha256
        if (try? String(contentsOf:marker,encoding:.utf8)) != expected || !files.fileExists(atPath:runtime.appendingPathComponent("StandaloneOSX/MasterDuel/Built-in/sprites").path) {
            let cache=runtime.appendingPathComponent("Downloads");try files.createDirectory(at:cache,withIntermediateDirectories:true)
            let archive=offline ?? cache.appendingPathComponent("mac-runtime.tar.zst")
            if offline == nil {
                let free=(try files.attributesOfFileSystem(forPath:runtime.path)[.systemFreeSize] as? NSNumber)?.int64Value ?? 0
                if free<10_000_000_000 { throw InstallError.failed("Ultra Duel needs 10 GB of free space for its first installation.") }
                let archiveSize=((try? files.attributesOfItem(atPath:archive.path)[.size]) as? NSNumber)?.int64Value ?? 0
                var cached=false
                if archiveSize==manifest.size {cached=(try checksum(archive))==expected}
                if cached {report("Using verified game content…",1)}
                else if let parts=manifest.parts,!parts.isEmpty {
                    try downloadParts(parts,cache:cache,archive:archive)
                } else {
                report("Downloading game artwork and duel fields (3.7 GB)…",0)
                let timer=DispatchSource.makeTimerSource(queue:DispatchQueue.global());timer.schedule(deadline:.now(),repeating:.milliseconds(300))
                timer.setEventHandler { [weak self] in
                    guard let self=self else{return};let bytes=((try? self.files.attributesOfItem(atPath:archive.path)[.size]) as? NSNumber)?.doubleValue ?? 0
                    self.report(String(format:"Downloading game content: %.2f / %.2f GB",bytes/1_073_741_824,Double(self.manifest.size)/1_073_741_824),min(1,bytes/Double(self.manifest.size)))
                };timer.resume();defer{timer.cancel()}
                let etag=try contentVersionHeader()
                try run(URL(fileURLWithPath:"/usr/bin/curl"),["--fail","--location","--retry","5","--retry-delay","2","--header","If-Match: "+etag,"--continue-at","-","--output",archive.path,manifest.url])
                }
            }
            report("Verifying game content…",nil)
            guard ((try files.attributesOfItem(atPath:archive.path)[.size]) as? NSNumber)?.int64Value == manifest.size,try checksum(archive)==expected else {
                if offline == nil { try? files.removeItem(at:archive) }
                throw InstallError.failed("Game content failed verification. Retry to download a fresh copy.")
            }
            if offline == nil {for part in manifest.parts ?? [] {try? files.removeItem(at:cache.appendingPathComponent(part.name))}}
            report("Installing artwork and duel fields…",nil)
            let decompressor=Process();decompressor.executableURL=resources.appendingPathComponent("Tools/zstd");decompressor.arguments=["-d","-c",archive.path]
            let pipe=Pipe();decompressor.standardOutput=pipe;try decompressor.run()
            do {
                try run(URL(fileURLWithPath:"/usr/bin/tar"),["-xpf","-","-C",runtime.path,"./Picture","./StandaloneOSX"],input:pipe.fileHandleForReading)
                decompressor.waitUntilExit();if decompressor.terminationStatus != 0 { throw InstallError.failed("Game content could not be decompressed.") }
            } catch { if decompressor.isRunning{decompressor.terminate()};throw error }
            guard files.fileExists(atPath:runtime.appendingPathComponent("StandaloneOSX/MasterDuel/Built-in/sprites").path) else { throw InstallError.failed("The Mac duel assets were not installed.") }
            try expected.write(to:marker,atomically:true,encoding:.utf8)
            if offline == nil { try? files.removeItem(at:archive) }
        }
        let version=runtime.appendingPathComponent(".ultra-mac-version")
        if (try? String(contentsOf:version,encoding:.utf8)) != manifest.version { report("Installing current game data…",nil);try copyDefaults();try manifest.version.write(to:version,atomically:true,encoding:.utf8) }
        report("Ready to duel",1)
    }
    func launch() throws {
        let task=Process();task.executableURL=resources.appendingPathComponent("Player.app/Contents/MacOS/Ultra Duel")
        task.currentDirectoryURL=runtime;var env=ProcessInfo.processInfo.environment;env["ULTRA_DUEL_DATA"]=runtime.path;task.environment=env;try task.run()
    }
}
final class App: NSObject, NSApplicationDelegate, NSWindowDelegate {
    var window: NSWindow!;var label: NSTextField!;var progress: NSProgressIndicator!;var button: NSButton!;var installer: RuntimeInstaller!;var busy=false
    func applicationDidFinishLaunching(_ notification: Notification) {
        window=NSWindow(contentRect:NSRect(x:0,y:0,width:540,height:240),styleMask:[.titled,.closable],backing:.buffered,defer:false);window.title="Ultra Duel";window.center();window.delegate=self
        let heading=NSTextField(labelWithString:"ULTRA DUEL");heading.font = .boldSystemFont(ofSize:28);heading.frame=NSRect(x:28,y:170,width:480,height:45)
        label=NSTextField(wrappingLabelWithString:"First launch installs the game’s artwork and duel fields. Interrupted downloads can be resumed.");label.frame=NSRect(x:28,y:100,width:480,height:62)
        progress=NSProgressIndicator(frame:NSRect(x:28,y:70,width:480,height:18));progress.style = .bar;progress.minValue=0;progress.maxValue=1
        button=NSButton(title:"Install and Play",target:self,action:#selector(start));button.frame=NSRect(x:330,y:20,width:180,height:36);button.bezelStyle = .rounded
        let views: [NSView]=[heading,label,progress,button];views.forEach{window.contentView!.addSubview($0)};window.makeKeyAndOrderFront(nil);NSApp.activate(ignoringOtherApps:true)
        do {
            let root=FileManager.default.urls(for:.applicationSupportDirectory,in:.userDomainMask)[0].appendingPathComponent("Ultra Duel/Runtime")
            installer=try RuntimeInstaller(resources:Bundle.main.resourceURL!,runtime:root)
            installer.report={ [weak self] text,value in DispatchQueue.main.async { self?.label.stringValue=text;self?.progress.isIndeterminate=value==nil;if let v=value{self?.progress.doubleValue=v}else{self?.progress.startAnimation(nil)} } }
            if FileManager.default.fileExists(atPath:root.appendingPathComponent(".ultra-mac-content").path){start()}
        } catch { label.stringValue="The app is incomplete. Download Ultra Duel again.";button.isEnabled=false }
    }
    @objc func start() {
        if busy { installer.cancel();return };busy=true;installer.cancelled=false;button.title="Pause"
        DispatchQueue.global().async {
            do { try self.installer.prepare();try self.installer.launch();DispatchQueue.main.async{NSApp.terminate(nil)} }
            catch { DispatchQueue.main.async { self.busy=false;self.button.title="Retry";self.progress.stopAnimation(nil);if case InstallError.failed(let message)=error{self.label.stringValue=message}else{self.label.stringValue=error.localizedDescription} } }
        }
    }
    func windowWillClose(_ notification: Notification){installer?.cancel();NSApp.terminate(nil)}
}
if CommandLine.arguments.contains("--prepare-only") {
    do {
        func value(_ flag:String)->String?{guard let i=CommandLine.arguments.firstIndex(of:flag),i+1<CommandLine.arguments.count else{return nil};return CommandLine.arguments[i+1]}
        guard let root=value("--runtime") else{throw InstallError.failed("Missing --runtime")}
        let resources=Bundle.main.resourceURL!;let installer=try RuntimeInstaller(resources:resources,runtime:URL(fileURLWithPath:root));installer.report={text,_ in print(text)}
        try installer.prepare(offline:value("--offline-archive").map{URL(fileURLWithPath:$0)});print("ULTRA_MAC_INSTALL_PASSED")
    } catch { fputs("\(error)\n",stderr);exit(2) }
} else {
    let app=NSApplication.shared;let delegate=App();app.delegate=delegate;app.setActivationPolicy(.regular);app.run()
}
