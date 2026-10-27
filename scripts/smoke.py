"""Disposable Compose smoke, including shared session and optional rollback drill."""
import http.cookiejar, urllib.request, urllib.parse, html.parser, json, time, subprocess, sys
class Inputs(html.parser.HTMLParser):
    def __init__(self, text):
        super().__init__(); self.token=None; self.item=None; self.feed(text)
    def handle_starttag(self, tag, attrs):
        attrs=dict(attrs)
        if attrs.get('name')=='__RequestVerificationToken': self.token=attrs['value']
        if attrs.get('item-id'): self.item=int(attrs['item-id'])
def ready(port):
    for _ in range(90):
        try:
            if urllib.request.urlopen(f'http://localhost:{port}/health/ready',timeout=2).status==200: return
        except Exception: pass
        time.sleep(2)
    raise RuntimeError(f'Port {port} never became ready')
for port in (5080,5081): ready(port)
client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def get(port,path): return client.open(f'http://localhost:{port}{path}').read().decode()
catalog=get(5080,'/')
token=Inputs(catalog).token; assert token
body=urllib.parse.urlencode({'codigo':'001','__RequestVerificationToken':token}).encode()
cart=client.open('http://localhost:5080/Pedido/Adicionar',body).read().decode(); item=Inputs(cart).item; assert item
assert Inputs(get(5081,'/Pedido/Carrinho')).item==item
subprocess.run(['docker','compose','restart','web'],check=True)
ready(5080)
assert Inputs(get(5080,'/Pedido/Carrinho')).item==item
if '--rollback' in sys.argv:
    subprocess.run(['docker','tag','casadocodigo:local','casadocodigo:known-good'],check=True)
    subprocess.run(['docker','build','-t','casadocodigo:broken','-'],input=b'FROM casadocodigo:known-good\nENTRYPOINT ["dotnet", "Missing.dll"]\n',check=True)
    import os
    subprocess.run(['docker','compose','up','-d','--no-deps','--no-build','web','web2'],env=dict(os.environ,APP_IMAGE='casadocodigo:broken'),check=True)
    time.sleep(2)
    try:
        client.open('http://localhost:5080/health/ready',timeout=2)
        raise AssertionError('Broken image unexpectedly stayed healthy')
    except (urllib.error.URLError, ConnectionError, TimeoutError): pass
    subprocess.run(['bash','scripts/rollback.sh','casadocodigo:known-good'],check=True)
    assert Inputs(get(5080,'/Pedido/Carrinho')).item==item
    assert Inputs(get(5081,'/Pedido/Carrinho')).item==item
print('Compose smoke passed: catalogue, POST, shared session, restart and rollback drill')
