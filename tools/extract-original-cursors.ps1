param([Parameter(Mandatory=$true)][string]$ElectronExe)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class OriginalCursorResources {
 public delegate bool Visitor(IntPtr module,IntPtr type,IntPtr name,IntPtr arg);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string path,IntPtr file,uint flags);
 [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
 [DllImport("kernel32.dll")] static extern bool EnumResourceNames(IntPtr module,IntPtr type,Visitor callback,IntPtr arg);
 [DllImport("kernel32.dll")] static extern IntPtr FindResource(IntPtr module,IntPtr name,IntPtr type);
 [DllImport("kernel32.dll")] static extern IntPtr LoadResource(IntPtr module,IntPtr resource);
 [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr resource);
 [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr module,IntPtr resource);
 static byte[] Read(IntPtr module,int type,int id){IntPtr resource=FindResource(module,(IntPtr)id,(IntPtr)type);byte[] data=new byte[SizeofResource(module,resource)];Marshal.Copy(LockResource(LoadResource(module,resource)),data,0,data.Length);return data;}
 public static int[] Extract(string exe,string output){
  Directory.CreateDirectory(output);var ids=new List<int>();IntPtr module=LoadLibraryEx(exe,IntPtr.Zero,2);if(module==IntPtr.Zero)throw new Exception("Cannot read Electron resource module");
  try{
   EnumResourceNames(module,(IntPtr)12,delegate(IntPtr m,IntPtr t,IntPtr n,IntPtr a){ids.Add(n.ToInt32());return true;},IntPtr.Zero);
   foreach(int id in ids){byte[] group=Read(module,12,id);int count=BitConverter.ToUInt16(group,4);var frames=new List<byte[]>();var widths=new List<int>();var heights=new List<int>();
    for(int i=0;i<count;i++){int pos=6+i*14;int imageId=BitConverter.ToUInt16(group,pos+12);byte[] frame=Read(module,1,imageId);frames.Add(frame);widths.Add(BitConverter.ToInt32(frame,8));heights.Add(BitConverter.ToInt32(frame,12)/2);}
    using(var stream=File.Create(Path.Combine(output,id+".cur")))using(var writer=new BinaryWriter(stream)){
     writer.Write((ushort)0);writer.Write((ushort)2);writer.Write((ushort)count);int offset=6+16*count;
     for(int i=0;i<count;i++){byte[] frame=frames[i];writer.Write((byte)widths[i]);writer.Write((byte)heights[i]);writer.Write((byte)0);writer.Write((byte)0);writer.Write(BitConverter.ToUInt16(frame,0));writer.Write(BitConverter.ToUInt16(frame,2));writer.Write(frame.Length-4);writer.Write(offset);offset+=frame.Length-4;}
     foreach(byte[] frame in frames)writer.Write(frame,4,frame.Length-4);
    }
   }
  }finally{FreeLibrary(module);}return ids.ToArray();
 }
}
'@
$output=Join-Path $PSScriptRoot '..\verification\original-cursors'
$ids=[OriginalCursorResources]::Extract($ElectronExe,$output)
$atlas=New-Object Drawing.Bitmap 420,([int][Math]::Ceiling($ids.Count/6.0)*95)
$g=[Drawing.Graphics]::FromImage($atlas);$g.Clear([Drawing.Color]::LightGray)
$font=New-Object Drawing.Font 'Consolas',9
for($i=0;$i -lt $ids.Count;$i++){
 $cursor=New-Object Windows.Forms.Cursor (Join-Path $output ($ids[$i].ToString()+'.cur'))
 $x=($i%6)*70;$y=[int][Math]::Floor($i/6.0)*95
 $cursor.DrawStretched($g,(New-Object Drawing.Rectangle ($x+4),($y+4),60,60))
 $g.DrawString($ids[$i].ToString(),$font,[Drawing.Brushes]::Black,$x+7,$y+68)
 $cursor.Dispose()
}
$atlas.Save((Join-Path $output 'atlas.png'));$font.Dispose();$g.Dispose();$atlas.Dispose()
$ids
