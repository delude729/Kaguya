using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace KaguyaNative {
    internal sealed class WaterIntervalItem:ToolStripControlHost {
        internal readonly TextBox Input;
        readonly Func<int> current;
        readonly Func<int,bool> save;
        bool cancelled;
        internal WaterIntervalItem(float dpi,Func<int> read,Func<int,bool> write):base(new IntervalRow(dpi)) {
            Name="water-interval";current=read;save=write;Input=((IntervalRow)Control).Input;
            Input.MouseDown+=delegate {RoundedMenu menu=Owner as RoundedMenu;if(menu!=null)menu.EnableKeyboardInput();Input.Focus();};
            Input.Enter+=delegate {Input.BeginInvoke((Action)delegate {if(!Input.IsDisposed)Input.SelectAll();});};
            Input.KeyPress+=delegate(object sender,KeyPressEventArgs e){if(!char.IsControl(e.KeyChar)&&!char.IsDigit(e.KeyChar))e.Handled=true;};
            Input.KeyDown+=delegate(object sender,KeyEventArgs e){
                if(e.KeyCode==Keys.Enter){e.Handled=true;e.SuppressKeyPress=true;if(Commit())((ContextMenuStrip)Owner).Close();}
                else if(e.KeyCode==Keys.Escape){cancelled=true;Input.Text=current().ToString();e.Handled=true;e.SuppressKeyPress=true;((ContextMenuStrip)Owner).Close();}
            };
            Input.Leave+=delegate {if(!cancelled)Commit();};
        }
        internal void BeginSession(){cancelled=false;Input.Text=current().ToString();Input.ForeColor=Color.FromArgb(68,60,69);}
        internal bool Commit(){int minutes;if(!int.TryParse(Input.Text,out minutes)||minutes<1||minutes>1440){Input.ForeColor=Color.Firebrick;return false;}Input.ForeColor=Color.FromArgb(68,60,69);return minutes==current()||save(minutes);}
        internal void EndSession(){if(!cancelled)Commit();Input.Text=current().ToString();}
    }
    internal sealed class IntervalRow:UserControl {
        internal readonly TextBox Input;
        readonly Font textFont;
        readonly float scale;
        internal IntervalRow(float dpi) {
            scale=dpi;
            AutoScaleMode=AutoScaleMode.None;BackColor=Color.White;Size=new Size((int)(154*dpi),(int)(28*dpi));
            textFont=new Font("Microsoft YaHei UI",11*dpi,FontStyle.Regular,GraphicsUnit.Pixel);Font=textFont;
            Panel pill=new Panel{BackColor=Color.FromArgb(244,234,240),Bounds=new Rectangle((int)(69*dpi),(int)(4*dpi),(int)(43*dpi),(int)(20*dpi))};
            using(GraphicsPath shape=new GraphicsPath()){float w=pill.Width,h=pill.Height,r=8*dpi;shape.AddArc(0,0,r,r,180,90);shape.AddArc(w-r,0,r,r,270,90);shape.AddArc(w-r,h-r,r,r,0,90);shape.AddArc(0,h-r,r,r,90,90);shape.CloseFigure();pill.Region=new Region(shape);}
            Input=new TextBox{BorderStyle=BorderStyle.None,TextAlign=HorizontalAlignment.Center,MaxLength=4,BackColor=pill.BackColor,Font=textFont,Location=new Point(0,(int)(3*dpi)),Width=pill.Width};pill.Controls.Add(Input);Controls.Add(pill);
        }
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);TextFormatFlags flags=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding;TextRenderer.DrawText(e.Graphics,"提醒间隔",textFont,new Rectangle((int)(14*scale),0,(int)(52*scale),Height),Color.FromArgb(20,20,20),flags);TextRenderer.DrawText(e.Graphics,"分钟",textFont,new Rectangle((int)(119*scale),0,(int)(32*scale),Height),Color.FromArgb(20,20,20),flags);}
        protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)textFont.Dispose();}
    }
    // Direct equivalents of the original CSS cursor: grab / cursor: grabbing.
    internal sealed class HandCursors:IDisposable {
        internal readonly Cursor Grab,Grabbing;
        static string PathFor(bool closed){return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","cursors",closed?"hand_grabbing.cur":"hand_grab.cur");}
        internal HandCursors(){Grab=new Cursor(PathFor(false));Grabbing=new Cursor(PathFor(true));}
        internal static Bitmap Bitmap(bool closed){using(Cursor cursor=new Cursor(PathFor(closed)))using(Icon icon=Icon.FromHandle(cursor.Handle))return icon.ToBitmap();}
        public void Dispose(){Grab.Dispose();Grabbing.Dispose();}
    }
    internal sealed class RoundedMenu:ContextMenuStrip {
        readonly float dpi;
        readonly Font menuFont;
        internal IntPtr PopupOwner;
        bool keyboardInput;
        IntPtr outsideClickHook;
        Native.MouseHook outsideClickCallback;
        internal RoundedMenu(float scale) {
            dpi=scale;AutoSize=false;ShowImageMargin=false;ShowCheckMargin=false;
            BackColor=Color.White;ForeColor=Color.FromArgb(20,20,20);
            menuFont=new Font("Microsoft YaHei UI",11*scale,FontStyle.Regular,GraphicsUnit.Pixel);Font=menuFont;
            Padding=new Padding(0,(int)Math.Round(12*scale),0,(int)Math.Round(12*scale));
            Renderer=new MenuRenderer(scale);DropShadowEnabled=false;
        }
        protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle=(p.ExStyle|0x80|0x08000000)&~0x40000;if(PopupOwner!=IntPtr.Zero)p.Parent=PopupOwner;return p;}}
        internal void EnableKeyboardInput(){keyboardInput=true;Native.SetWindowLong(Handle,-20,Native.GetWindowLong(Handle,-20)&~0x08000000);Native.SetForegroundWindow(Handle);}
        protected override void WndProc(ref Message m){if(m.Msg==0x21){WaterIntervalItem field=Items["water-interval"] as WaterIntervalItem;if(field!=null&&field.Input.RectangleToScreen(field.Input.ClientRectangle).Contains(Cursor.Position)){EnableKeyboardInput();m.Result=(IntPtr)1;return;}}base.WndProc(ref m);}
        protected override void OnOpening(System.ComponentModel.CancelEventArgs e){WaterIntervalItem field=Items["water-interval"] as WaterIntervalItem;if(field!=null)field.BeginSession();base.OnOpening(e);}
        protected override void OnClosing(ToolStripDropDownClosingEventArgs e){
            if(e.CloseReason==ToolStripDropDownCloseReason.ItemClicked)
                foreach(ToolStripItem item in Items)if(item.Selected&&(string)item.Tag=="toggle"){e.Cancel=true;break;}
            base.OnClosing(e);
        }
        protected override void OnOpened(EventArgs e){
            base.OnOpened(e);
            // A nonactivating tray menu may never receive deactivation or an
            // outside click through WinForms' thread-local menu filter.
            if(outsideClickHook==IntPtr.Zero){outsideClickCallback=ObserveMouse;outsideClickHook=Native.SetWindowsHookEx(14,outsideClickCallback,Native.GetModuleHandle(null),0);}
        }
        IntPtr ObserveMouse(int code,IntPtr message,IntPtr data){
            int msg=message.ToInt32();
            if(code>=0&&(msg==0x201||msg==0x204||msg==0x207||msg==0x20b)&&Visible){
                Native.Point point=(Native.Point)System.Runtime.InteropServices.Marshal.PtrToStructure(data,typeof(Native.Point));
                Point local=PointToClient(new Point(point.X,point.Y));
                if(!ClientRectangle.Contains(local)||(Region!=null&&!Region.IsVisible(local)))
                    BeginInvoke((Action)delegate{if(!IsDisposed&&Visible)Close(ToolStripDropDownCloseReason.AppClicked);});
            }
            // Observe only: the original click must still reach its target.
            return Native.CallNextHookEx(outsideClickHook,code,message,data);
        }
        void StopObservingMouse(){if(outsideClickHook!=IntPtr.Zero){Native.UnhookWindowsHookEx(outsideClickHook);outsideClickHook=IntPtr.Zero;}outsideClickCallback=null;}
        protected override void OnClosed(ToolStripDropDownClosedEventArgs e){StopObservingMouse();WaterIntervalItem field=Items["water-interval"] as WaterIntervalItem;if(field!=null)field.EndSession();if(keyboardInput){keyboardInput=false;Native.SetWindowLong(Handle,-20,Native.GetWindowLong(Handle,-20)|0x08000000);}base.OnClosed(e);}
        internal static Point AnchoredLocation(Point cursor,Size size,Rectangle workArea) {
            return new Point(Math.Max(workArea.Left,Math.Min(cursor.X,workArea.Right-size.Width)),
                Math.Max(workArea.Top,Math.Min(cursor.Y-size.Height,workArea.Bottom-size.Height)));
        }
        internal void ShowAnchored(Control owner,Point cursor) {
            Point location=AnchoredLocation(cursor,Size,Screen.FromPoint(cursor).WorkingArea);
            // AboveRight means the lower-left corner is the supplied anchor.
            Point anchor=new Point(location.X,location.Y+Height);
            Show(owner,owner.PointToClient(anchor),ToolStripDropDownDirection.AboveRight);
        }
        internal void Prepare() {
            int width=(int)Math.Round(154*dpi),row=(int)Math.Round(28*dpi),height=Padding.Vertical;
            foreach(ToolStripItem item in Items) {
                item.AutoSize=false;item.Margin=Padding.Empty;item.Padding=Padding.Empty;
                item.Size=new Size(width,item is ToolStripSeparator?(int)Math.Round(18*dpi):row);
                item.TextAlign=ContentAlignment.MiddleCenter;height+=item.Height;
            }
            Size=new Size(width,height);UpdateShape();
        }
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);UpdateShape();}
        protected override void Dispose(bool disposing){if(disposing)StopObservingMouse();base.Dispose(disposing);if(disposing&&menuFont!=null)menuFont.Dispose();}
        void UpdateShape() {
            if(Width<2||Height<2)return;float d=Math.Min(28*(dpi==0?1:dpi),Math.Min(Width,Height));
            using(GraphicsPath path=new GraphicsPath()) {
                path.AddArc(0,0,d,d,180,90);path.AddArc(Width-d,0,d,d,270,90);
                path.AddArc(Width-d,Height-d,d,d,0,90);path.AddArc(0,Height-d,d,d,90,90);path.CloseFigure();
                Region previous=Region;Region=new Region(path);if(previous!=null)previous.Dispose();
            }
        }
    }
    internal sealed class MenuRenderer:ToolStripProfessionalRenderer {
        readonly float dpi;
        internal MenuRenderer(float scale){dpi=scale;RoundedEdges=false;}
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Color.White);}
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){}
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){}
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
            using(Brush b=new SolidBrush(e.Item.Selected&&e.Item.Enabled?Color.FromArgb(242,242,242):Color.White))e.Graphics.FillRectangle(b,new Rectangle(Point.Empty,e.Item.Size));
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
            Rectangle rect=new Rectangle(Point.Empty,e.Item.Size);
            TextRenderer.DrawText(e.Graphics,e.Text,e.TextFont,rect,e.Item.Enabled?Color.FromArgb(20,20,20):Color.FromArgb(165,165,165),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding);
            if((string)e.Item.Tag=="toggle") {
                ToolStripMenuItem item=e.Item as ToolStripMenuItem;bool on=item!=null&&item.Checked;
                float w=20*dpi,h=11*dpi,x=e.Item.Width-27*dpi,y=(e.Item.Height-h)/2;
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(Pen track=new Pen(on?Color.FromArgb(180,93,118):Color.FromArgb(190,190,190),h)){track.StartCap=track.EndCap=LineCap.Round;e.Graphics.DrawLine(track,x+h/2,y+h/2,x+w-h/2,y+h/2);}
                float r=h-4*dpi;float knob=on?x+w-2*dpi-r:x+2*dpi;
                e.Graphics.FillEllipse(Brushes.White,knob,y+2*dpi,r,r);
            }
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
            int y=e.Item.Height/2;using(Pen p=new Pen(Color.FromArgb(218,229,248),Math.Max(1,dpi*.6f)))e.Graphics.DrawLine(p,0,y,e.Item.Width,y);
        }
    }
}
