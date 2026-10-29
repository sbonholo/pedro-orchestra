using System; using System.IO; using System.Linq; using OrchestraProject;
class P {
  static double Pitch(float[] buf, int s0, double expected) {
    int n = 48000; int minLag=(int)(48000/(expected*1.3)), maxLag=(int)(48000/(expected/1.3));
    double C(int l){ double c=0; for(int i=0;i<n;i++) c+=buf[s0+i]*buf[s0+i+l]; return c; }
    double best=-1e9; int bl=minLag; for(int l=minLag;l<=maxLag;l++){ double c=C(l); if(c>best){best=c;bl=l;} }
    double a=C(bl-1), b=C(bl), cc=C(bl+1); return 48000.0/(bl+0.5*(a-cc)/(a-2*b+cc));
  }
  static void Main() {
    int fails = 0;
    foreach (var (t, notes) in new (InstrumentType, int[])[] {
        (InstrumentType.Violin, new[]{55,62,67,72,76,79,84,91}), (InstrumentType.Viola, new[]{48,55,60,64,69,74}),
        (InstrumentType.Cello, new[]{36,40,43,48,50,55,57,60,64,69}), (InstrumentType.DoubleBass, new[]{28,31,36,40,43,50}) }) {
      string dir = "../../unity/Assets/OrchestraProject/Resources/OrchestraSamples/" + t + "/";
      var set = SampleSet.Parse(t, File.ReadAllText(dir + "samples.txt"), f => File.ReadAllBytes(dir + f + ".bytes"));
      var def = InstrumentCatalog.Get(t);
      foreach (int midi in notes) {
        var rp = new RenderedPart { Instrument = def, SectionGain = 1f };
        rp.Notes.Add(new RenderedNote { StartSec=0, DurationSec=5.0, PlayedMidi=midi, Velocity=0.8f, AttackSec=0.05f, Brightness=0.9f });
        var buf = SampleVoice.RenderPart(rp, set, 48000, 5.0, 1);
        double exp = 440*Math.Pow(2,(midi-69)/12.0);
        int s1=(int)((StringSynth.LeadInSec+0.6)*48000), s2=(int)((StringSynth.LeadInSec+3.4)*48000);
        double c1=1200*Math.Log2(Pitch(buf,s1,exp)/exp), c2=1200*Math.Log2(Pitch(buf,s2,exp)/exp);
        double R(int s){double r=0; for(int i=0;i<9600;i++) r+=buf[s+i]*buf[s+i]; return Math.Sqrt(r/9600);}
        bool ok = Math.Abs(c1)<25 && Math.Abs(c2)<25 && R(s2)>0.4*R(s1) && !buf.Any(float.IsNaN);
        if(!ok) fails++;
        Console.WriteLine($"{(ok?"PASS":"FAIL")} {t,-10} {NoteParser.MidiToName(midi),-4} via {set.Nearest(midi).Name,-14} pitch {c1:+0;-0}c / {c2:+0;-0}c (vibrato in recording), level {R(s1):0.000}->{R(s2):0.000}");
      }
    }
    Console.WriteLine(fails==0?"ALL PASS":fails+" FAILED");
  }
}
