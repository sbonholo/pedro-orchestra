import numpy as np, subprocess, os, json
sr=48000; R="unity/Assets/OrchestraProject/Resources/OrchestraSamples"
out={}
for inst in ["Violin","Viola","Cello","DoubleBass"]:
    lines=[l.split() for l in open(f"{R}/{inst}/samples.txt") if l.strip() and not l.startswith(("#","@"))]
    out[inst]=[]
    for name,midi,hz,ls,le in lines:
        ls,le=int(ls),int(le)
        d=np.fromfile(f"{R}/{inst}/{name}.bytes",dtype='<i2').astype(np.float64)/32768
        target=int(6.0*sr); xf=min(2400,(le-ls)//3)
        y=list(d[:le]); y=np.array(y)
        loop=d[ls:le]
        while len(y)<target:
            # crossfade: blend end of y (last xf samples) with the xf samples just before loop start
            head=d[ls-xf:ls]; w=np.linspace(0,1,xf)
            y[-xf:]=y[-xf:]*(1-w)+head*w
            y=np.concatenate([y,loop])
        y=y[:target]; f=int(0.1*sr); y[-f:]*=np.linspace(1,0,f)
        safe=name.replace("#","s"); wav=f"web/audio/{safe}.wav"; mp3=f"web/audio/{safe}.mp3"
        (np.clip(y,-1,1)*32767).astype('<i2').tofile(wav+".raw")
        subprocess.run(["ffmpeg","-loglevel","error","-y","-f","s16le","-ar","48000","-ac","1","-i",wav+".raw","-c:a","libmp3lame","-b:a","112k",mp3],check=True)
        os.remove(wav+".raw")
        out[inst].append({"f":safe,"m":int(midi),"hz":float(hz)})
json.dump(out,open("web/samples.json","w"))
print({k:len(v) for k,v in out.items()})
