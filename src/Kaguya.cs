using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace KaguyaNative
{
    internal static class Program
    {
        [STAThread] static void Main(string[] args)
        {
            Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try {
                if(args.Length>0 && args[0]=="--self-test") { Checks.Run(); return; }
                if(args.Length>0 && args[0]=="--quit") { Native.PostMessage(Native.FindWindow(null,Pet.Title),0x10,IntPtr.Zero,IntPtr.Zero); return; }
                bool fresh;
                using(Mutex mutex=new Mutex(true,"Local\\KaguyaNativePet2",out fresh)) {
                    if(!fresh) { Native.PostMessage(Native.FindWindow(null,Pet.Title),0x8001,IntPtr.Zero,IntPtr.Zero); return; }
                    using(Pet pet=new Pet(false)) Application.Run(pet);
                }
            } catch(Exception e) {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.log"),e.ToString());
                if(args.Length==0) MessageBox.Show(e.Message,"辉夜桌宠");
                Environment.ExitCode=1;
            }
        }
    }
    internal sealed class State
    {
        public double mood=80,energy=75,scale=1;
        public bool sleeping=false,topmost=true,panelEnabled=true;
        public double? x=null,y=null;
    }
    internal sealed class Geometry
    {
        internal static readonly RectangleF Character=new RectangleF(45,375,290,300);
        internal static double Center(double s) { return 190+(256-214)*Math.Min(220*s/512,245*s/576); }
        internal static RectangleF Sprite(double s) {
            double f=Math.Min(220*s/512,245*s/576),ix=Center(s)-512*f/2,iy=675-576*f;
            return RectangleF.FromLTRB((float)(ix+76*f),(float)(iy+38*f),(float)(ix+436*f),(float)(iy+548*f));
        }
        // Original main.cjs baseline: 629 + layoutTopPadding (46) = 675.
        internal static RectangleF BoundarySprite(double s) { return Sprite(s); }
        internal static RectangleF Companion(double s) {
            RectangleF reference=BoundarySprite(.8),r=BoundarySprite(s);
            double marginLeft=reference.Left-93-(Center(.8)-204),marginTop=reference.Top-410-46;
            return RectangleF.FromLTRB((float)(r.Left-marginLeft),(float)(r.Top-marginTop),
                (float)(Math.Max(Math.Max(r.Right,Center(s)+110*s),315)+4),Math.Max(r.Bottom,705)+12);
        }
        internal static PointF Clamp(double x,double y,RectangleF area,RectangleF r) {
            return new PointF((float)Math.Floor(Math.Max(area.Left-r.Left,Math.Min(x,area.Right-r.Right))+.5),
                (float)Math.Floor(Math.Max(area.Top-r.Top,Math.Min(y,area.Bottom-r.Bottom))+.5));
        }
    }
    internal sealed class Pet:Form
    {
        internal const string Title="Kaguya.Native.Pet2";
        internal State State=new State();
        internal bool PanelOpen,Dragging;
        internal string Frame="idle",Bubble="";
        internal readonly float Dpi;
        internal readonly float UiAssetScale;
        readonly string uiDirectory;
        readonly bool testing;
        readonly bool isolatedSettings;
        readonly string root=AppDomain.CurrentDomain.BaseDirectory;
        readonly string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"kaguya-native-pet");
        readonly JavaScriptSerializer json=new JavaScriptSerializer();
        readonly Dictionary<string,Bitmap> images=new Dictionary<string,Bitmap>();
        readonly Dictionary<string,RectangleF> layout=new Dictionary<string,RectangleF>();
        readonly LayeredSurface surface=new LayeredSurface();
        readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
        readonly Stopwatch clock=Stopwatch.StartNew();
        readonly Random random=new Random();
        NotifyIcon tray; Icon icon; ContextMenuStrip menu;
        readonly HandCursors hands;
        bool pressed,ready,disposed,freeMovement;
        Point downScreen,dragOffset;
        string pressedTarget="",hover="",focus="character";
        double bubbleUntil,imageUntil,shyStart=-10,panelChanged=-10,panelFrom,panelTo,lastTick,lastSave=-1;
        bool pendingSave;
        Bitmap cachedPanel;
        string cachedPanelKey="",lastScene="";
        double meterClock;
        internal double Now {get{return clock.Elapsed.TotalSeconds;}}
        internal double PanelDelta {get{return Geometry.BoundarySprite(State.scale).Top-Geometry.BoundarySprite(1).Top;}}
        internal Pet(bool test):this(test,null){}
        internal Pet(bool test,string settingsDirectory)
        {
            testing=test;isolatedSettings=test&&settingsDirectory!=null;
            if(settingsDirectory!=null)data=settingsDirectory;
            using(Graphics g=Graphics.FromHwnd(IntPtr.Zero)) Dpi=g.DpiX/96f;
            UiAssetScale=SelectUiScale(Dpi);
            uiDirectory=UiAssetScale==1?Path.Combine("assets","ui"):Path.Combine("assets","ui","dpi-"+Math.Round(UiAssetScale*100));
            hands=new HandCursors();
            if(!test||isolatedSettings) LoadState();
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;
            AutoScaleMode=AutoScaleMode.None;ClientSize=new Size((int)Math.Round(380*Dpi),(int)Math.Round(726*Dpi));
            Text=Title;TopMost=State.topmost;KeyPreview=true;
            foreach(string name in new string[]{"idle","happy","tea","sleep"}) LoadImage(name,Path.Combine("assets","kaguya",name+".png"));
            for(int s=0;s<2;s++)for(int t=0;t<2;t++)for(int z=0;z<3;z++) {
                string name="panel-"+s+"-"+t+"-"+z;LoadUiImage(name);
            }
            for(int s=0;s<2;s++)for(int o=0;o<2;o++) {
                string name="badge-"+s+"-"+o;LoadUiImage(name);
            }
            Dictionary<string,object> source=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"assets","ui","layout.json")));
            foreach(string key in new string[]{"panel","close","feed","pat","sleep","topmost","hide","badge","character","mood","energy","mood-label","energy-label"}) layout[key]=ReadRect((Dictionary<string,object>)source[key]);
            System.Collections.IList scales=(System.Collections.IList)source["scales"];
            for(int i=0;i<3;i++)layout["scale"+i]=ReadRect((Dictionary<string,object>)scales[i]);
            Frame=State.sleeping?"sleep":"idle";
            ResetPosition();
            if(!test) {
                icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                Icon=icon;tray=new NotifyIcon{Icon=icon,Text="辉夜 · 今天也陪在你身边",Visible=true};
                tray.MouseClick+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)ShowPet();else if(e.Button==MouseButtons.Right)ShowMenu(Cursor.Position);};
            }
            timer.Interval=16;timer.Tick+=delegate{TickFrame();};
            Shown+=delegate{ready=true;Say("今天，就让我在这里陪你吧");PaintFrame();if(!test){menu=CreateMenu();IntPtr warmMenuHandle=menu.Handle;using(Bitmap preview=new Bitmap(menu.Width,menu.Height))menu.DrawToBitmap(preview,new Rectangle(Point.Empty,menu.Size));timer.Start();}};
        }
        RectangleF ReadRect(Dictionary<string,object> d){return new RectangleF(Convert.ToSingle(d["x"]),Convert.ToSingle(d["y"]),Convert.ToSingle(d["width"]),Convert.ToSingle(d["height"]));}
        void LoadImage(string name,string path) {using(Bitmap src=new Bitmap(Path.Combine(root,path))) {Bitmap b=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppPArgb);using(Graphics g=Graphics.FromImage(b)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(src,0,0);}images.Add(name,b);}}
        float SelectUiScale(float requested) {
            float best=1;
            foreach(string folder in Directory.GetDirectories(Path.Combine(root,"assets","ui"),"dpi-*")) {
                int percent;
                if(!int.TryParse(Path.GetFileName(folder).Substring(4),out percent)||!File.Exists(Path.Combine(folder,"panel-0-1-1.png")))continue;
                float candidate=percent/100f;
                if(candidate>=requested&&(best<requested||candidate<best)||best<requested&&candidate>best)best=candidate;
            }
            return best;
        }
        void LoadUiImage(string name) {
            using(Bitmap src=new Bitmap(Path.Combine(root,uiDirectory,name+".png"))) {
                // Round only the canvas dimensions; preserve original logical geometry.
                int width=(int)Math.Round((name.StartsWith("panel-")?380:190)*Dpi);
                int height=(int)Math.Round((name.StartsWith("panel-")?419:54)*Dpi);
                Bitmap b=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
                using(Graphics g=Graphics.FromImage(b)) {
                    g.CompositingMode=CompositingMode.SourceCopy;
                    if(Math.Abs(UiAssetScale-Dpi)<.001f)g.DrawImageUnscaled(src,0,0);
                    else {g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;using(ImageAttributes attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(src,new Rectangle(0,0,width,height),0,0,src.Width,src.Height,GraphicsUnit.Pixel,attributes);}}
                }
                if(name.StartsWith("badge-"))DrawBadgeLabels(b,name);
                images.Add(name,b);
            }
        }
        void DrawBadgeLabels(Bitmap bitmap,string name) {
            string metadata=Path.Combine(root,uiDirectory,"badge-labels.json");
            if(!File.Exists(metadata))return;
            var variants=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(metadata));
            var labels=(Dictionary<string,object>)variants[name];
            using(Graphics g=Graphics.FromImage(bitmap)) {
                g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
                foreach(string id in new string[]{"badge-name","badge-state"}) {
                    var label=(Dictionary<string,object>)labels[id];RectangleF logical=ReadRect(label);
                    RectangleF pixels=new RectangleF((float)Math.Round(logical.X*Dpi),(float)Math.Round(logical.Y*Dpi),(float)Math.Round(logical.Width*Dpi),(float)Math.Round(logical.Height*Dpi));
                    // Draw directly into the final-size cached badge; never scale these glyphs.
                    using(Font font=new Font("Microsoft YaHei UI",Convert.ToSingle(label["fontSize"])*Dpi,FontStyle.Regular,GraphicsUnit.Pixel))
                    using(Brush brush=new SolidBrush(C(id=="badge-name"?"#443c45":"#ac8e9e")))
                    using(StringFormat format=(StringFormat)StringFormat.GenericTypographic.Clone()) {
                        format.Alignment=StringAlignment.Near;format.LineAlignment=StringAlignment.Near;
                        format.FormatFlags=StringFormatFlags.NoWrap|StringFormatFlags.NoClip;
                        string text=(string)label["text"];SizeF size=g.MeasureString(text,font,PointF.Empty,format);
                        PointF origin=new PointF((float)Math.Round(pixels.Left+(pixels.Width-size.Width)/2),(float)Math.Round(pixels.Top+(pixels.Height-size.Height)/2));
                        g.DrawString(text,font,brush,origin,format);
                    }
                }
            }
        }
        internal Bitmap BadgeBitmap(){return (Bitmap)images["badge-"+(State.sleeping?1:0)+"-"+(PanelOpen?1:0)].Clone();}
        void LoadState() {
            string path=Path.Combine(data,"pet-state.json");
            bool firstRun=!File.Exists(path);
            // Legacy folder and JSON key are read only for one-way save migration.
            if(firstRun&&!isolatedSettings)path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"naiwa-desktop-pet","pet-state.json");
            try {
                string content=File.ReadAllText(path);
                State s=json.Deserialize<State>(content);
                var saved=json.Deserialize<Dictionary<string,object>>(content);
                if(s!=null) {
                    if(!saved.ContainsKey("energy")&&saved.ContainsKey("milk"))s.energy=Convert.ToDouble(saved["milk"]);
                    State=s;State.mood=FiniteClamp(s.mood,80);State.energy=FiniteClamp(s.energy,75);
                    if(s.scale!=.8&&s.scale!=1&&s.scale!=1.2)State.scale=1;
                }
            }catch{}
            if(firstRun)State.topmost=true;
            try{var d=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(data,"movement-calibration.json")));freeMovement=d.ContainsKey("enabled")&&d["enabled"] is bool&&(bool)d["enabled"];}catch{}
        }
        double FiniteClamp(double v,double fallback){return double.IsNaN(v)||double.IsInfinity(v)?fallback:Math.Max(0,Math.Min(100,v));}
        internal void SaveState(bool force) {
            if(testing&&!isolatedSettings)return;
            pendingSave=true;if(!force&&Now-lastSave<.25)return;
            try{Directory.CreateDirectory(data);State.x=Left/Dpi;State.y=Top/Dpi;string p=Path.Combine(data,"pet-state.json"),tmp=p+".tmp";File.WriteAllText(tmp,json.Serialize(State));if(File.Exists(p))File.Replace(tmp,p,null);else File.Move(tmp,p);lastSave=Now;pendingSave=false;}catch(Exception e){Debug.WriteLine(e);}
        }
        protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle=(p.ExStyle|0x80000|0x80|0x08000000)&~0x40000;p.ClassStyle|=8;return p;}}
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override void OnPaint(PaintEventArgs e) { }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        internal RectangleF Area(Point point) {Rectangle r=Screen.FromPoint(point).WorkingArea;return new RectangleF(r.X/Dpi,r.Y/Dpi,r.Width/Dpi,r.Height/Dpi);}
        internal Point ClampLocation(Point physical,Point? pointer) {
            if(freeMovement)return physical;
            RectangleF r=Geometry.Companion(State.scale);
            Point p=pointer??new Point(physical.X+(int)((r.Left+r.Right)/2*Dpi),physical.Y+(int)((r.Top+r.Bottom)/2*Dpi));
            PointF c=Geometry.Clamp(physical.X/Dpi,physical.Y/Dpi,Area(p),r);
            return new Point((int)Math.Round(c.X*Dpi),(int)Math.Round(c.Y*Dpi));
        }
        internal void ResetPosition() {
            Rectangle a=Screen.PrimaryScreen.WorkingArea;
            PointF p=Geometry.Clamp(a.Right/Dpi,a.Bottom/Dpi,new RectangleF(a.X/Dpi,a.Y/Dpi,a.Width/Dpi,a.Height/Dpi),Geometry.Companion(State.scale));
            EndDrag();PanelOpen=false;panelTo=panelFrom=0;Location=new Point((int)Math.Round(p.X*Dpi),(int)Math.Round(p.Y*Dpi));SaveState(false);
        }
        internal void TogglePanel() {
            if(!State.panelEnabled)return;
            if(!PanelOpen) {
                RectangleF r=Geometry.BoundarySprite(State.scale);Point c=new Point(Left+(int)((r.Left+r.Right)/2*Dpi),Top+(int)((r.Top+r.Bottom)/2*Dpi));
                PointF p=Geometry.Clamp(Left/Dpi,Top/Dpi,Area(c),new RectangleF(0,0,380,726));
                Location=new Point((int)Math.Round(p.X*Dpi),(int)Math.Round(p.Y*Dpi));SaveState(false);
            }
            panelFrom=PanelAlpha;PanelOpen=!PanelOpen;panelTo=PanelOpen?1:0;panelChanged=Now;PaintFrame();
        }
        internal void TogglePanelEnabled() {
            State.panelEnabled=!State.panelEnabled;
            if(!State.panelEnabled){PanelOpen=false;panelFrom=panelTo=0;panelChanged=Now;focus="character";}
            SaveState(true);PaintFrame();
        }
        double PanelAlpha {get{double t=Math.Min(1,Math.Max(0,(Now-panelChanged)/.08));return panelFrom+(panelTo-panelFrom)*(t*t*(3-2*t));}}
        internal RectangleF Rect(string id) {RectangleF r=layout[id];if(id!="badge"&&id!="character")r.Y+=(float)PanelDelta;return r;}
        internal string Target(Point p) {
            PointF q=new PointF(p.X/Dpi,p.Y/Dpi);
            if(PanelOpen&&Rect("panel").Contains(q)) {
                foreach(string id in new string[]{"close","feed","pat","sleep","topmost","hide","scale0","scale1","scale2"})if(Rect(id).Contains(q))return id;
                return "panel";
            }
            if(Rect("badge").Contains(q))return "badge";
            if(Geometry.Character.Contains(q))return "character";
            return "";
        }
        protected override void WndProc(ref Message m) {
            if(m.Msg==0x8001){ShowPet();return;}
            // A desktop companion must not activate WinForms' hidden taskbar owner.
            if(m.Msg==0x21){m.Result=(IntPtr)3;return;}
            if(m.Msg==0x84&&ready&&!pressed) {
                long v=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(v&65535),(short)((v>>16)&65535)));
                m.Result=Target(p)!=""?(IntPtr)1:new IntPtr(-1);return;
            }
            base.WndProc(ref m);
            if((m.Msg==0x7e||m.Msg==0x2e0)&&ready){Location=ClampLocation(Location,null);PaintFrame();SaveState(false);}
        }
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if(e.Button==MouseButtons.Right){EndDrag();ShowMenu(PointToScreen(e.Location));return;}
            if(e.Button!=MouseButtons.Left)return;
            pressedTarget=Target(e.Location);if(pressedTarget=="")return;
            pressed=true;downScreen=PointToScreen(e.Location);Capture=true;focus=pressedTarget;UpdateCursor(pressedTarget);
        }
        internal void MoveGesture(Point p) {
            if(!pressed||pressedTarget!="character")return;
            double dx=(p.X-downScreen.X)/Dpi,dy=(p.Y-downScreen.Y)/Dpi;
            bool began=false;
            if(!Dragging&&Math.Sqrt(dx*dx+dy*dy)>5) {
                began=true;
                Dragging=true;UpdateCursor("character");if(PanelOpen){panelFrom=PanelAlpha;PanelOpen=false;panelTo=0;panelChanged=Now;}
                // Same grab point as the Electron version: capture offset at threshold crossing.
                dragOffset=new Point(p.X-Left,p.Y-Top);
            }
            if(Dragging){Location=ClampLocation(new Point(p.X-dragOffset.X,p.Y-dragOffset.Y),p);if(began)PaintFrame();}
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);MoveGesture(PointToScreen(e.Location));
            if(Dragging)return;
            string next=Target(e.Location);UpdateCursor(next);if(next!=hover){hover=next;PaintFrame();}
        }
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);if(!Dragging){hover="";UpdateCursor("");}PaintFrame();}
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);if(e.Button!=MouseButtons.Left||!pressed)return;
            bool dragged=Dragging;string target=pressedTarget;
            if(dragged)MoveGesture(PointToScreen(e.Location));
            EndDrag();
            if(!dragged&&(target=="character"||target==Target(e.Location)))ActivateTarget(target);
            PaintFrame();
        }
        internal void EndDrag() {bool moved=Dragging;pressed=false;Dragging=false;pressedTarget="";Capture=false;if(hands!=null)UpdateCursor(Target(PointToClient(Cursor.Position)));if(moved){SaveState(false);RecordCalibration();}}
        internal void UpdateCursor(string target){Cursor=Dragging||(pressed&&pressedTarget=="character")?hands.Grabbing:target=="character"?hands.Grab:target=="panel"||target==""||(target=="badge"&&!State.panelEnabled)?Cursors.Default:Cursors.Hand;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&pressed){EndDrag();PaintFrame();}}
        protected override void OnDeactivate(EventArgs e){base.OnDeactivate(e);if(pressed){EndDrag();PaintFrame();}}
        internal void ActivateTarget(string id) {
            if(id=="badge"||id=="close")TogglePanel();
            else if(id=="character"||id=="pat")Pat();
            else if(id=="feed")Feed(true);
            else if(id=="sleep")Sleep(true);
            else if(id=="topmost"){State.topmost=!State.topmost;TopMost=State.topmost;SaveState(true);}
            else if(id=="panel-enabled")TogglePanelEnabled();
            else if(id=="hide")Hide();
            else if(id.StartsWith("scale")){int i=int.Parse(id.Substring(5));State.scale=new double[]{.8,1,1.2}[i];Location=ClampLocation(Location,null);SaveState(false);}
            PaintFrame();
        }
        internal void Say(string value){Bubble=value;bubbleUntil=Now+3.3;}
        internal void SetFrame(string name){Frame=name;imageUntil=Now+3.2;}
        internal void Pat() {
            if(State.sleeping){Say("嘘，辉夜正在休息……☾");return;}
            State.mood=Math.Min(100,State.mood+8);SetFrame("happy");shyStart=Now;
            string[] phrases={"能陪你一会……也不是不可以","今天也很努力呢，稍微休息一下吧","哼，才没有在等你……♡"};Say(phrases[random.Next(phrases.Length)]);SaveState(false);
        }
        internal void Feed(bool visual) {
            if(State.sleeping){if(visual)Say("等辉夜醒来，再一起喝茶吧");return;}
            State.energy=Math.Min(100,State.energy+20);State.mood=Math.Min(100,State.mood+5);
            if(visual){SetFrame("tea");Say("茶已经泡好了。要一起喝吗？");}SaveState(false);
        }
        internal void Sleep(bool visual) {
            State.sleeping=!State.sleeping;Frame=State.sleeping?"sleep":"idle";imageUntil=0;
            if(visual)Say(State.sleeping?"稍微休息一下……一会儿见":"我醒了。你一直在这里吗？");SaveState(false);
        }
        internal void AdvanceMeters() {
            State.energy=Math.Max(0,State.energy-1);
            State.mood=State.sleeping?Math.Min(100,State.mood+1):Math.Max(0,State.mood-1);
            SaveState(false);
        }
        void TickFrame() {
            double now=Now;if(pendingSave&&now-lastSave>=.25)SaveState(false);
            if(now-meterClock>=30){meterClock=now;AdvanceMeters();}
            if(!State.sleeping&&imageUntil>0&&now>=imageUntil){Frame="idle";imageUntil=0;}
            // Hidden pets keep their timers/state without rendering invisible frames.
            if(Visible&&(menu==null||!menu.Visible))PaintFrame();lastTick=now;
        }
        void ShowPet(){Show();PaintFrame();}
        internal void ShowMenu(Point p) {
            if(menu==null)menu=CreateMenu();
            menu.Items["feed"].Enabled=!State.sleeping;
            menu.Items["sleep"].Text=State.sleeping?"叫醒辉夜":"休息一会";
            ((ToolStripMenuItem)menu.Items["topmost"]).Checked=State.topmost;
            ((ToolStripMenuItem)menu.Items["panel-enabled"]).Checked=State.panelEnabled;
            ((RoundedMenu)menu).ShowAnchored(this,p);
        }
        internal RoundedMenu ActiveMenu {get{return menu as RoundedMenu;}}
        internal RoundedMenu CreateMenu() {
            RoundedMenu result=new RoundedMenu(Dpi);
            result.PopupOwner=Handle;
            result.Items.Add("显示辉夜",null,delegate{ShowPet();});
            ToolStripItem feed=result.Items.Add("一起喝茶",null,delegate{Feed(false);PaintFrame();});feed.Name="feed";feed.Enabled=!State.sleeping;
            result.Items.Add(State.sleeping?"叫醒辉夜":"休息一会",null,delegate{Sleep(false);PaintFrame();}).Name="sleep";
            ToolStripMenuItem top=new ToolStripMenuItem("始终置顶",null,delegate{ActivateTarget("topmost");});top.Checked=State.topmost;top.Name="topmost";top.Tag="toggle";result.Items.Add(top);
            ToolStripMenuItem panel=new ToolStripMenuItem("启用面板",null,delegate{ActivateTarget("panel-enabled");});panel.Checked=State.panelEnabled;panel.Name="panel-enabled";panel.Tag="toggle";result.Items.Add(panel);
            result.Items.Add("回到屏幕右下角",null,delegate{ResetPosition();PaintFrame();});result.Items.Add(new ToolStripSeparator());
            result.Items.Add("暂时隐藏",null,delegate{Hide();});result.Items.Add("退出辉夜",null,delegate{Close();});result.Prepare();return result;
        }
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if(e.KeyCode==Keys.Escape&&PanelOpen){TogglePanel();e.Handled=true;return;}
            if(e.KeyCode==Keys.Tab) {
                string[] order=PanelOpen?new string[]{"close","feed","pat","sleep","topmost","scale0","scale1","scale2","hide","character","badge"}:State.panelEnabled?new string[]{"character","badge"}:new string[]{"character"};
                int i=Array.IndexOf(order,focus);focus=order[(i+(e.Shift?order.Length-1:1))%order.Length];PaintFrame();e.Handled=true;
            }
            if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){ActivateTarget(focus);e.Handled=true;e.SuppressKeyPress=true;}
        }
        protected override bool IsInputKey(Keys keyData){Keys key=keyData&Keys.KeyCode;if(key==Keys.Tab||key==Keys.Enter||key==Keys.Space||key==Keys.Escape)return true;return base.IsInputKey(keyData);}
        void RecordCalibration() {
            if(!freeMovement||testing)return;
            try{Directory.CreateDirectory(data);RectangleF r=Geometry.BoundarySprite(State.scale),a=Area(Cursor.Position);File.WriteAllText(Path.Combine(data,"movement-calibration-position.json"),json.Serialize(new {recordedAt=DateTime.UtcNow.ToString("o"),window=new{x=Left/Dpi,y=Top/Dpi,width=380,height=726},scale=State.scale,characterBounds=r,companionBounds=Geometry.Companion(State.scale),workArea=a,suggestedInsets=new{left=a.Left-Left/Dpi,top=a.Top-Top/Dpi}}));}catch{}
        }
        static Color C(string value){return ColorTranslator.FromHtml(value);}
        static GraphicsPath Round(RectangleF r,float radius) {GraphicsPath p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));if(d<=0)return p;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        static void FillRound(Graphics g,RectangleF r,float radius,Color color){using(GraphicsPath p=Round(r,radius))using(Brush b=new SolidBrush(color))g.FillPath(b,p);}
        static void TextAt(Graphics g,string text,float size,Color color,RectangleF rect,StringAlignment align) {
            using(Font f=new Font("Microsoft YaHei UI",size,FontStyle.Regular,GraphicsUnit.Pixel))using(Brush b=new SolidBrush(color))using(StringFormat sf=new StringFormat{Alignment=align,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(text,f,b,rect,sf);
        }
        static void DrawAlpha(Graphics g,Bitmap image,RectangleF rect,float alpha) {
            using(ImageAttributes a=new ImageAttributes()){ColorMatrix m=new ColorMatrix();m.Matrix33=alpha;a.SetColorMatrix(m);a.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(image,Rectangle.Round(rect),0,0,image.Width,image.Height,GraphicsUnit.Pixel,a);}
        }
        void DrawUiPixels(Graphics g,Bitmap image,float x,float y,float alpha) {
            GraphicsState saved=g.Save();
            g.ResetTransform();g.PixelOffsetMode=PixelOffsetMode.Default;
            int px=(int)Math.Round(x*Dpi),py=(int)Math.Round(y*Dpi);
            if(alpha>=1)g.DrawImageUnscaled(image,px,py);
            else DrawAlpha(g,image,new RectangleF(px,py,image.Width,image.Height),alpha);
            g.Restore(saved);
        }
        internal Bitmap RenderBitmap() {
            Bitmap b=new Bitmap(ClientSize.Width,ClientSize.Height,PixelFormat.Format32bppPArgb);
            using(Graphics g=Graphics.FromImage(b)) {
                g.Clear(Color.Transparent);g.ScaleTransform(Dpi,Dpi);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                // Alpha=1 preserves the original rectangular hit areas, including empty artwork margins.
                using(Brush hit=new SolidBrush(Color.FromArgb(1,255,250,251))) {g.FillRectangle(hit,Geometry.Character);g.FillRectangle(hit,layout["badge"]);if(PanelOpen)g.FillRectangle(hit,Rect("panel"));}
                using(Brush shadow=new SolidBrush(Color.FromArgb(20,116,65,89)))g.FillEllipse(shadow,142.5f,666,95,8);
                double s=State.scale,cx=Geometry.Center(s),baseY=675,phase=(Now%(State.sleeping?6:4))/(State.sleeping?6:4);
                double breathe=1+.012*(.5-.5*Math.Cos(phase*2*Math.PI));double angle=0;
                if(Dragging){breathe=1;angle=-4;}
                else if(Now-shyStart<1.3) {
                    double t=((Now-shyStart)%.65)/.65;breathe=1;
                    angle=t<.35?-3*(.5-.5*Math.Cos(t/.35*Math.PI)):t<.7?-3+6*(.5-.5*Math.Cos((t-.35)/.35*Math.PI)):3*(.5+.5*Math.Cos((t-.7)/.3*Math.PI));
                }
                GraphicsState save=g.Save();g.TranslateTransform((float)cx,(float)baseY);g.ScaleTransform((float)s,(float)s);g.RotateTransform((float)angle);g.ScaleTransform(1,(float)breathe);
                double f=Math.Min(220.0/512,245.0/576);RectangleF dest=new RectangleF((float)(-512*f/2),(float)(-576*f),(float)(512*f),(float)(576*f));
                if(State.sleeping) {
                    using(ImageAttributes attr=new ImageAttributes()) {
                        const float sat=.85f;float r=.213f*(1-sat),gg=.715f*(1-sat),bl=.072f*(1-sat);
                        attr.SetColorMatrix(new ColorMatrix(new float[][]{new float[]{r+sat,r,r,0,0},new float[]{gg,gg+sat,gg,0,0},new float[]{bl,bl,bl+sat,0,0},new float[]{0,0,0,.96f,0},new float[]{0,0,0,0,1}}));
                        g.DrawImage(images[Frame],Rectangle.Round(dest),0,0,512,576,GraphicsUnit.Pixel,attr);
                    }
                }else g.DrawImage(images[Frame],dest);
                g.Restore(save);
                if(State.sleeping){GraphicsState z=g.Save();g.TranslateTransform((float)cx,675);g.ScaleTransform((float)s,(float)s);TextAt(g,"z Z",21,C("#a18bb1"),new RectangleF(44,-260,65,28),StringAlignment.Center);g.Restore(z);}
                double pa=PanelAlpha;
                if(pa>.001) {
                    Bitmap panel=GetPanel();DrawUiPixels(g,panel,0,(float)(PanelDelta+10*(1-pa)),(float)pa);
                }
                string badge="badge-"+(State.sleeping?1:0)+"-"+(PanelOpen?1:0);
                DrawUiPixels(g,images[badge],95,(int)Math.Floor(layout["badge"].Y)-12,1);
                if(Now<bubbleUntil)DrawBubble(g);
            }
            return b;
        }
        internal Bitmap PanelBitmap() {
            int scale=State.scale==.8?0:State.scale==1?1:2;
            Bitmap original=images["panel-"+(State.sleeping?1:0)+"-"+(State.topmost?1:0)+"-"+scale];
            Bitmap b=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppPArgb);
            using(Graphics g=Graphics.FromImage(b)) {
                g.DrawImageUnscaled(original,0,0);g.ScaleTransform(Dpi,Dpi);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
                foreach(string id in new string[]{"mood","energy"}) {RectangleF r=layout[id];FillRound(g,r,3,C("#eee5ea"));double value=id=="mood"?State.mood:State.energy;if(value>0)FillRound(g,new RectangleF(r.X,r.Y,(float)(r.Width*value/100),r.Height),3,C(id=="mood"?"#ce9aab":"#b3a9c9"));}
                RectangleF mood=layout["mood-label"];mood.X=100;mood.Width=81;
                TextAt(g,State.mood>60?"愉快":State.mood>30?"想见你":"想被关心",11,C("#b48e9a"),mood,StringAlignment.Far);
                RectangleF energy=layout["energy-label"];energy.X=280;energy.Width=53;TextAt(g,State.energy.ToString("0")+"%",11,C("#b48e9a"),energy,StringAlignment.Far);
                if(hover=="feed"||hover=="pat"||hover=="sleep") {
                    RectangleF r=layout[hover];r.Y-=2;FillRound(g,r,14,C("#efdae3"));
                    TextAt(g,hover=="feed"?"♧":hover=="pat"?"♡":"☾",24,C("#b86981"),new RectangleF(r.X,r.Y+9,r.Width,28),StringAlignment.Center);
                    TextAt(g,hover=="feed"?"一起喝茶":hover=="pat"?"摸摸头":State.sleeping?"叫醒辉夜":"休息一会",11,C("#443c45"),new RectangleF(r.X,r.Y+40,r.Width,16),StringAlignment.Center);
                }
            }return b;
        }
        Bitmap GetPanel(){string key=State.sleeping+"/"+State.topmost+"/"+State.scale+"/"+State.mood+"/"+State.energy+"/"+hover;if(key!=cachedPanelKey){if(cachedPanel!=null)cachedPanel.Dispose();cachedPanel=PanelBitmap();cachedPanelKey=key;}return cachedPanel;}
        void DrawBubble(Graphics g) {
            RectangleF r=Geometry.BoundarySprite(State.scale),reference=Geometry.BoundarySprite(.8);
            Point c=new Point(Left+(int)((r.Left+r.Right)/2*Dpi),Top+(int)((r.Top+r.Bottom)/2*Dpi));RectangleF a=Area(c);
            float x=(float)Math.Max(a.Left-Left/Dpi+4,Math.Min(65,a.Right-Left/Dpi-254));
            float y=(float)Math.Max(a.Top-Top/Dpi+12,Math.Min(442+r.Top-reference.Top,a.Bottom-Top/Dpi-80));
            using(Font font=new Font("Microsoft YaHei UI",11,FontStyle.Regular,GraphicsUnit.Pixel)) {
                SizeF size=g.MeasureString(Bubble,font,228);RectangleF rect=new RectangleF(x,y,250,(float)Math.Ceiling(size.Height)+22);
                FillRound(g,new RectangleF(x,y+3,250,rect.Height),16,Color.FromArgb(12,114,70,91));FillRound(g,rect,16,Color.FromArgb(245,255,250,251));
                using(GraphicsPath p=Round(rect,16))using(Pen pen=new Pen(C("#ead9e2")))g.DrawPath(pen,p);
                using(Brush brush=new SolidBrush(C("#8e6074")))using(StringFormat sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(Bubble,font,brush,new RectangleF(x+10,y+11,230,rect.Height-22),sf);
            }
        }
        internal void PaintFrame(){
            if(!ready||disposed||!IsHandleCreated)return;
            // Window movement reuses the existing native surface. During a drag,
            // upload pixels only for a visual change; never gate mouse movement on rendering.
            string scene=State.sleeping+"/"+State.topmost+"/"+State.scale+"/"+State.mood+"/"+State.energy+"/"+Frame+"/"+hover+"/"+PanelOpen;
            if(!Dragging||Now-panelChanged<.1)scene+="/t"+(int)(Now*60);
            if(Now<bubbleUntil)scene+="/b"+Bubble+"/"+Left+"/"+Top;
            if(scene==lastScene)return;
            using(Bitmap b=RenderBitmap())surface.Update(Handle,b,Location);lastScene=scene;
        }
        internal void TestPress(Point screen){pressed=true;pressedTarget="character";downScreen=screen;UpdateCursor("character");}
        protected override void Dispose(bool disposing) {
            if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();SaveState(true);if(tray!=null){tray.Visible=false;tray.Dispose();}if(menu!=null)menu.Dispose();if(icon!=null)icon.Dispose();hands.Dispose();if(cachedPanel!=null)cachedPanel.Dispose();surface.Dispose();foreach(Bitmap b in images.Values)b.Dispose();}base.Dispose(disposing);
        }
    }
}
