using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace KaguyaNative {
internal static class Checks {
 static List<object> checks=new List<object>();
 static void Check(bool pass,string name){checks.Add(new{name=name,passed=pass});if(!pass)throw new Exception("Failed: "+name);}
 internal static void Run() {
  string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verification");Directory.CreateDirectory(folder);
  bool success=false;string error=null;
  try {
   string settings=Path.Combine(folder,"settings-check-"+Guid.NewGuid().ToString("N"));
   using(Pet first=new Pet(true,settings)) {
    Check(first.State.topmost&&first.TopMost&&first.State.panelEnabled,"first launch defaults to topmost and panel enabled");
    first.ActivateTarget("topmost");first.ActivateTarget("panel-enabled");
   }
   using(Pet second=new Pet(true,settings)) {
    Check(!second.State.topmost&&!second.TopMost&&!second.State.panelEnabled,"closed topmost and panel settings survive dispose and reload");
    second.ActivateTarget("topmost");second.ActivateTarget("panel-enabled");
   }
   using(Pet third=new Pet(true,settings))Check(third.State.topmost&&third.TopMost&&third.State.panelEnabled,"enabled topmost and panel settings survive dispose and reload");
   File.WriteAllText(Path.Combine(settings,"pet-state.json"),"{\"mood\":42,\"topmost\":false}");
   using(Pet older=new Pet(true,settings))Check(!older.State.topmost&&!older.TopMost&&older.State.panelEnabled&&older.State.mood==42,"old settings retain explicit topmost false and default missing panel switch to enabled");
   File.WriteAllText(Path.Combine(settings,"pet-state.json"),"{\"mood\":42,\"milk\":37,\"topmost\":false}");
   using(Pet legacy=new Pet(true,settings))Check(legacy.State.energy==37&&legacy.State.mood==42&&!legacy.State.topmost,"legacy energy key migrates without resetting saved state");
   string migrated=File.ReadAllText(Path.Combine(settings,"pet-state.json"));
   Check(migrated.Contains("\"energy\":37")&&!migrated.Contains("\"milk\""),"new saves contain only the energy key");
   File.WriteAllText(Path.Combine(settings,"pet-state.json"),"{\"energy\":29,\"milk\":91}");
   using(Pet current=new Pet(true,settings))Check(current.State.energy==29,"current energy key takes precedence over legacy key");
   using(Pet smaller=new Pet(true,settings))smaller.SetScale(.73,true);
   using(Pet reloaded=new Pet(true,settings))Check(reloaded.State.scale==.73,"custom 73 percent size survives save and reload");
   Check(Pet.NormalizeScale(double.NaN)==1&&Pet.NormalizeScale(double.PositiveInfinity)==1&&Pet.NormalizeScale(.1)==.5&&Pet.NormalizeScale(2)==1.2,"invalid scale fallback and slider range limits");
   Directory.Delete(settings,true);
   using(Pet pet=new Pet(true)) {
    pet.Show();Application.DoEvents();
    Check(!pet.PanelOpen,"startup panel collapsed");
    Check(pet.State.scale==1&&pet.State.mood==80&&pet.State.energy==75,"original defaults");
    pet.UpdateCursor("character");IntPtr grabHandle=pet.Cursor.Handle;
    Check(grabHandle!=IntPtr.Zero&&grabHandle!=Cursors.SizeAll.Handle&&grabHandle!=Cursors.Hand.Handle,"character hover uses grab hand instead of move arrows or link pointer");
    using(Bitmap cursor=HandCursors.Bitmap(false))cursor.Save(Path.Combine(folder,"cursor-grab.png"));
    using(Bitmap cursor=HandCursors.Bitmap(true))cursor.Save(Path.Combine(folder,"cursor-grabbing.png"));
    Check(Native.SendMessage(pet.Handle,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()==3,"mouse clicks do not activate the companion or a hidden taskbar owner");
    Check((Native.GetWindowLong(pet.Handle,-20)&0x80)!=0&&(Native.GetWindowLong(pet.Handle,-20)&0x40000)==0,"companion is a tool window without taskbar application style");
    using(RoundedMenu popup=pet.CreateMenu()) {
     Check(!popup.Region.IsVisible(0,0)&&popup.Region.IsVisible(popup.Width/2,popup.Height/2),"context menu has rounded window corners");
     Check(popup.Items.Count==9&&popup.Items[0].Text=="显示辉夜"&&popup.Items[4].Name=="panel-enabled"&&popup.Items[5].Text=="回到屏幕右下角"&&popup.Items[6] is ToolStripSeparator&&popup.Items[8].Text=="退出辉夜","context menu adds panel switch and preserves existing action ordering and separator");
     Check(((ToolStripMenuItem)popup.Items["panel-enabled"]).Checked,"menu panel toggle reflects enabled state");
     Point menuAnchor=new Point(600,500);IntPtr foreground=Native.GetForegroundWindow();
     popup.ShowAnchored(pet,menuAnchor);Application.DoEvents();
     Check(Math.Abs(popup.Left-menuAnchor.X)<=2&&Math.Abs(popup.Bottom-menuAnchor.Y)<=2,"popup bottom-left corner stays at the mouse anchor");
     Check(Native.GetForegroundWindow()==foreground,"context menu does not steal foreground focus");
     Check((Native.GetWindowLong(popup.Handle,-20)&0x80)!=0&&(Native.GetWindowLong(popup.Handle,-20)&0x40000)==0,"context menu is a tool window without taskbar application style");
     Check(TaskbarCandidates()==0,"opening context menu creates no visible taskbar-eligible application window");
     using(Bitmap preview=new Bitmap(popup.Width,popup.Height)){popup.DrawToBitmap(preview,new Rectangle(Point.Empty,popup.Size));for(int y=0;y<preview.Height;y++)for(int x=0;x<preview.Width;x++)if(!popup.Region.IsVisible(x,y))preview.SetPixel(x,y,Color.Transparent);preview.Save(Path.Combine(folder,"context-menu.png"));}
     popup.Close();
     double menuEnergy=pet.State.energy;popup.Items[1].PerformClick();Check(pet.State.energy==Math.Min(100,menuEnergy+20),"restyled menu still dispatches feeding action");pet.State.energy=75;pet.State.mood=80;
    }
    Rectangle work=Screen.PrimaryScreen.WorkingArea;
    Point clipped=RoundedMenu.AnchoredLocation(new Point(work.Right-1,work.Top+1),new Size(192,333),work);
    Check(clipped.X>=work.Left&&clipped.X+192<=work.Right&&clipped.Y==work.Top,"popup stays on screen at top-right boundary");
    pet.ShowMenu(new Point(600,500));IntPtr reused=pet.ActiveMenu.Handle;pet.ActiveMenu.Close();
    pet.State.sleeping=true;pet.State.panelEnabled=false;pet.ShowMenu(new Point(600,500));
    Check(pet.ActiveMenu.Handle==reused&&pet.ActiveMenu.Items["sleep"].Text=="叫醒辉夜"&&!pet.ActiveMenu.Items["feed"].Enabled&&!((ToolStripMenuItem)pet.ActiveMenu.Items["panel-enabled"]).Checked,"reopening reuses the popup window and refreshes current state");
    pet.ActiveMenu.Close();pet.State.sleeping=false;pet.State.panelEnabled=true;
    Stopwatch openWatch=Stopwatch.StartNew();
    for(int i=0;i<10;i++){pet.ShowMenu(new Point(600,500));Application.DoEvents();pet.ActiveMenu.Close();}
    openWatch.Stop();Check(pet.ActiveMenu.Handle==reused,"ten popup cycles reuse the same native handle");
    checks.Add(new{name="ten warm context-menu open/close cycles",totalMs=openWatch.Elapsed.TotalMilliseconds});
    var baseline=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(folder,"original-geometry.json")));
    foreach(Dictionary<string,object> item in (System.Collections.IList)baseline["cases"]) {
     double scale=Convert.ToDouble(item["scale"]);var orig=(Dictionary<string,object>)item["companion"];RectangleF actual=Geometry.Companion(scale);
     Check(Math.Abs(actual.Left-Convert.ToDouble(orig["left"]))<.001&&Math.Abs(actual.Top-Convert.ToDouble(orig["top"]))<.001&&Math.Abs(actual.Bottom-Convert.ToDouble(orig["bottom"]))<.001,"left, top and bottom movement geometry matches original JavaScript at scale "+scale);
     if(scale==1.2)Check(Math.Abs(actual.Right-Convert.ToDouble(orig["right"]))<.001,"large size retains its original right boundary");
    }
    Point outside=pet.PointToScreen(new Point(5,5)),inside=pet.PointToScreen(new Point((int)(46*pet.Dpi),(int)(500*pet.Dpi)));
    Func<Point,IntPtr> pack=p=>new IntPtr((p.Y<<16)|(p.X&65535));
    Check(Native.SendMessage(pet.Handle,0x84,IntPtr.Zero,pack(outside)).ToInt32()==-1,"outside original regions passes through");
    Check(Native.SendMessage(pet.Handle,0x84,IntPtr.Zero,pack(inside)).ToInt32()==-1,"transparent character margin rejects Windows mouse input");
    Check(Native.WindowFromPoint(new Native.Point(inside.X,inside.Y))!=pet.Handle,"Windows routes transparent character margin to the underlying window");
    pet.Bubble="";
    pet.TogglePanel();Application.DoEvents();
    Check(pet.PanelOpen,"enabled panel responds to status bar");
    using(RoundedMenu popup=pet.CreateMenu())popup.Items["panel-enabled"].PerformClick();
    Check(!pet.State.panelEnabled&&!pet.PanelOpen,"menu switch immediately closes and disables an open panel");
    Point disabledLocation=pet.Location;pet.ActivateTarget("badge");pet.TogglePanel();
    Check(!pet.PanelOpen&&pet.Location==disabledLocation,"disabled status bar and direct panel command do nothing and do not move the pet");
    using(RoundedMenu popup=pet.CreateMenu())Check(!((ToolStripMenuItem)popup.Items["panel-enabled"]).Checked,"menu panel toggle reflects disabled state");
    pet.Pat();Check(pet.Frame=="happy"&&pet.State.mood==88,"panel disabled still allows character pat interaction");pet.State.mood=80;pet.Frame="idle";
    using(RoundedMenu popup=pet.CreateMenu())popup.Items["panel-enabled"].PerformClick();pet.ActivateTarget("badge");
    Check(pet.State.panelEnabled&&pet.PanelOpen,"re-enabling restores status bar panel click");
    // Render after panel transition completes without sleeping the GUI thread.
    Stopwatch wait=Stopwatch.StartNew();while(wait.ElapsedMilliseconds<120)Application.DoEvents();
    using(Bitmap b=pet.RenderBitmap()) {
     b.Save(Path.Combine(folder,"native-panel.png"));
     using(Bitmap panel=pet.PanelBitmap()) {
      Check(panel.Width==(int)Math.Round(380*pet.Dpi)&&panel.Height==(int)Math.Round(419*pet.Dpi),"panel is rasterized at physical display resolution");
      int differences=0,pixels=0;
      for(int y=(int)(100*pet.Dpi);y<(int)(140*pet.Dpi);y++)for(int x=(int)(50*pet.Dpi);x<(int)(260*pet.Dpi);x++) {
       Color expected=panel.GetPixel(x,y);if(expected.A!=255)continue;pixels++;if(b.GetPixel(x,y).ToArgb()!=expected.ToArgb())differences++;
      }
      Check(pixels>1000&&differences==0,"opaque title pixels reach the native window without a second resampling");
     }
     Check(pet.UiAssetScale>=pet.Dpi,"UI source resolution meets display DPI");
     bool wasSleeping=pet.State.sleeping,wasOpen=pet.PanelOpen;
     string uiFolder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","ui");
     if(pet.UiAssetScale!=1)uiFolder=Path.Combine(uiFolder,"dpi-"+Math.Round(pet.UiAssetScale*100));
     var labelVariants=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(uiFolder,"badge-labels.json")));
     for(int sleep=0;sleep<2;sleep++)for(int open=0;open<2;open++) {
      pet.State.sleeping=sleep==1;pet.PanelOpen=open==1;string variant="badge-"+sleep+"-"+open;
      using(Bitmap status=pet.BadgeBitmap()) {
       status.Save(Path.Combine(folder,variant+"-native.png"));
       bool complete=true;
       foreach(var labelObject in ((Dictionary<string,object>)labelVariants[variant]).Values) {
        var label=(Dictionary<string,object>)labelObject;string text=(string)label["text"];
        double left=Convert.ToDouble(label["x"])*pet.Dpi,labelTop=Convert.ToDouble(label["y"])*pet.Dpi,width=Convert.ToDouble(label["width"])*pet.Dpi,height=Convert.ToDouble(label["height"])*pet.Dpi;
        for(int character=0;character<text.Length;character++) {
         int ink=0;
         for(int y=(int)labelTop;y<(int)Math.Ceiling(labelTop+height);y++)for(int x=(int)(left+character*width/text.Length);x<(int)(left+(character+1)*width/text.Length);x++) {
          Color pixel=status.GetPixel(x,y);if(pixel.A>200&&pixel.R<220&&pixel.G<190&&pixel.B<205)ink++;
         }
         if(ink<3)complete=false;
        }
       }
       Check(complete,"all name and status characters are visible without clipping: "+variant);
      }
     }
     pet.State.sleeping=wasSleeping;pet.PanelOpen=wasOpen;
     using(Bitmap badge=pet.BadgeBitmap()) {
      badge.Save(Path.Combine(folder,"badge-native-text.png"));
      int ox=(int)Math.Round(95*pet.Dpi),oy=(int)Math.Round(663*pet.Dpi),difference=0;
      for(int y=(int)(17*pet.Dpi);y<(int)(38*pet.Dpi);y++)for(int x=(int)(32*pet.Dpi);x<(int)(120*pet.Dpi);x++) {
       Color source=badge.GetPixel(x,y),actual=b.GetPixel(ox+x,oy+y);
       if(Math.Abs(source.R-actual.R)>1||Math.Abs(source.G-actual.G)>1||Math.Abs(source.B-actual.B)>1)difference++;
      }
      Check(badge.Width==(int)Math.Round(190*pet.Dpi)&&difference==0,"status badge labels reach the window at physical pixels without scaling");
     }
     checks.Add(new{name="UI raster scale",displayScale=pet.Dpi,assetScale=pet.UiAssetScale});
    }
    Check(pet.PanelOpen,"badge opens original panel");
    pet.TogglePanel();
    // Let the panel and speech bubble disappear before inspecting blank pixels.
    wait.Restart();while(wait.ElapsedMilliseconds<3400)Application.DoEvents();
    foreach(double scale in new double[]{.5,.6,.73,.8,1,1.2}) {
     pet.State.scale=scale;
     foreach(string frame in new string[]{"idle","happy","tea","sleep"}) {
      pet.Frame=frame;pet.State.sleeping=frame=="sleep";
      using(Bitmap rendered=pet.RenderBitmap()) {
       foreach(PointF logical in new PointF[]{new PointF(46,500),new PointF(334,500),new PointF(190,380)}) {
        Point pixel=new Point((int)(logical.X*pet.Dpi),(int)(logical.Y*pet.Dpi));
        Check(rendered.GetPixel(pixel.X,pixel.Y).A==0&&pet.Target(pixel)=="","left/right/top blank pixels pass through at scale "+scale+" frame "+frame+" point "+logical);
        Point screen=pet.PointToScreen(pixel);
        Check(Native.SendMessage(pet.Handle,0x84,IntPtr.Zero,pack(screen)).ToInt32()==-1,"blank pixel rejects native hit test at scale "+scale+" frame "+frame+" point "+logical);
       }
       Point body=new Point((int)(190*pet.Dpi),(int)(600*pet.Dpi));
       Check(rendered.GetPixel(body.X,body.Y).A>100&&pet.Target(body)=="character","visible body stays interactive at scale "+scale+" frame "+frame);
      }
     }
     pet.State.sleeping=false;pet.Frame="idle";
     RectangleF a=new RectangleF(0,0,1536,864),r=Geometry.Companion(scale);PointF left=Geometry.Clamp(-9999,-9999,a,r),right=Geometry.Clamp(9999,9999,a,r);
     Check(Math.Abs(left.X+r.Left)<.51&&Math.Abs(left.Y+r.Top)<.51&&Math.Abs(right.X+r.Right-1536)<.51&&Math.Abs(right.Y+r.Bottom-864)<.51,"all four original movement limits at scale "+scale);
     Check(Math.Abs(1536-(right.X+Geometry.BoundarySprite(scale).Right)-44.125)<.51,"right-side character gap matches original large size at scale "+scale);
     using(Bitmap b=pet.RenderBitmap())b.Save(Path.Combine(folder,"native-scale-"+scale.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"));
    }
    pet.TogglePanel();
    pet.State.scale=1;pet.Feed(true);Check(pet.State.energy==95&&pet.State.mood==85&&pet.Frame=="tea","tea +20 energy +5 mood");
    pet.Pat();Check(pet.State.mood==93&&pet.Frame=="happy","pat +8 mood and happy frame");
    pet.Sleep(true);Check(pet.State.sleeping&&pet.Frame=="sleep","sleep frame");
    double energy=pet.State.energy,mood=pet.State.mood;pet.Feed(true);pet.Pat();Check(pet.State.energy==energy&&pet.State.mood==mood&&pet.Frame=="sleep","sleep blocks feeding and pat");
    pet.AdvanceMeters();Check(pet.State.energy==energy-1&&pet.State.mood==mood+1,"30-second energy and sleep mood rule");
    pet.Sleep(true);Check(!pet.State.sleeping&&pet.Frame=="idle","wake returns idle");
    energy=pet.State.energy;mood=pet.State.mood;pet.AdvanceMeters();
    Check(pet.State.energy==energy-1&&pet.State.mood==mood-1,"awake mood and energy both decrease by one every 30 seconds");
    pet.State.energy=0;pet.State.mood=0;pet.AdvanceMeters();
    Check(pet.State.energy==0&&pet.State.mood==0,"natural decay clamps both meters at zero");
    pet.Sleep(false);pet.State.mood=100;pet.AdvanceMeters();
    Check(pet.State.energy==0&&pet.State.mood==100,"sleep mood recovery clamps at 100 while energy stays at zero");
    pet.Sleep(false);
    pet.State.energy=99;pet.State.mood=99;pet.Feed(true);pet.Pat();Check(pet.State.energy==100&&pet.State.mood==100,"meters clamp to 100");
    bool top=pet.State.topmost;pet.ActivateTarget("topmost");Check(pet.TopMost!=top&&pet.State.topmost!=top,"topmost toggle");
    foreach(string id in new string[]{"size-slider","size-reset"}) {
     RectangleF button=pet.Rect(id);
     Check(pet.Target(new Point((int)((button.X+button.Width/2)*pet.Dpi),(int)((button.Y+button.Height/2)*pet.Dpi)))==id,"slider and reset hit target "+id);
    }
    RectangleF slider=pet.Rect("size-slider");Point anchored=pet.Location;
    Point sliderLeft=new Point((int)Math.Round((slider.Left+6)*pet.Dpi),(int)Math.Round((slider.Top+slider.Height/2)*pet.Dpi));
    Native.SendMessage(pet.Handle,0x201,(IntPtr)1,pack(sliderLeft));
    Check(pet.State.scale==.5&&pet.Capture&&!pet.Dragging,"slider press previews 50 percent and captures mouse without moving pet");
    Point sliderRight=new Point((int)Math.Round((slider.Right+40)*pet.Dpi),sliderLeft.Y);
    Native.SendMessage(pet.Handle,0x200,(IntPtr)1,pack(sliderRight));
    Check(pet.State.scale==1.2&&pet.PanelOpen&&pet.Location==anchored&&pet.Rect("size-slider")==slider,"slider dragging clamps to 120 percent and keeps panel fixed");
    Native.SendMessage(pet.Handle,0x202,IntPtr.Zero,pack(sliderRight));
    Check(!pet.Capture&&!pet.Dragging&&pet.State.scale==1.2,"slider release outside control ends capture");
    pet.SetScale(.73,false);pet.ActivateTarget("size-reset");Check(pet.State.scale==1,"reset restores 100 percent");
    pet.SetScale(.73,false);
    using(Bitmap panel=pet.PanelBitmap())panel.Save(Path.Combine(folder,"panel-size-slider.png"));
    pet.State.scale=1;pet.Location=pet.ClampLocation(new Point(500,-200),null);Point down=new Point(pet.Left+(int)(180*pet.Dpi),pet.Top+(int)(520*pet.Dpi));pet.TestPress(down);
    Check(pet.Cursor.Handle!=grabHandle&&!pet.Dragging,"press immediately uses grabbing cursor before crossing the drag threshold, matching CSS active");
    pet.MoveGesture(new Point(down.X+(int)(4*pet.Dpi),down.Y));Check(!pet.Dragging,"4px remains click");
    Point threshold=new Point(down.X+(int)(7*pet.Dpi),down.Y);pet.MoveGesture(threshold);Check(pet.Dragging&&!pet.PanelOpen,"above 5px starts drag and closes panel");
    Check(pet.Cursor.Handle!=grabHandle&&pet.Cursor.Handle!=Cursors.SizeAll.Handle,"active dragging uses closed hand cursor");
    Point start=pet.Location;Point dest=new Point(threshold.X+(int)(30*pet.Dpi),threshold.Y+(int)(20*pet.Dpi));
    pet.MoveGesture(dest);Check(Math.Abs(pet.Left-start.X-30*pet.Dpi)<=2&&Math.Abs(pet.Top-start.Y-20*pet.Dpi)<=2,"native drag follows mouse delta: start="+start+" final="+pet.Location+" dpi="+pet.Dpi);
    Stopwatch dragWatch=Stopwatch.StartNew();for(int i=0;i<120;i++)pet.MoveGesture(new Point(dest.X+i%30,dest.Y+i%10));dragWatch.Stop();
    checks.Add(new{name="120 event-driven window moves",totalMs=dragWatch.Elapsed.TotalMilliseconds});
    pet.EndDrag();Check(!pet.Dragging,"release ends drag");
    pet.Hide();Check(!pet.Visible,"hide");pet.Show();Check(pet.Visible,"show restores");
    pet.State.scale=1;pet.ResetPosition();Check(!pet.PanelOpen,"reset collapses panel");
    foreach(double bubbleScale in new double[]{.5,.73,1.2}) {
     pet.State.scale=bubbleScale;pet.ResetPosition();pet.Say("今天也很努力呢，先喝杯茶，再稍微休息一下吧");
     using(Bitmap preview=pet.RenderBitmap()) {
      using(Graphics graphics=Graphics.FromImage(preview)) {
       graphics.ScaleTransform(pet.Dpi,pet.Dpi);
       RectangleF bubble=pet.BubbleBounds(graphics),badgeBounds=pet.Rect("badge");
       double bodyCenter=Geometry.Center(bubbleScale)+(214-256)*Math.Min(220*bubbleScale/512,245*bubbleScale/576);
       Check(Math.Abs(bubble.Left+bubble.Width/2-bodyCenter)<.01&&Math.Abs(bubble.Left+bubble.Width/2-(badgeBounds.Left+badgeBounds.Width/2))<.01,"speech bubble, character body and badge share one axis at right edge, scale "+bubbleScale);
       Check(bubble.Right+pet.Left/pet.Dpi<=Screen.PrimaryScreen.WorkingArea.Right/pet.Dpi-3.9,"speech bubble stays inside right screen edge, scale "+bubbleScale);
       string originalBubble=pet.Bubble;
       pet.Bubble="辉夜";RectangleF singleLine=pet.BubbleBounds(graphics);
       foreach(string shortSentence in new string[]{"能多陪你一会儿也不是不可以。","能陪你一会……也不是不可以"}) {
        pet.Bubble=shortSentence;RectangleF shortBounds=pet.BubbleBounds(graphics);
        Check(Math.Abs(shortBounds.Left+shortBounds.Width/2-bodyCenter)<.01&&Math.Abs(shortBounds.Bottom-singleLine.Bottom)<.01,"short sentences preserve shared axis and bottom even when wrapping at scale "+bubbleScale+": "+shortSentence);
        Check(shortBounds.Right+pet.Left/pet.Dpi<=Screen.PrimaryScreen.WorkingArea.Right/pet.Dpi-3.9,"short sentence stays inside right edge at scale "+bubbleScale);
       }
       if(bubbleScale==.5)Check(bubble.Height>singleLine.Height,"speech wraps when centered width is insufficient");
       pet.Bubble="辉夜\n辉夜";RectangleF twoLines=pet.BubbleBounds(graphics);
       pet.Bubble="辉夜\n辉夜\n辉夜";RectangleF threeLines=pet.BubbleBounds(graphics);
       Check(twoLines.Height>singleLine.Height&&threeLines.Height>twoLines.Height&&Math.Abs(singleLine.Bottom-twoLines.Bottom)<.01&&Math.Abs(singleLine.Bottom-threeLines.Bottom)<.01,"wrapped speech grows upward with a fixed last-line bottom, scale "+bubbleScale);
       pet.Bubble=originalBubble;
      }
      preview.Save(Path.Combine(folder,"bubble-right-"+bubbleScale.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"));
     }
    }
    pet.State.scale=1;pet.ResetPosition();
    int before=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);Stopwatch perf=Stopwatch.StartNew();
    for(int i=0;i<180;i++){if(i%3==0)pet.Pat();else if(i%3==1)pet.Feed(true);else pet.Sleep(true);using(Bitmap frame=pet.RenderBitmap()){CheckFrame(frame,pet.Dpi);}pet.PaintFrame();}
    perf.Stop();int after=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);
    Check(after-before<12,"180 action/render cycles do not leak GDI handles");
    checks.Add(new{name="180 action cycles with two renders and alpha inspection per cycle; not frame rate or drag latency",cycles=180,totalMs=perf.Elapsed.TotalMilliseconds,gdiBefore=before,gdiAfter=after});
    pet.State.sleeping=false;pet.Frame="idle";pet.State.scale=1;pet.TogglePanel();
    wait.Restart();while(wait.ElapsedMilliseconds<120)Application.DoEvents();
    using(Bitmap b=pet.RenderBitmap())b.Save(Path.Combine(folder,"native-final.png"));
    success=true;pet.Close();
   }
  }catch(Exception e){error=e.ToString();Environment.ExitCode=1;}
  File.WriteAllText(Path.Combine(folder,"self-test.json"),new JavaScriptSerializer().Serialize(new{success=success,error=error,checks=checks}));
 }
 static void CheckFrame(Bitmap b,float dpi) {if(b.GetPixel((int)(190*dpi),(int)(570*dpi)).A<100)throw new Exception("Empty character during action switch");}
 static int TaskbarCandidates() {
  int count=0;uint current=(uint)Process.GetCurrentProcess().Id;
  Native.EnumWindows(delegate(IntPtr window,IntPtr argument){uint id;Native.GetWindowThreadProcessId(window,out id);if(id==current&&Native.IsWindowVisible(window)){int ex=Native.GetWindowLong(window,-20);if((ex&0x40000)!=0||((ex&0x80)==0&&(ex&0x08000000)==0&&Native.GetWindow(window,4)==IntPtr.Zero))count++;}return true;},IntPtr.Zero);
  return count;
 }
}
}
