param([switch]$RefreshCache)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class KaguyaShellIcons {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct FileInfo {
        public IntPtr Icon;
        public int Index;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)] public string TypeName;
    }
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
    public static extern IntPtr SHGetFileInfo(string path, uint attributes, out FileInfo info, uint size, uint flags);
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
    public static extern uint ExtractIconEx(string path, int index, out IntPtr large, out IntPtr small, uint count);
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
    public static extern void SHChangeNotify(uint change, uint flags, string item1, string item2);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
}
'@
$root = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $root 'KaguyaPet.exe'
$output = Join-Path $root 'verification'
if ($RefreshCache) {
    # Flush the EXE update notification before asking the shell for its cached icon.
    [KaguyaShellIcons]::SHChangeNotify(0x2000, 0x1005, $exe, $null)
    [KaguyaShellIcons]::SHChangeNotify(0x08000000, 0x1000, $null, $null)
}
function Save-Icon([IntPtr]$Handle, [string]$Name) {
    if ($Handle -eq [IntPtr]::Zero) { throw "Missing icon: $Name" }
    $icon = [Drawing.Icon]::FromHandle($Handle)
    $bitmap = $icon.ToBitmap()
    try {
        $bitmap.Save((Join-Path $output ($Name + '.png')), [Drawing.Imaging.ImageFormat]::Png)
        $visible = 0; $transparent = 0; $colored = 0
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
            for ($x = 0; $x -lt $bitmap.Width; $x++) {
                $pixel = $bitmap.GetPixel($x, $y)
                if ($pixel.A -eq 0) { $transparent++ }
                if ($pixel.A -gt 0) { $visible++; if ([Math]::Max($pixel.R, [Math]::Max($pixel.G, $pixel.B)) -gt 80) { $colored++ } }
            }
        }
        [pscustomobject]@{Name=$Name; Width=$bitmap.Width; Height=$bitmap.Height; Visible=$visible; Transparent=$transparent; Colored=$colored; Passed=($visible -gt 0 -and $transparent -gt 0 -and $colored -gt 0)}
    } finally { $bitmap.Dispose(); $icon.Dispose(); [void][KaguyaShellIcons]::DestroyIcon($Handle) }
}
$results = @()
[IntPtr]$large = [IntPtr]::Zero; [IntPtr]$small = [IntPtr]::Zero
if ([KaguyaShellIcons]::ExtractIconEx($exe, 0, [ref]$large, [ref]$small, 1) -eq 0) { throw 'EXE icon extraction failed' }
$results += Save-Icon $small 'exe-icon-small'
$results += Save-Icon $large 'exe-icon-large'
foreach ($size in @('small', 'large')) {
    $info = [KaguyaShellIcons+FileInfo]::new()
    $flags = [uint32]0x100
    if ($size -eq 'small') { $flags = $flags -bor 1 }
    if ([KaguyaShellIcons]::SHGetFileInfo($exe, 0, [ref]$info, [Runtime.InteropServices.Marshal]::SizeOf($info), $flags) -eq [IntPtr]::Zero) { throw 'Shell icon lookup failed' }
    $results += Save-Icon $info.Icon ('shell-icon-' + $size)
}
$results | ConvertTo-Json | Set-Content (Join-Path $output 'icon-check.json') -Encoding UTF8
$results | ConvertTo-Json -Compress
if ($results.Passed -contains $false) { throw 'An EXE or shell icon is blank or opaque' }
