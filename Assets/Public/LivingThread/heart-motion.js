/* Homepage/comparison-only optical art renderer. Never requested by Login. */
export function initializeHearts(hosts, {reviewOptions, reduceMotion, isPaused}) {
  // All light follows the existing heart clock. These accents do not add a
  // timer or a second animation loop, and return to rest with the renderer.
  function createPulseLight(host){
    const scene=host.parentElement,glow=scene.querySelector('.heart-light');
    const rim=scene.querySelector('.heart-rim'),trails=Array.from(scene.querySelectorAll('.energy-trail'));
    const lightContext=host.closest('[data-heart-context]')||document;
    const sharedTrails=Array.from(lightContext.querySelectorAll('[data-shared-pulse]'));
    const treatment=host.dataset.treatment||'current';
    const glowLevel=treatment==='a'?[.13,.14]:treatment==='b'?[.25,.28]:treatment==='c'?[.22,.30]:treatment==='hybrid'?[.30,.46]:[.42,.44];
    const connectionLights=Array.from(lightContext.querySelectorAll('[data-shared-light]'));
    const nodes=Array.from(scene.querySelectorAll('[data-pulse-node]'));
    const envelope=(t,a,b)=>t<=a||t>=b?0:Math.sin(Math.PI*(t-a)/(b-a))**2;
    return (value,time)=>{
      const warmth=Math.max(value,.55*envelope(time,.18,.64)+.22*envelope(time,.61,1.02));
      if(glow){glow.style.opacity=String(glowLevel[0]+glowLevel[1]*warmth);glow.style.transform='scale('+(1+.045*warmth)+')';}
      if(rim)rim.style.opacity=String(.24+.38*warmth);
      connectionLights.forEach(light=>light.style.setProperty('--light-energy',String(.32+.35*warmth)));
      nodes.forEach((node,index)=>node.style.opacity=String(.24+Math.max(0,value-index*.12)*.7));
      trails.forEach((trail,index)=>{
        const progress=Math.max(0,Math.min(1,(time-(index?.55:.18))/1.1));
        const intensity=Math.sin(Math.PI*progress)*(index?.3:.64);
        trail.style.strokeDashoffset=String(-progress*.9);
        trail.style.opacity=String(Math.max(0,intensity));
      });
      sharedTrails.forEach((trail,index)=>{
        const progress=Math.max(0,Math.min(1,(time-(index?.58:.22))/1.16));
        trail.style.strokeDashoffset=String(-progress*.94);
        trail.style.opacity=String(Math.max(0,Math.sin(Math.PI*progress)*(index?.42:.78)));
      });
    };
  }
  /* A textured 2D mesh, NOT a volumetric/rigged heart. The top vessel region
     stays anchored; lower regions contract with different weights. */
  function createHeart(host) {
    const canvas = host.querySelector('canvas'), image = host.querySelector('img');
    const strength=host.dataset.treatment==='a'?.65:host.dataset.treatment==='b'?.78:host.dataset.treatment==='hybrid'?.82:1;
    const illuminate = createPulseLight(host);
    let gl = null;
    try { if (reviewOptions.get('renderer') !== 'canvas') gl = canvas.getContext('webgl', {alpha:true, antialias:false, premultipliedAlpha:false, powerPreference:'low-power'}); } catch (_) {}
    if (!gl) { host.dataset.rendererReason = reviewOptions.get('renderer') === 'canvas' ? 'Explicit fallback review' : 'WebGL unavailable'; return canvasHeart(host); }
    const vertex = `precision mediump float; attribute vec2 a_position;
      uniform float u_beat; uniform float u_secondary; uniform vec2 u_fit;
      varying vec2 v_uv;
      void main(){
        vec2 p=a_position; v_uv=p*.5+.5;
        // UV y points upward. The upper vessels stay anchored. Regional
        // weighting adds ventricular squeeze and a very small apex torsion.
        // This is optical deformation of a textured surface, not anatomy simulation.
        float lower=1.-smoothstep(.43,.70,v_uv.y);
        float region=.74+.26*smoothstep(.2,.8,v_uv.x);
        float squeeze=u_beat*lower*region;
        p.x-=(p.x-.04)*.105*squeeze;
        p.y-=(p.y+.12)*.052*squeeze;
        float apex=1.-smoothstep(.10,.40,v_uv.y);
        p.x+=.012*u_beat*apex;
        float upper=exp(-pow((v_uv.y-.58)*8.,2.))*exp(-pow((v_uv.x-.32)*5.,2.));
        p.x+=.007*u_secondary*upper;
        gl_Position=vec4(p*u_fit,0.,1.);
      }`;
    const fragment = `precision mediump float;
      uniform sampler2D u_image; uniform float u_beat;
      varying vec2 v_uv;
      void main(){
        vec4 c=texture2D(u_image,v_uv);
        float local=exp(-dot((v_uv-vec2(.40,.48))*vec2(2.8,2.),(v_uv-vec2(.40,.48))*vec2(2.8,2.)));
        // A restrained local highlight, not full-image flashing.
        c.rgb+=vec3(.075,.037,.045)*u_beat*local;
        gl_FragColor=c;
      }`;
    function shader(type, text) { const s=gl.createShader(type); gl.shaderSource(s,text);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw new Error(gl.getShaderInfoLog(s)||'Heart shader could not compile');return s; }
    let program;
    try { program=gl.createProgram();gl.attachShader(program,shader(gl.VERTEX_SHADER,vertex));gl.attachShader(program,shader(gl.FRAGMENT_SHADER,fragment));gl.linkProgram(program);if(!gl.getProgramParameter(program,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(program)||'Heart shader link failed'); } catch (error) { host.dataset.rendererReason = error.message; return canvasHeart(host); }
    gl.useProgram(program);
    const positions=[];const n=56;
    for(let y=0;y<n;y++)for(let x=0;x<n;x++){const a=x/n*2-1,b=y/n*2-1,d=2/n;positions.push(a,b,a+d,b,a,b+d,a,b+d,a+d,b,a+d,b+d);}
    const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(positions),gl.STATIC_DRAW);
    const position=gl.getAttribLocation(program,'a_position');gl.enableVertexAttribArray(position);gl.vertexAttribPointer(position,2,gl.FLOAT,false,0,0);
    const beat=gl.getUniformLocation(program,'u_beat'), secondary=gl.getUniformLocation(program,'u_secondary'), fit=gl.getUniformLocation(program,'u_fit');
    let ready=false,frame=0,visible=true,failed=false,elapsed=0,last=0;
    const texture=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);
    const canMove=()=>ready&&!failed&&!isPaused()&&!reduceMotion()&&!document.hidden&&visible;
    function size() {const rect={width:host.clientWidth,height:host.clientHeight};const dpr=Math.min(devicePixelRatio||1,1.5);canvas.width=Math.max(1,Math.round(rect.width*dpr));canvas.height=Math.max(1,Math.round(rect.height*dpr));gl.viewport(0,0,canvas.width,canvas.height);const box=rect.width/rect.height,img=image.naturalWidth/image.naturalHeight;gl.uniform2f(fit,Math.min(1,img/box),Math.min(1,box/img));}
    function bump(t,start,end){if(t<=start||t>=end)return 0;const s=Math.sin(Math.PI*(t-start)/(end-start));return s*s;}
    function draw(time) {
      const primary=bump(time,.10,.43), second=bump(time,.53,.80), value=primary+.44*second;
      gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT);gl.uniform1f(beat,value*strength);gl.uniform1f(secondary,second*strength);gl.drawArrays(gl.TRIANGLES,0,positions.length/2);
      illuminate(value,time);
      // Deliberately inspectable, non-medical motion state for prototype QA.
      host.dataset.beat=value.toFixed(3);host.dataset.phase=primary>0?'primary':second>0?'secondary':'rest';
    }
    let frameTotal=0,frameCount=0,paintTotal=0;
    function tick(now){frame=0;if(!canMove())return; if(last){const delta=now-last;elapsed+=Math.min(delta,60);frameTotal+=delta;frameCount++;}last=now;const start=performance.now();draw((elapsed%1800)/1000);paintTotal+=performance.now()-start;if(frameCount>=120){host.dataset.frameMs=(frameTotal/frameCount).toFixed(2);host.dataset.submitMs=(paintTotal/frameCount).toFixed(3);frameTotal=0;frameCount=0;paintTotal=0;}frame=requestAnimationFrame(tick);}
    function update(){if(frame)cancelAnimationFrame(frame);frame=0;last=0;host.dataset.motionGate=JSON.stringify({ready,failed,paused:isPaused(),reduced:reduceMotion(),hidden:document.hidden,visible});if(!ready||failed)return;if(canMove()){frame=requestAnimationFrame(tick);host.dataset.motion='playing';}else{draw(0);host.dataset.motion='static';}}
    function loaded(){if(!image.naturalWidth||failed)return;try{gl.bindTexture(gl.TEXTURE_2D,texture);gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,true);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,image);ready=true;size();draw(0);host.classList.add('is-rendering');host.dataset.renderer='webgl-textured-2d-mesh';update();}catch(_){failed=true;host.dataset.renderer='static-image';host.dataset.rendererReason='Texture unavailable';}}
    if(image.complete)loaded();else image.addEventListener('load',loaded,{once:true});
    new ResizeObserver(()=>{if(ready&&!failed){size();draw(canMove()?(elapsed%1800)/1000:0);}}).observe(host);
    if('IntersectionObserver'in window)new IntersectionObserver(entries=>{visible=entries[0].isIntersecting;update();},{threshold:.01}).observe(host);
    canvas.addEventListener('webglcontextlost',e=>{e.preventDefault();failed=true;if(frame)cancelAnimationFrame(frame);frame=0;host.classList.remove('is-rendering');host.dataset.renderer='static-image';host.dataset.motion='static';host.dataset.beat='0.000';host.dataset.phase='rest';illuminate(0,0);});
    return {update};
  }
  // Dependency-free Canvas fallback for browsers without WebGL. Horizontal
  // strips undergo region-weighted contraction; this is still 2D optical art.
  function canvasHeart(host) {
    const strength=host.dataset.treatment==='a'?.65:host.dataset.treatment==='b'?.78:host.dataset.treatment==='hybrid'?.82:1;
    const original=host.querySelector('canvas'), canvas=document.createElement('canvas');
    canvas.setAttribute('aria-hidden','true');original.replaceWith(canvas);
    const ctx=canvas.getContext('2d'), image=host.querySelector('img'), illuminate=createPulseLight(host);
    if(!ctx){host.dataset.renderer='static-image';return {update(){}};}
    let frame=0,last=0,elapsed=0,lastPaint=0,ready=false,visible=true,iw=0,ih=0,ox=0,oy=0;
    const canMove=()=>ready&&!isPaused()&&!reduceMotion()&&!document.hidden&&visible;
    function size(){const box={width:host.clientWidth,height:host.clientHeight},dpr=Math.min(devicePixelRatio||1,1.25);canvas.width=Math.round(box.width*dpr);canvas.height=Math.round(box.height*dpr);const scale=Math.min(canvas.width/image.naturalWidth,canvas.height/image.naturalHeight);iw=image.naturalWidth*scale;ih=image.naturalHeight*scale;ox=(canvas.width-iw)/2;oy=(canvas.height-ih)/2;}
    const smooth=(a,b,v)=>{const t=Math.max(0,Math.min(1,(v-a)/(b-a)));return t*t*(3-2*t);};
    const bump=(t,a,b)=>t<=a||t>=b?0:Math.sin(Math.PI*(t-a)/(b-a))**2;
    function draw(t){const first=bump(t,.10,.43),second=bump(t,.53,.80),pulse=(first+.44*second)*strength;ctx.clearRect(0,0,canvas.width,canvas.height);
      const rows=160;
      function targetY(y){const weight=smooth(.30,.57,y);return y-(y-.56)*.052*pulse*weight;}
      for(let row=0;row<rows;row++){
        const y=row/rows,yn=(row+1)/rows,weight=smooth(.30,.57,y),squeeze=.105*pulse*weight;
        const width=iw*(1-squeeze),x=ox+(iw-width)*.52,ty=oy+ih*targetY(y),th=ih*(targetY(yn)-targetY(y));
        ctx.drawImage(image,0,y*image.naturalHeight,image.naturalWidth,image.naturalHeight/rows,x,ty,width,th+.6);
      }
      illuminate(pulse,t);
      host.dataset.beat=pulse.toFixed(3);host.dataset.phase=first>0?'primary':second>0?'secondary':'rest';
    }
    function tick(now){frame=0;if(!canMove())return;if(last)elapsed+=Math.min(now-last,70);last=now;if(now-lastPaint>=32){const start=performance.now();draw((elapsed%1800)/1000);host.dataset.paintMs=(performance.now()-start).toFixed(2);lastPaint=now;}frame=requestAnimationFrame(tick);}
    function update(){if(frame)cancelAnimationFrame(frame);frame=0;last=0;if(!ready)return;if(canMove()){host.dataset.motion='playing';frame=requestAnimationFrame(tick);}else{draw(0);host.dataset.motion='static';}}
    function load(){if(!image.naturalWidth)return;ready=true;size();draw(0);host.classList.add('is-rendering');host.dataset.renderer='canvas2d-regional-optical';update();}
    if(image.complete)load();else image.addEventListener('load',load,{once:true});
    new ResizeObserver(()=>{if(ready){size();draw(canMove()?(elapsed%1800)/1000:0);}}).observe(host);
    if('IntersectionObserver'in window)new IntersectionObserver(entries=>{visible=entries[0].isIntersecting;update();},{threshold:.01}).observe(host);
    return {update};
  }

  return hosts.map(createHeart);
}

