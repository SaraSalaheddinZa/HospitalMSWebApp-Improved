// Isolated client-preference tests. No browser automation, database or credentials.
const fs = require('fs');
const vm = require('vm');
const assert = require('assert');
const source = fs.readFileSync(require('path').join(__dirname, '../Scripts/guardian-theme.js'), 'utf8');
let passed = 0;
function check(value, label) { assert.ok(value, label); passed++; console.log('PASS: ' + label); }
function fixture(options = {}) {
    const attrs = {}, listeners = {}, windowListeners = {}, buttonListeners = {}, buttonAttrs = {}, label = {};
    const store = { value: options.saved || null, writes: 0 };
    const button = { hidden: true, setAttribute:(k,v)=>buttonAttrs[k]=v, querySelector:()=>label, addEventListener:(k,v)=>buttonListeners[k]=v };
    const media = { matches:!!options.dark, addEventListener:(k,v)=>listeners.media=v };
    const root = { classList:{contains:()=>!options.internal}, setAttribute:(k,v)=>attrs[k]=v, getAttribute:k=>attrs[k] };
    const document = { documentElement:root, querySelectorAll:()=>[button], addEventListener:(k,v)=>listeners[k]=v };
    const storage = { getItem:()=>{if(options.blocked)throw Error('denied');return store.value;},setItem:(k,v)=>{if(options.blocked)throw Error('denied');store.value=v;store.writes++;} };
    vm.runInNewContext(source,{document,window:{matchMedia:()=>media,addEventListener:(k,v)=>windowListeners[k]=v},localStorage:storage});
    if (listeners.DOMContentLoaded) listeners.DOMContentLoaded();
    return {attrs,button,buttonAttrs,label,media,store,toggle:()=>buttonListeners.click(),system:dark=>{media.matches=dark;listeners.media();},sync:value=>windowListeners.storage({key:'guardian-public-theme',newValue:value})};
}
let light = fixture(); check(light.attrs['data-theme']==='light','First visit follows light OS preference');
let dark = fixture({dark:true}); check(dark.attrs['data-theme']==='dark','First visit follows dark OS preference');
check(dark.store.writes===0,'OS default does not become an explicit persisted choice');
dark.system(false); check(dark.attrs['data-theme']==='light','OS changes followed before explicit selection');
dark.toggle(); check(dark.store.value==='dark'&&dark.attrs['data-theme']==='dark','Explicit choice applies and persists');
dark.system(false); check(dark.attrs['data-theme']==='dark','Explicit choice takes precedence over OS');
check(dark.buttonAttrs['aria-label']==='Switch to light mode'&&dark.label.textContent==='Light'&&!dark.button.hidden,'Toggle announces the next action');
let reload = fixture({saved:dark.store.value}); check(reload.attrs['data-theme']==='dark','Saved choice restored before stylesheet loading');
reload.sync('light'); check(reload.attrs['data-theme']==='light','Cross-tab preference changes are respected');
reload.sync(null); reload.system(true); check(reload.attrs['data-theme']==='dark','Removed preference returns to OS behavior');
check(fixture({saved:'unexpected',dark:true}).attrs['data-theme']==='dark','Malformed stored preference ignored');
let blocked = fixture({blocked:true,dark:true}); blocked.toggle(); check(blocked.attrs['data-theme']==='light','Blocked storage does not prevent theme switching');
check(fixture({internal:true}).attrs['data-theme']===undefined,'Theme initializer does not touch authenticated pages');
const motionSource = fs.readFileSync(require('path').join(__dirname, '../Scripts/guardian-cinematic.js'), 'utf8');
const motionClasses = new Set(), motionEvents = {}, docEvents = {}, motionAttrs = {};
const reduced = {matches:true, addEventListener:(k,v)=>motionEvents.change=v};
const motionButton = {hidden:false,setAttribute:(k,v)=>motionAttrs[k]=v,addEventListener:(k,v)=>motionEvents[k]=v};
const motionDocument = {hidden:false,documentElement:{classList:{contains:()=>true,toggle:(k,on)=>on?motionClasses.add(k):motionClasses.delete(k)}},querySelector:s=>s==='[data-motion-toggle]'?motionButton:null,getElementById:()=>null,addEventListener:(k,v)=>docEvents[k]=v};
vm.runInNewContext(motionSource,{document:motionDocument,window:{matchMedia:q=>q.includes('reduced-motion')?reduced:{matches:false},addEventListener:()=>{}}});
check(motionClasses.has('motion-paused')&&motionButton.hidden,'Reduced-motion preference disables ambience and hides redundant pause control');
reduced.matches=false;motionEvents.change();check(!motionClasses.has('motion-paused')&&!motionButton.hidden,'Preference changes restore optional motion controls');
motionEvents.click();check(motionClasses.has('motion-paused')&&motionAttrs['aria-pressed']==='true','Explicit motion pause exposes accessible state');
motionEvents.click();motionDocument.hidden=true;docEvents.visibilitychange();check(motionClasses.has('motion-paused'),'Background document pauses ambience');
motionDocument.hidden=false;docEvents.visibilitychange();check(!motionClasses.has('motion-paused'),'Visible document resumes permitted ambience');
const css=fs.readFileSync(require('path').join(__dirname,'../Content/guardian-cinematic.css'),'utf8');
check(css.includes('@media(prefers-reduced-motion:reduce)')&&css.includes('animation:none!important;transition:none!important'),'Stylesheet removes nonessential animation and transitions under reduced motion');
const path=require('path');
const home=fs.readFileSync(path.join(__dirname,'../Views/Guest/Home.aspx'),'utf8');
const login=fs.readFileSync(path.join(__dirname,'../Views/login.aspx'),'utf8');
const crypto=require('crypto');
const heart=fs.readFileSync(path.join(__dirname,'../Assets/Images/guardian-heart.png'));
check(crypto.createHash('sha256').update(heart).digest('hex')==='4aa676dd067c51d3b71dfad62d1c75caceaa9e739f0d42ba67942735c0c45d81','Original approved anatomical heart bytes are preserved');

const base=path.join(__dirname,'../Assets/Public/LivingThread');
const preference=fs.readFileSync(path.join(base,'public-preference.js'),'utf8');
const homeMotion=fs.readFileSync(path.join(base,'study.js'),'utf8');
const loginMotion=fs.readFileSync(path.join(base,'living-sculpture.js'),'utf8');
const mesh=fs.readFileSync(path.join(base,'heart-motion.js'),'utf8');
const loginCss=['living-sculpture.css','living-sculpture-render.css','living-sculpture-polish.css'].map(f=>fs.readFileSync(path.join(base,f),'utf8')).join('\n');
check(home.includes('data-motion-toggle')&&login.includes('id="motion-toggle"'),'Approved public pages both expose pause controls');
check(login.includes('data-sign-in')&&login.includes('OnClick="loginBtn_Click"')&&login.includes('ID="roleCb"')&&login.includes('ID="rememberMeCb"'),'Server authentication handler and role/remember controls retained');
check(!login.includes('<canvas')&&!login.includes('heart-motion')&&!login.includes('guardian-heart'),'Approved Login has no heart/renderer dependency');
check(home.includes('mobile-care-bar')&&home.includes('hero-access'),'Approved Homepage retains mobile and desktop care actions');
check(homeMotion.includes('guardian-public-theme')&&loginMotion.includes('guardian-public-theme')&&preference.includes('guardian-public-theme'),'One public theme preference across Home and Login');
check(!homeMotion.includes('guardian-study-theme')&&!loginMotion.includes('guardian-sculpture-theme'),'Prototype-specific preference keys retired in application copies');
check(!home.includes('localhost:')&&!home.includes('login.html')&&!login.includes('form-action'),'Prototype routes and submit-blocking CSP absent');
check(loginMotion.includes('if(errors.length)')&&!loginMotion.includes('No sign-in was attempted')&&!login.includes('Preview'),'Real login is not replaced by prototype success or preview state');
check(loginMotion.includes('focusRun')&&loginMotion.includes('duration:1750')&&loginMotion.includes("form.addEventListener('focusin'"),'Approved one-shot focus light trace retained');
check(loginCss.includes('prefers-reduced-motion')&&homeMotion.includes('reduceMotion')&&loginMotion.includes('reduced.matches'),'Both motion systems retain reduced-motion support');
check(mesh.includes('initializeHearts')&&homeMotion.includes("import('./heart-motion.js"),'Approved regional heartbeat loaded as a separate module');
for(const options of [{saved:'light',dark:true},{saved:'dark',dark:false},{saved:null,dark:true},{saved:'bad',dark:false},{blocked:true,dark:true}]){
 const root={dataset:{}};
 vm.runInNewContext(preference,{document:{documentElement:root},localStorage:{getItem:()=>{if(options.blocked)throw Error('denied');return options.saved;}},matchMedia:q=>({matches:q.includes('color-scheme')&&options.dark}),URLSearchParams,location:{search:''}});
 check(root.dataset.theme===(['light','dark'].includes(options.saved)?options.saved:options.dark?'dark':'light'),'Integrated pre-paint preference handles saved/OS/blocked-storage state');
}
console.log('PublicTheme: '+passed+' assertions passed.');
