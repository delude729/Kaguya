// Build-time only: preserve the original HTML/CSS panel as native bitmap assets.
const {app,BrowserWindow}=require('electron');
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..');
const original=path.join(root,'reference-ui');
const uiScale=Number(process.argv.find(a=>a.startsWith('--ui-scale='))?.split('=')[1]||1);
const badgeOnly=process.argv.includes('--badge-only');
if(!Number.isFinite(uiScale)||uiScale<1||uiScale>4)throw new Error('Invalid UI scale');
const output=uiScale===1?path.join(root,'assets/ui'):path.join(root,`assets/ui/dpi-${Math.round(uiScale*100)}`);
fs.mkdirSync(output,{recursive:true});
// OneDrive can briefly lock an existing asset while synchronizing it.
async function writePng(filename,img){
 const target=path.join(output,filename),data=img.toPNG({scaleFactor:uiScale});
 for(let attempt=0;;attempt++){
  try{fs.writeFileSync(target,data);return;}catch(e){if(attempt===9)throw e;await new Promise(r=>setTimeout(r,200));}
 }
}
app.commandLine.appendSwitch('force-device-scale-factor',String(uiScale));
app.disableHardwareAcceleration();
app.whenReady().then(async()=>{
 // Minimum bounds prevent high-DPI exports being clamped to the desktop work area.
 const w=new BrowserWindow({width:380,height:726,minWidth:380,minHeight:726,show:false,transparent:true,frame:false,webPreferences:{backgroundThrottling:false}});
 try{
 await w.loadFile(path.join(original,'index.html'));
 w.showInactive();
 await new Promise(r=>setTimeout(r,400));
 await w.webContents.insertCSS('*{transition:none!important;animation:none!important} #mood-label,#energy-label,progress,#size-slider,#size-value{visibility:hidden!important}');
 await w.webContents.executeJavaScript(`
 document.querySelectorAll('main > :not(#panel)').forEach(e=>e.style.visibility='hidden');
 document.querySelector('#panel').classList.remove('closed');
 document.querySelector('#panel').inert=false;
 `);
 await w.webContents.executeJavaScript('new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)))');
 await new Promise(r=>setTimeout(r,150));
 const layout=await w.webContents.executeJavaScript(`(()=>{const out={};for(const id of ['panel','close','feed','pat','sleep','topmost','hide','badge','character','mood','energy','mood-label','energy-label','size-slider','size-value']){const r=document.getElementById(id).getBoundingClientRect();out[id]={x:r.x,y:r.y,width:r.width,height:r.height};}out.scales=[...document.querySelectorAll('[data-scale]')].map(e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height};});return out;})()`);
 if(uiScale===1&&!badgeOnly)fs.writeFileSync(path.join(root,'assets/ui/layout.json'),JSON.stringify(layout,null,2));
 const dimensions=[];
 if(!badgeOnly)for(let sleep=0;sleep<2;sleep++)for(let top=0;top<2;top++)for(let scale=0;scale<3;scale++){
 await w.webContents.executeJavaScript(`document.querySelector('#sleep span').textContent=${JSON.stringify(sleep?'叫醒辉夜':'休息一会')};document.querySelector('#topmost span').textContent=${JSON.stringify(top?'开':'关')};document.querySelector('#topmost span').classList.toggle('on',${!!top});document.querySelectorAll('[data-scale]').forEach((e,i)=>e.classList.toggle('selected',i===${scale}));`);
 await w.webContents.executeJavaScript('new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)))');
 await new Promise(r=>setTimeout(r,150));
 const img=await w.webContents.capturePage({x:0,y:0,width:380,height:Math.ceil(layout.panel.y+layout.panel.height+24)});
 const filename=`panel-${sleep}-${top}-${scale}.png`;
 await writePng(filename,img);
 dimensions.push({filename,size:img.getSize(uiScale),scaleFactors:img.getScaleFactors()});
 }
 await w.webContents.executeJavaScript(`document.querySelector('#panel').style.visibility='hidden';document.querySelector('#badge').style.visibility='visible'`);
 // Export the background separately: small badge text is hinted natively at the
 // final device resolution, using these original CSS bounds and type sizes.
 const badgeAlignment=await w.webContents.executeJavaScript(`(()=>{
 const badge=document.querySelector('#badge'),dpr=devicePixelRatio;
 for(const node of [...badge.childNodes])if(node.nodeType===Node.TEXT_NODE&&node.textContent.trim()){
  const span=document.createElement('span');span.id='badge-name';span.textContent=node.textContent.trim();node.replaceWith(span);
 }
 const before={};for(const id of ['badge-name','badge-state']){const r=document.getElementById(id).getBoundingClientRect();before[id]={x:r.x,y:r.y,width:r.width,height:r.height};}
 for(const id of ['badge-name','badge-state'])document.getElementById(id).style.visibility='hidden';
 return {dpr,before};})()`);
 const badgeLabels={};
 for(let sleep=0;sleep<2;sleep++)for(let open=0;open<2;open++){
 await w.webContents.executeJavaScript(`document.querySelector('#badge-state').textContent=${JSON.stringify(sleep?'休息中':'在你身边')};document.querySelector('#badge b').textContent=${JSON.stringify(open?'⌃':'⌄')}`);
 await w.webContents.executeJavaScript('new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)))');
 await new Promise(r=>setTimeout(r,150));
 const img=await w.webContents.capturePage({x:95,y:Math.floor(layout.badge.y)-12,width:190,height:Math.ceil(layout.badge.height)+24});
 const filename=`badge-${sleep}-${open}.png`;
 badgeLabels[filename.slice(0,-4)]=await w.webContents.executeJavaScript(`(()=>{
 const out={};for(const id of ['badge-name','badge-state']){const e=document.getElementById(id),r=e.getBoundingClientRect(),s=getComputedStyle(e);out[id]={x:r.x-95,y:r.y-${Math.floor(layout.badge.y)-12},width:r.width,height:r.height,fontSize:parseFloat(s.fontSize),text:e.textContent,color:s.color};}return out;})()`);
 await writePng(filename,img);
 dimensions.push({filename,size:img.getSize(uiScale),scaleFactors:img.getScaleFactors()});
 }
 fs.writeFileSync(path.join(output,'badge-labels.json'),JSON.stringify(badgeLabels,null,2));
 fs.writeFileSync(path.join(root,`verification/ui-export-${Math.round(uiScale*100)}${badgeOnly?'-badge':''}.json`),JSON.stringify({success:true,uiScale,layout,badgeAlignment,dimensions},null,2));
 const stale=path.join(root,'verification/ui-export-error.txt');if(fs.existsSync(stale))fs.unlinkSync(stale);
 }catch(e){fs.writeFileSync(path.join(root,'verification/ui-export-error.txt'),e.stack+'\n'+JSON.stringify({bounds:w.getBounds(),content:w.getContentBounds()}));process.exitCode=1;}
 w.destroy();app.quit();
});
