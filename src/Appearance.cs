using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace KaguyaNative {
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
        internal RoundedMenu(float scale) {
            dpi=scale;AutoSize=false;ShowImageMargin=false;ShowCheckMargin=false;
            BackColor=Color.White;ForeColor=Color.FromArgb(20,20,20);
            menuFont=new Font("Microsoft YaHei UI",11*scale,FontStyle.Regular,GraphicsUnit.Pixel);Font=menuFont;
            Padding=new Padding(0,(int)Math.Round(12*scale),0,(int)Math.Round(12*scale));
            Renderer=new MenuRenderer(scale);DropShadowEnabled=false;
        }
        protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle=(p.ExStyle|0x80|0x08000000)&~0x40000;if(PopupOwner!=IntPtr.Zero)p.Parent=PopupOwner;return p;}}
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
        protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing&&menuFont!=null)menuFont.Dispose();}
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
