/* Approved Living Sculpture presentation. Real authentication stays in login.aspx.cs. */
(() => {
 'use strict';
 const root=document.documentElement, q=s=>document.querySelector(s);
 const form=q('#form1'),email=q('#emailTb'),password=q('#passwordTb'),role=q('#roleCb'),feedback=q('#ErrMsg');
 const reduced=matchMedia('(prefers-reduced-motion: reduce)'),forcedReduce=new URLSearchParams(location.search).get('motion')==='reduce';
 const traces=[...document.querySelectorAll('.thread-focus')],active=new Set();
 let paused=false,focusRun=0,modeReady=false,lastStaffRole=role.value==='Patient'?'':role.value;
 const copy={
  patient:{eyebrow:'A connected place · for every care journey',title1:'Your care.',title2:'A more open',title3:'tomorrow.',intro:'Appointments, records and your next step. A familiar place to continue your care journey.',formKicker:'YOUR PERSONAL ENTRANCE',formTitle:'Welcome back.',formIntro:'Continue your care journey.',helpLabel:'New to Guardian Health?',helpAction:'Registration guidance'},
  staff:{eyebrow:'A shared place · for the people who care',title1:'Your purpose.',title2:'A more connected',title3:'every day.',intro:'Different roles. One shared commitment. Your entrance to the people and work of care.',formKicker:'YOUR PROFESSIONAL ENTRANCE',formTitle:'Welcome, team.',formIntro:'Your place in connected care.',helpLabel:'Need access to your workspace?',helpAction:'Account assistance'}
 };
 function canMove(){return root.dataset.motion==='running'&&!document.hidden&&!reduced.matches;}
 function animate(el,frames,options){
  if(!canMove()||!el.animate)return null;
  const a=el.animate(frames,options);active.add(a);
  a.onfinish=()=>active.delete(a);a.oncancel=()=>active.delete(a);return a;
 }
 function cancelEffects(){active.forEach(a=>a.cancel());active.clear();root.dataset.trace='quiet';}
 function clearFeedback(){
  feedback.hidden=true;feedback.textContent='';
  [email,password,role].forEach(el=>el.removeAttribute('aria-invalid'));
  document.querySelectorAll('.field-error').forEach(el=>{el.hidden=true;el.textContent='';});
 }
 function concealPassword(){password.type='password';q('#password-toggle').setAttribute('aria-pressed','false');q('#password-toggle').setAttribute('aria-label','Show password');}
 function updateUrl(key,value){const u=new URL(location.href);u.searchParams.set(key,value);if(key==='mode'){u.searchParams.delete('role');u.searchParams.delete('access');u.searchParams.set(value==='staff'?'access':'role',value==='staff'?'staff':'Patient');}history.replaceState(null,'',u.pathname+u.search+u.hash);}
 function setMode(mode,save){
  cancelEffects();root.dataset.mode=mode==='staff'?'staff':'patient';const staff=root.dataset.mode==='staff';
  document.querySelectorAll('[data-mode-choice]').forEach(el=>el.setAttribute('aria-pressed',String(el.dataset.modeChoice===root.dataset.mode)));
  document.querySelectorAll('[data-copy]').forEach(el=>el.textContent=copy[root.dataset.mode][el.dataset.copy]);
  q('.role-field').hidden=!staff;role.disabled=false;role.required=true;
  if(staff&&role.value==='Patient')role.value=lastStaffRole;
  if(!staff){if(role.value&&role.value!=='Patient')lastStaffRole=role.value;role.value='Patient';}
  role.querySelector('option[value="Patient"]').hidden=staff;
  q('#entry-mode').value=root.dataset.mode;
  // Values are kept only in this document; switching modes never reveals a password.
  concealPassword();if(save)clearFeedback();
  if(save)updateUrl('mode',root.dataset.mode);
  if(modeReady){animate(q('.editorial'),[{opacity:.6},{opacity:1}],{duration:420,easing:'ease-out'});animate(q('.form-heading'),[{opacity:.65},{opacity:1}],{duration:350,easing:'ease-out'});}
  modeReady=true;resizeMask();
 }
 function setTheme(theme,save){
  root.dataset.theme=theme==='dark'?'dark':'light';
  const dark=root.dataset.theme==='dark';
  q('[data-theme-label]').textContent=dark?'Dark':'Light';
  q('#theme-toggle').setAttribute('aria-label','Switch to '+(dark?'light':'dark')+' mode');
  if(save){try{localStorage.setItem('guardian-public-theme',root.dataset.theme);}catch(_){}}
 }
 function setMotion(){
  const off=paused||forcedReduce||reduced.matches;
  root.dataset.motion=off?'static':'running';
  q('#motion-toggle').disabled=forcedReduce||reduced.matches;
  q('#motion-toggle').setAttribute('aria-pressed',String(off));
  const label=q('#motion-toggle').disabled?'Reduced motion':off?'Resume motion':'Pause motion';
  q('#motion-toggle').setAttribute('aria-label',label);q('#motion-toggle span').textContent=label;
  if(off)cancelEffects();
 }
 const systemTheme=matchMedia('(prefers-color-scheme: dark)');
 const syncTheme=()=>{let value;try{value=localStorage.getItem('guardian-public-theme');}catch(_){}setTheme(['light','dark'].includes(value)?value:systemTheme.matches?'dark':'light',false);};
 window.addEventListener('storage',e=>{if(e.key==='guardian-public-theme')syncTheme();});
 systemTheme.addEventListener('change',syncTheme);
 document.querySelectorAll('[data-mode-choice]').forEach((button,index,buttons)=>{
  button.addEventListener('click',()=>setMode(button.dataset.modeChoice,true));
  button.addEventListener('keydown',event=>{
   if(!['ArrowLeft','ArrowRight','Home','End'].includes(event.key))return;
   event.preventDefault();
   const i=event.key==='Home'?0:event.key==='End'?1:(index+1)%2;
   buttons[i].focus();setMode(buttons[i].dataset.modeChoice,true);
  });
 });
 q('#theme-toggle').addEventListener('click',()=>setTheme(root.dataset.theme==='dark'?'light':'dark',true));
 q('#motion-toggle').addEventListener('click',()=>{paused=!paused;setMotion();});
 reduced.addEventListener('change',setMotion);
 document.addEventListener('visibilitychange',()=>{root.dataset.pageHidden=String(document.hidden);if(document.hidden)cancelEffects();});
 document.addEventListener('visibilitychange',()=>{if(document.hidden)concealPassword();});
 q('#password-toggle').addEventListener('click',()=>{
  const show=password.type==='password';password.type=show?'text':'password';
  q('#password-toggle').setAttribute('aria-pressed',String(show));q('#password-toggle').setAttribute('aria-label',show?'Hide password':'Show password');
 });
 function focusTrace(event){
  if(![email,password,role].includes(event.target))return;
  cancelEffects();root.dataset.fieldFocus='true';if(!canMove())return;
  // Bring the one-shot trace to the outer glass edge beside the focused field.
  // Its curve stays outside the content; only this event performs geometry reads.
  const scene=q('.light-trail-back').getBoundingClientRect(),glass=q('.glass-panel').getBoundingClientRect(),field=event.target.getBoundingClientRect();
  const edgeX=(glass.left-scene.left-5)/scene.width*800;
  const edgeY=(field.top+field.height/2-scene.top)/scene.height*1000;
  const crownY=Math.max(30,(glass.top-scene.top)/scene.height*1000-47);
  q('.focus-back').setAttribute('d','M'+(edgeX+140)+' '+crownY+'C'+(edgeX-100)+' '+(crownY-60)+' '+(edgeX-135)+' '+(edgeY-35)+' '+edgeX+' '+edgeY);
  const target=event.target===email?.55:event.target===password?.39:.24;
  root.dataset.trace='playing';root.dataset.focusRun=String(++focusRun);
  traces.forEach((el,i)=>animate(el,[
   {strokeDashoffset:1,opacity:0,offset:0},
   {opacity:.95,offset:.18},
   {strokeDashoffset:i?target:.02,opacity:.85,offset:.73},
   {strokeDashoffset:i?target-.04:-.04,opacity:0,offset:1}
  ],{duration:1750,easing:'cubic-bezier(.22,.6,.35,1)'}));
  const a=animate(q('.aura-focus'),[{opacity:0},{opacity:.32,offset:.43},{opacity:0}],{duration:1750,easing:'ease-in-out'});
  if(a)a.finished.then(()=>{root.dataset.trace='settled';}).catch(()=>{});
 }
 form.addEventListener('focusin',focusTrace);
 form.addEventListener('focusout',e=>{if(!form.contains(e.relatedTarget))root.dataset.fieldFocus='false';});
 [email,password,role].forEach(input=>input.addEventListener('input',()=>{
  input.removeAttribute('aria-invalid');
  const err=q(input===email?'#email-error':input===password?'#password-error':'#role-error');err.hidden=true;err.textContent='';
 }));
 function markError(input,id,message){input.setAttribute('aria-invalid','true');q(id).textContent=message;q(id).hidden=false;}
 form.addEventListener('submit',event=>{
  cancelEffects();clearFeedback();const errors=[];
  if(!email.value.trim()||!email.validity.valid){markError(email,'#email-error','Enter a valid email address.');errors.push('email address');}
  if(!password.value){markError(password,'#password-error','Enter your password.');errors.push('password');}
  if(!role.value){markError(role,'#role-error','Choose your staff role.');errors.push('staff role');}
  if(errors.length){
   event.preventDefault();feedback.textContent='Please check your '+errors.join(', ')+'.';feedback.hidden=false;
   feedback.focus({preventScroll:true});feedback.scrollIntoView({block:'center',behavior:'instant'});
  }
  // Valid forms submit normally to the existing server handler. Never simulate success.
 });
 // Foreground silk may cross the outer glass rim, never its text or interactive content.
 // Geometry is calculated only on actual layout changes, not in an animation loop.
 function resizeMask(){
  const svg=q('.silk-front'),panel=q('.glass-panel');
  if(!svg||!panel||!q('#front-fade-start')||!q('#front-fade-end'))return;
  const s=svg.getBoundingClientRect(),p=panel.getBoundingClientRect();
  if(!s.width||!s.height)return;
  // Soft depth handoff below the content, not a hard rectangular cut through the silk.
  const start=Math.max(0,Math.min(.96,(p.bottom-s.top-18)/s.height));
  const end=Math.min(1,start+45/s.height);
  q('#front-fade-start').setAttribute('offset',String(start));
  q('#front-fade-end').setAttribute('offset',String(end));
  q('.silk-back').style.setProperty('--back-end',String(start*100)+'%');
  q('.silk-back').style.setProperty('--back-fade',String(end*100)+'%');
 }
 if('ResizeObserver' in window)new ResizeObserver(resizeMask).observe(q('.glass-panel'));
 window.addEventListener('resize',resizeMask,{passive:true});
 setMode(root.dataset.mode,false);setTheme(root.dataset.theme,false);setMotion();
 root.dataset.pageHidden=String(document.hidden);root.dataset.trace='quiet';
 if(feedback.textContent.trim()){feedback.hidden=false;requestAnimationFrame(()=>{feedback.focus({preventScroll:true});feedback.scrollIntoView({block:'center',behavior:'instant'});});}
 animate(q('.silk-back'),[{opacity:0},{opacity:1}],{duration:950,easing:'ease-out'});
 animate(q('.silk-front'),[{opacity:0},{opacity:1}],{duration:1100,easing:'ease-out'});
 animate(q('.glass-panel'),[{opacity:.55},{opacity:1}],{duration:600,easing:'ease-out'});
 animate(q('.editorial'),[{opacity:.4},{opacity:1}],{duration:700,easing:'ease-out'});
})();

