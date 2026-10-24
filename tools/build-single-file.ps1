$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stage = Join-Path $root 'verification\single-file-build'
$release = Join-Path $root '单文件发布版'
[void][IO.Directory]::CreateDirectory($stage)
[void][IO.Directory]::CreateDirectory($release)
$utf8 = [Text.UTF8Encoding]::new($false)
function Replace-Once([string]$Source, [string]$Before, [string]$After) {
    $count = [regex]::Matches($Source, [regex]::Escape($Before)).Count
    if ($count -ne 1) { throw "Expected exactly one source anchor (found $count): $Before" }
    return $Source.Replace($Before, $After)
}
# Generate resource-loading variants only; the working version's sources stay intact.
$pet = [IO.File]::ReadAllText((Join-Path $root 'src\Kaguya.cs'))
$pet = Replace-Once $pet 'using(Bitmap src=new Bitmap(Path.Combine(root,path)))' 'using(Stream source=EmbeddedAssets.Open(Path.Combine(root,path)))using(Bitmap src=new Bitmap(source))'
$pet = Replace-Once $pet 'using(Bitmap src=new Bitmap(Path.Combine(root,uiDirectory,name+".png")))' 'using(Stream source=EmbeddedAssets.Open(Path.Combine(root,uiDirectory,name+".png")))using(Bitmap src=new Bitmap(source))'
$pet = Replace-Once $pet 'File.ReadAllText(Path.Combine(root,"assets","ui","layout.json"))' 'EmbeddedAssets.ReadAllText(Path.Combine(root,"assets","ui","layout.json"))'
$pet = Replace-Once $pet 'Directory.GetDirectories(Path.Combine(root,"assets","ui"),"dpi-*")' 'EmbeddedAssets.UiDirectories(Path.Combine(root,"assets","ui"))'
$pet = Replace-Once $pet 'File.Exists(Path.Combine(folder,"panel-0-1-1.png"))' 'EmbeddedAssets.Exists(Path.Combine(folder,"panel-0-1-1.png"))'
$pet = Replace-Once $pet 'File.Exists(metadata)' 'EmbeddedAssets.Exists(metadata)'
$pet = Replace-Once $pet 'File.ReadAllText(metadata)' 'EmbeddedAssets.ReadAllText(metadata)'
[IO.File]::WriteAllText((Join-Path $stage 'Kaguya.cs'), $pet, $utf8)
$appearance = [IO.File]::ReadAllText((Join-Path $root 'src\Appearance.cs'))
$appearance = Replace-Once $appearance 'internal HandCursors(){Grab=new Cursor(PathFor(false));Grabbing=new Cursor(PathFor(true));}' 'readonly Stream grabSource,grabbingSource; internal HandCursors(){grabSource=EmbeddedAssets.Open(PathFor(false));grabbingSource=EmbeddedAssets.Open(PathFor(true));Grab=new Cursor(grabSource);Grabbing=new Cursor(grabbingSource);}'
$appearance = Replace-Once $appearance 'using(Cursor cursor=new Cursor(PathFor(closed)))' 'using(Stream source=EmbeddedAssets.Open(PathFor(closed)))using(Cursor cursor=new Cursor(source))'
$appearance = Replace-Once $appearance 'public void Dispose(){Grab.Dispose();Grabbing.Dispose();}' 'public void Dispose(){Grab.Dispose();Grabbing.Dispose();grabSource.Dispose();grabbingSource.Dispose();}'
[IO.File]::WriteAllText((Join-Path $stage 'Appearance.cs'), $appearance, $utf8)
$checks = [IO.File]::ReadAllText((Join-Path $root 'src\Checks.cs'))
$checks = Replace-Once $checks 'File.ReadAllText(Path.Combine(folder,"original-geometry.json"))' 'EmbeddedAssets.ReadAllText(Path.Combine(folder,"original-geometry.json"))'
$checks = Replace-Once $checks 'File.ReadAllText(Path.Combine(uiFolder,"badge-labels.json"))' 'EmbeddedAssets.ReadAllText(Path.Combine(uiFolder,"badge-labels.json"))'
[IO.File]::WriteAllText((Join-Path $stage 'Checks.cs'), $checks, $utf8)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$output = Join-Path $release 'KaguyaPet.exe'
$arguments = @('/nologo', '/target:winexe', '/optimize+', '/platform:anycpu',
    ('/win32icon:"' + (Join-Path $root 'assets\kaguya.ico') + '"'),
    ('/win32manifest:"' + (Join-Path $root 'src\Pet.manifest') + '"'),
    ('/out:"' + $output + '"'),
    '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll')
$resources = @(Get-ChildItem -LiteralPath (Join-Path $root 'assets') -Recurse -File | Sort-Object FullName)
foreach ($file in $resources) {
    $relative = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
    $arguments += '/resource:"' + $file.FullName + '",KaguyaEmbedded/' + $relative
}
$arguments += '/resource:"' + (Join-Path $root 'verification\original-geometry.json') + '",KaguyaEmbedded/verification/original-geometry.json'
foreach ($source in @((Join-Path $stage 'Kaguya.cs'), (Join-Path $root 'src\Native.cs'), (Join-Path $stage 'Appearance.cs'), (Join-Path $stage 'Checks.cs'), (Join-Path $PSScriptRoot 'single-file\EmbeddedAssets.cs'))) { $arguments += '"' + $source + '"' }
$response = Join-Path $stage 'compile.rsp'
[IO.File]::WriteAllLines($response, $arguments, $utf8)
& $compiler ('@' + $response)
if ($LASTEXITCODE -ne 0) { throw "Single-file compilation failed: $LASTEXITCODE" }
[pscustomobject]@{Executable=$output;Bytes=(Get-Item -LiteralPath $output).Length;EmbeddedAssets=$resources.Count;ExternalAssetsRequired=$false} | ConvertTo-Json -Compress
