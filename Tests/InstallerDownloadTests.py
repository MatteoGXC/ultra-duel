"""Exercise the actual .NET installer downloader against failing HTTP fixtures."""
import http.server
import os
from pathlib import Path
import socket
import subprocess
import tempfile
import threading
import time

ROOT=Path(__file__).resolve().parents[1]
DATA=bytes(range(256))*4096
requests=[]
class Handler(http.server.BaseHTTPRequestHandler):
    protocol_version='HTTP/1.1'
    def log_message(self,*args):pass
    def do_GET(self):
        offset=int(self.headers.get('Range','bytes=0-').split('=')[1].split('-')[0]);requests.append((self.path,offset))
        if self.path=='/ignore':offset=0
        self.send_response(206 if offset else 200)
        if offset:self.send_header('Content-Range',f'bytes {offset+(1 if self.path=="/bad" else 0)}-{len(DATA)-1}/{len(DATA)}')
        self.send_header('Content-Length',str(len(DATA)-offset));self.end_headers()
        try:
            if self.path=='/drop' and offset==0:
                self.wfile.write(DATA[:262144]);self.wfile.flush();self.connection.shutdown(socket.SHUT_RDWR);return
            for position in range(offset,len(DATA),16384):
                self.wfile.write(DATA[position:position+16384]);self.wfile.flush()
                if self.path=='/slow':time.sleep(.03)
        except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):pass

def main():
    compiler=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    with tempfile.TemporaryDirectory(prefix='ultra-installer-test-') as tmp:
        tmp=Path(tmp);exe=tmp/'probe.exe';fixture=tmp/'fixture.bin';fixture.write_bytes(DATA)
        command=[str(compiler),'/nologo','/target:exe','/main:InstallerDownloadProbe','/out:'+str(exe)]
        command+=['/r:'+name+'.dll' for name in ['System.Windows.Forms','System.Drawing','System.Web.Extensions','System.IO.Compression','System.IO.Compression.FileSystem','Microsoft.CSharp']]
        subprocess.run(command+[str(ROOT/'Tools/WindowsInstaller.cs'),str(ROOT/'Assets/Scripts/MDPro3/Game/UltraInstallMigration.cs'),str(ROOT/'Assets/Scripts/MDPro3/Game/UltraUserDataBackup.cs'),str(ROOT/'Tests/InstallerDownloadProbe.cs')],check=True)
        server=http.server.ThreadingHTTPServer(('127.0.0.1',0),Handler);threading.Thread(target=server.serve_forever,daemon=True).start()
        try:subprocess.run([str(exe),f'http://127.0.0.1:{server.server_port}',str(fixture),str(tmp/'downloads')],check=True,timeout=60)
        finally:server.shutdown();server.server_close()
        assert any(path=='/drop' and offset>0 for path,offset in requests),requests
        assert sum(path=='/resume' and offset>0 for path,offset in requests)>=2,requests
        print('PASS: server observed nonzero resume ranges after interruption and pause.')
if __name__=='__main__':main()
