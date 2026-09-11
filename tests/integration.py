"""Deterministic HTTP contract tests. No external provider requests are made."""
import http.cookiejar,json,os,socket,subprocess,tempfile,time,urllib.request,urllib.error
from pathlib import Path
root=Path(__file__).resolve().parents[1]
with socket.socket() as sock:
 sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
base=f'http://127.0.0.1:{port}'
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def request(path,body=None,code=200,client=opener):
 req=urllib.request.Request(base+path,data=None if body is None else json.dumps(body).encode(),headers={'Content-Type':'application/json'})
 try:
  with client.open(req,timeout=30) as res: status=res.status; data=res.read()
 except urllib.error.HTTPError as ex: status=ex.code;data=ex.read()
 assert status==code,(path,status,data.decode()[:500])
 return json.loads(data) if data else None
with tempfile.TemporaryDirectory() as directory:
 env={**os.environ,'ASPNETCORE_URLS':base,'ASPNETCORE_ENVIRONMENT':'Testing','TestFixtures':'true','DATA_DIR':directory}
 with open(Path(directory)/'server.log','w+') as log:
  process=subprocess.Popen([os.environ.get('DOTNET','dotnet'),str(root/'src/bin/Release/net10.0/App.dll'),'--contentRoot',str(root/'src')],env=env,stdout=log,stderr=log,cwd=root)
  try:
   for _ in range(100):
    try:
     assert request('/api/health')['mode']=='test fixtures';break
    except (OSError,AssertionError): time.sleep(.1)
   else: raise RuntimeError('Server did not start')
   request('/api/game',code=404)
   g=request('/api/games',{})
   assert g['round']==1 and len(g['answers'])==0 and 'lat' not in g['photo']
   request('/api/guess',{'roundId':g['roundId'],'latitude':91,'longitude':0},400)
   request('/api/guess',{'roundId':'forged','latitude':0,'longitude':0},409)
   request('/api/guess',{'roundId':g['roundId'],'latitude':0,'longitude':0},404,urllib.request.build_opener())
   for i in range(5):
    rid=g['roundId']
    g=request('/api/guess',{'roundId':rid,'latitude':0,'longitude':0,'score':999999})
    assert g['answered'] and 0<=g['answers'][-1]['score']<=5000
    request('/api/guess',{'roundId':rid,'latitude':0,'longitude':0},409)
    g=request('/api/next',{})
   assert g['complete'] and len(g['answers'])==5 and g['total']==sum(x['score'] for x in g['answers'])
   request('/api/next',{},409)
   print('HTTP game ownership, coordinate bounds, duplicate guesses and five-round completion passed.')
  finally:
   process.terminate()
   try:process.wait(timeout=10)
   except subprocess.TimeoutExpired:process.kill();process.wait()
