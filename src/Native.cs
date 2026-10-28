using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
namespace KaguyaNative {
    internal static class Native
    {
        internal const int Wake=0x8001, Status=0x8002;
        [StructLayout(LayoutKind.Sequential)] internal struct Point { internal int X,Y; internal Point(int x,int y){X=x;Y=y;} }
        [StructLayout(LayoutKind.Sequential)] internal struct Size { internal int W,H; internal Size(int w,int h){W=w;H=h;} }
        [StructLayout(LayoutKind.Sequential)] internal struct Rect { internal int Left,Top,Right,Bottom; }
        [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct Blend { internal byte Op,Flags,Alpha,Format; }
        [StructLayout(LayoutKind.Sequential)] internal struct BitmapInfo
        {
            internal uint Size; internal int Width,Height; internal ushort Planes,BitCount;
            internal uint Compression,SizeImage; internal int XPels,YPels; internal uint Used,Important;
        }
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr FindWindow(string cls,string text);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr h,int m,IntPtr w,IntPtr l);
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
        [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern IntPtr GetFocus();
        [DllImport("user32.dll")] internal static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
        [DllImport("user32.dll")] internal static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr window,uint command);
        internal delegate bool WindowVisitor(IntPtr window,IntPtr argument);
        [DllImport("user32.dll")] internal static extern bool EnumWindows(WindowVisitor visitor,IntPtr argument);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(IntPtr h,StringBuilder name,int count);
        [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] internal static extern int DwmRect(IntPtr h,int attr,out Rect value,int size);
        [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] internal static extern int DwmInt(IntPtr h,int attr,out int value,int size);
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr h,out Rect r);
        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr h,int index);
        [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr h,int index,int value);
        internal delegate IntPtr MouseHook(int code,IntPtr message,IntPtr data);
        [DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int kind,MouseHook callback,IntPtr module,uint thread);
        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
        [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr h,IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr h);
        [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr h);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr h);
        [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateDIBSection(IntPtr h,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
        [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dest,ref Point position,ref Size size,IntPtr src,ref Point origin,uint key,ref Blend blend,uint flags);
        [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr icon);
        [DllImport("user32.dll")] internal static extern int GetGuiResources(IntPtr process,int flags);
    }

    internal sealed class LayeredSurface:IDisposable
    {
        private IntPtr dc,bmp,old,bits;
        private byte[] buffer;
        private int width,height;
        internal void Update(IntPtr window,Bitmap image,Point location)
        {
            if(width!=image.Width||height!=image.Height)Create(image.Width,image.Height);
            BitmapData locked=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
            try{Marshal.Copy(locked.Scan0,buffer,0,buffer.Length);Marshal.Copy(buffer,0,bits,buffer.Length);}finally{image.UnlockBits(locked);}
            Native.Point destination=new Native.Point(location.X,location.Y),origin=new Native.Point(0,0);Native.Size size=new Native.Size(width,height);
            Native.Blend blend=new Native.Blend{Op=0,Flags=0,Alpha=255,Format=1};
            if(!Native.UpdateLayeredWindow(window,IntPtr.Zero,ref destination,ref size,dc,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        private void Create(int w,int h)
        {
            Dispose();width=w;height=h;buffer=new byte[w*h*4];dc=Native.CreateCompatibleDC(IntPtr.Zero);
            Native.BitmapInfo info=new Native.BitmapInfo{Size=40,Width=w,Height=-h,Planes=1,BitCount=32,Compression=0,SizeImage=(uint)(w*h*4)};
            bmp=Native.CreateDIBSection(dc,ref info,0,out bits,IntPtr.Zero,0);
            if(bmp==IntPtr.Zero)throw new InvalidOperationException("无法创建透明窗口");old=Native.SelectObject(dc,bmp);
        }
        internal bool Hit(int x,int y){return buffer!=null&&x>=0&&y>=0&&x<width&&y<height&&buffer[(y*width+x)*4+3]>24;}
        public void Dispose(){if(dc!=IntPtr.Zero){if(old!=IntPtr.Zero)Native.SelectObject(dc,old);if(bmp!=IntPtr.Zero)Native.DeleteObject(bmp);Native.DeleteDC(dc);}dc=bmp=old=bits=IntPtr.Zero;width=height=0;buffer=null;}
    }


}
