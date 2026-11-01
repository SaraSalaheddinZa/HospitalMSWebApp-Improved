/* Approved Living Thread public presentation. All account actions use application routes. */
(() => {
  'use strict';
  const root=document.documentElement;
  const reduced=matchMedia('(prefers-reduced-motion: reduce)');
  const reviewOptions=new URLSearchParams(location.search);
  const staticReview=reviewOptions.get('motion')==='reduce';
  const reduceMotion=()=>reduced.matches||staticReview;
  const themeButton=document.querySelector('[data-theme-toggle]');
  const motionButton=document.querySelector('[data-motion-toggle]');
  let paused=false,hearts=[],sceneController=null;
  function setTheme(theme,save){
    const apply=()=>{
      root.dataset.theme=theme==='light'?'light':'dark';
      const target=root.dataset.theme==='dark'?'light':'dark';
      themeButton.setAttribute('aria-label','Switch to '+target+' mode');
      themeButton.querySelector('[data-theme-label]').textContent=target[0].toUpperCase()+target.slice(1);
      if(save){try{localStorage.setItem('guardian-public-theme',root.dataset.theme);}catch(_){}}
    };
    // CSS eases the palette without a snapshot overlay that can intercept
    // an immediate Patient/Staff navigation click after theme selection.
    apply();
  }
  setTheme(root.dataset.theme,false);
  themeButton.addEventListener('click',()=>setTheme(root.dataset.theme==='dark'?'light':'dark',true));
  const systemTheme=matchMedia('(prefers-color-scheme: dark)');
  const syncTheme=()=>{let value;try{value=localStorage.getItem('guardian-public-theme');}catch(_){}setTheme(['light','dark'].includes(value)?value:systemTheme.matches?'dark':'light',false);};
  addEventListener('storage',e=>{if(e.key==='guardian-public-theme')syncTheme();});
  systemTheme.addEventListener('change',syncTheme);
  function motionChanged(){
    const isStatic=paused||reduceMotion();
    root.dataset.static=String(isStatic);
    motionButton.textContent=reduceMotion()?'Motion off':paused?'Resume motion':'Pause motion';
    motionButton.setAttribute('aria-label',reduceMotion()?'Reduced motion: static artwork':paused?'Resume all motion':'Pause all motion');
    motionButton.setAttribute('aria-pressed',String(isStatic));
    motionButton.disabled=reduceMotion();
    hearts.forEach(h=>h.update());
    if(sceneController)sceneController.update();
  }
  motionButton.addEventListener('click',()=>{paused=!paused;motionChanged();});
  reduced.addEventListener('change',motionChanged);
  document.addEventListener('visibilitychange',()=>{
    root.dataset.pageHidden=String(document.hidden);
    hearts.forEach(h=>h.update());
    if(sceneController)sceneController.update();
  });
  motionChanged();
  const menu=document.querySelector('.menu-button'),nav=document.querySelector('#main-nav');
  if(menu&&nav){
    const close=focus=>{nav.classList.remove('is-open');menu.setAttribute('aria-expanded','false');if(focus)menu.focus();};
    menu.addEventListener('click',()=>{const open=menu.getAttribute('aria-expanded')!=='true';menu.setAttribute('aria-expanded',String(open));nav.classList.toggle('is-open',open);if(open)requestAnimationFrame(()=>{if(menu.getAttribute('aria-expanded')==='true')nav.querySelector('a')?.focus({preventScroll:true});});});
    nav.addEventListener('click',e=>{if(e.target.closest('a'))close(false);});
    document.addEventListener('click',e=>{if(!nav.contains(e.target)&&!menu.contains(e.target))close(false);});
    document.addEventListener('keydown',e=>{if(e.key==='Escape'&&nav.classList.contains('is-open'))close(true);});
  }
  const tabs=Array.from(document.querySelectorAll('[role=tab]'));
  function chooseTab(tab,focus){
    tabs.forEach(t=>{const on=t===tab;t.setAttribute('aria-selected',String(on));t.tabIndex=on?0:-1;document.getElementById(t.getAttribute('aria-controls')).hidden=!on;});
    const constellation=document.querySelector('.journey-constellation');
    if(constellation)constellation.dataset.chapter=String(tabs.indexOf(tab));
    const counter=document.querySelector('[data-chapter-counter]');
    if(counter)counter.textContent='Chapter 0'+(tabs.indexOf(tab)+1)+' of 04';
    if(focus)tab.focus();
  }
  tabs.forEach((tab,index)=>{
    tab.addEventListener('click',()=>chooseTab(tab,false));
    tab.addEventListener('keydown',e=>{let next=index;if(e.key==='ArrowRight'||e.key==='ArrowDown')next=(index+1)%tabs.length;else if(e.key==='ArrowLeft'||e.key==='ArrowUp')next=(index+tabs.length-1)%tabs.length;else if(e.key==='Home')next=0;else if(e.key==='End')next=tabs.length-1;else return;e.preventDefault();chooseTab(tabs[next],true);});
  });
  document.querySelector('[data-chapter-next]')?.addEventListener('click',()=>chooseTab(tabs[(tabs.findIndex(t=>t.getAttribute('aria-selected')==='true')+1)%tabs.length],false));
  document.querySelector('[data-chapter-prev]')?.addEventListener('click',()=>chooseTab(tabs[(tabs.findIndex(t=>t.getAttribute('aria-selected')==='true')+tabs.length-1)%tabs.length],false));
  // Native disclosure controls remain usable without JavaScript. The adjacent
  // artwork follows the selected department; it is never clinical data.
  const departmentStage=document.querySelector('.department-stage');
  if(departmentStage){
    const departments=Array.from(document.querySelectorAll('[data-dept]'));
    const captions={medicine:'The conversation starts here.',laboratory:'Details become understanding.',pharmacy:'Your next step, connected.',emergency:'When every moment matters.'};
    departments.forEach(detail=>detail.addEventListener('toggle',()=>{
      if(!detail.open)return;
      departments.forEach(other=>{if(other!==detail)other.open=false;});
      const selected=detail.dataset.dept;
      departmentStage.dataset.department=selected;
      departmentStage.querySelectorAll('[data-dept-photo]').forEach(photo=>{photo.dataset.active=String(photo.dataset.deptPhoto===selected);});
      departmentStage.querySelectorAll('[data-dept-art]').forEach(art=>{art.toggleAttribute('hidden',art.dataset.deptArt!==selected);});
      departmentStage.querySelector('.department-scene-caption').textContent=captions[selected];
    }));
  }
  if('IntersectionObserver'in window){
    const reveal=new IntersectionObserver(entries=>entries.forEach(e=>{if(e.isIntersecting){e.target.classList.add('has-entered');reveal.unobserve(e.target);}}),{threshold:.12});
    document.querySelectorAll('.reveal').forEach(el=>reveal.observe(el));
    const sectionLinks=Array.from(document.querySelectorAll('[data-section-link]'));
    const activeSection=new IntersectionObserver(entries=>{
      entries.forEach(entry=>{
        if(!entry.isIntersecting){
          sectionLinks.filter(link=>link.hash==='#'+entry.target.id).forEach(link=>link.removeAttribute('aria-current'));
          return;
        }
        document.querySelector('.nav-home')?.removeAttribute('aria-current');
        sectionLinks.forEach(link=>{
          if(link.hash==='#'+entry.target.id)link.setAttribute('aria-current','location');
          else link.removeAttribute('aria-current');
        });
      });
    },{rootMargin:'-18% 0px -62% 0px',threshold:0});
    sectionLinks.forEach(link=>{const section=document.querySelector(link.hash);if(section)activeSection.observe(section);});
  }
  // Scroll motion is event-driven: one scheduled frame per scroll/resize, no
  // perpetual secondary render loop and no manipulation of normal scrolling.
  function createSceneController(){
    const path=document.querySelector('.thread-progress');
    const traveller=document.querySelector('.thread-traveller');
    const hero=document.querySelector('.hero');
    const story=document.querySelector('.human-story');
    const length=path?path.getTotalLength():0;
    let frame=0;
    const clamp=(n,min,max)=>Math.min(max,Math.max(min,n));
    function render(){
      frame=0;
      if(document.hidden)return;
      const still=paused||reduceMotion();
      const distance=Math.max(1,document.documentElement.scrollHeight-innerHeight);
      const progress=clamp(scrollY/distance,0,1);
      root.dataset.scrollProgress=progress.toFixed(3);
      document.querySelector('.site-header')?.classList.toggle('is-compact',scrollY>55);
      if(scrollY<55){document.querySelectorAll('[data-section-link]').forEach(a=>a.removeAttribute('aria-current'));document.querySelector('.nav-home')?.setAttribute('aria-current','page');}
      root.style.setProperty('--journey-progress',still?1:progress);
      root.style.setProperty('--hero-shift',still?0:clamp(scrollY/(hero?hero.offsetHeight:1000),0,1)*34);
      if(story){const rect=story.getBoundingClientRect();story.style.setProperty('--scene-drift',still?0:clamp((innerHeight*.5-rect.top)/innerHeight,-1,1)*18);}
      document.querySelectorAll('.portal-story [data-scene]').forEach(section=>{
        const rect=section.getBoundingClientRect();
        section.style.setProperty('--portal-drift',still?'0px':(clamp((innerHeight*.55-rect.top)/innerHeight,-1,1)*16).toFixed(2)+'px');
      });
      if(path&&traveller&&!still){const point=path.getPointAtLength(progress*length);traveller.setAttribute('cx',point.x);traveller.setAttribute('cy',point.y);}
    }
    function update(){if(frame)cancelAnimationFrame(frame);frame=requestAnimationFrame(render);}
    addEventListener('scroll',update,{passive:true});addEventListener('resize',update,{passive:true});
    document.querySelectorAll('details').forEach(el=>el.addEventListener('toggle',update));
    if('ResizeObserver'in window)new ResizeObserver(update).observe(document.body);
    update();return{update};
  }
  document.querySelectorAll('[data-heart] img[data-fallback]').forEach(image=>{
    if(reviewOptions.get('art')==='original')image.src=image.dataset.fallback;
    image.addEventListener('error',()=>{if(!image.src.endsWith(image.dataset.fallback))image.src=image.dataset.fallback;},{once:true});
  });
  // Login has no heart hosts and never requests the renderer or artwork.
  const heartHosts=Array.from(document.querySelectorAll('[data-heart]'));
  if(heartHosts.length){
    import('./heart-motion.js?v=1').then(({initializeHearts})=>{
      hearts=initializeHearts(heartHosts,{reviewOptions,reduceMotion,isPaused:()=>paused});
      motionChanged();
    }).catch(()=>{
      // The untouched source image remains visible when the module is unavailable.
      heartHosts.forEach(host=>{host.dataset.renderer='static-image';host.dataset.rendererReason='Renderer module unavailable';});
      console.warn('Heart motion unavailable; static artwork retained.');
    });
  }
  sceneController = createSceneController();
  motionChanged();
})();
