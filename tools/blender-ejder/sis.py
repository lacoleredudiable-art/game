import sys, OpenEXR, numpy as np
from PIL import Image
def lin(h):
    c=np.array([int(h[i:i+2],16)/255 for i in (1,3,5)])
    return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
for name in sys.argv[2:]:
    f=OpenEXR.File(f"{sys.argv[1]}/{name}.exr")
    rgb=d=None
    for p in f.parts:
        for k,v in p.channels.items():
            if k.endswith("Combined"): rgb=v.pixels[...,:3].astype(np.float32)
            if k.endswith("Depth.Z"): d=v.pixels.astype(np.float32)
    d=np.where(d>1e5,400,d)
    fog=1-np.exp(-0.012*d)                       # SI-4.1
    out=rgb*(1-fog[...,None])+lin("#4A5260")*0.9*fog[...,None]
    # renk işleme yaklaşık (O-8.8): doygunluk -35, pozlama +0.15
    out*=2**0.15; g=out.mean(-1,keepdims=True); out=g+(out-g)*0.65
    out=out/(1+out)*1.35                         # yumuşak ton eşleme
    srgb=np.where(out<=0.0031308,out*12.92,1.055*np.clip(out,0,None)**(1/2.4)-0.055)
    Image.fromarray((np.clip(srgb,0,1)*255).astype(np.uint8)).save(f"{sys.argv[1]}/{name}.png")
    print(name,"ok")
