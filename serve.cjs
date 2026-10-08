// Local-only static server. Run from the project root after building.
const http=require('http'),fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'Builds/WebGL');
const mime={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.css':'text/css','.json':'application/json','.png':'image/png','.data':'application/octet-stream','.unityweb':'application/octet-stream'};
const server=http.createServer((req,res)=>{
 let pathname;try{pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);}catch{res.writeHead(400);return res.end('Bad request');}
 const file=path.resolve(root,'.'+(pathname==='/'?'/index.html':pathname));
 if(!file.startsWith(root+path.sep)){res.writeHead(403);return res.end('Forbidden');}
 fs.stat(file,(err,stat)=>{
  if(err||!stat.isFile()){res.writeHead(404);return res.end('Not found');}
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Content-Length':stat.size,'Cache-Control':'no-cache'});
  const stream=fs.createReadStream(file);stream.on('error',()=>res.destroy());stream.pipe(res);
 });
});
server.on('error',async error=>{
 if(error.code==='EADDRINUSE'){
  try{
   const response=await fetch('http://127.0.0.1:8765/',{signal:AbortSignal.timeout(3000)});
   if(response.ok && (await response.text()).includes('<title>Echo Shift')){
    console.log('Echo Shift is already running at http://127.0.0.1:8765');return;
   }
  }catch{}
 }
 console.error('Could not start the local server: '+error.message);process.exitCode=1;
});
server.listen(8765,'127.0.0.1',()=>console.log('Echo Shift: http://127.0.0.1:8765 — Ctrl+C stops the server.'));
