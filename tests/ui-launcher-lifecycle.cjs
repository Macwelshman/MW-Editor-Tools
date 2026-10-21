const fs=require('fs'),vm=require('vm'),assert=require('assert');
const React=require('../UI/node_modules/react'),ts=require('../UI/node_modules/typescript');
const geometry={};vm.runInNewContext(ts.transpileModule(fs.readFileSync('UI/src/launcher-position.ts','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS}}).outputText,{exports:geometry});
let hooks=[],cursor=0,effects=[],dirty=true,frames=new Map(),timers=new Map(),listeners=new Map(),id=0,mounted=true;
let state={available:true,visible:false},sidebar=500,frozen=false,hidden=false,updates=0,tree;
const rect=(x,w=56)=>({left:x,right:x+w,top:65,bottom:121,width:w,height:w});
let neighbours=[rect(520)];
const node={contains:()=>false,getBoundingClientRect:()=>rect(frozen?520:520+parseFloat(tree?.props.style.marginLeft||0),hidden?0:56)};
const react={...React,useRef:value=>{const i=cursor++;return hooks[i]??(hooks[i]={current:value})},useState:value=>{const i=cursor++;if(!(i in hooks))hooks[i]=value;return[hooks[i],next=>{updates++;if(!Object.is(hooks[i],next)){hooks[i]=next;dirty=true}}]},useLayoutEffect:(fn,deps)=>{const i=cursor++,old=hooks[i];if(!old||deps.some((d,j)=>!Object.is(d,old.deps[j]))){effects.push(()=>{old?.cleanup?.();hooks[i]={deps,cleanup:fn()}})}}};
const out={},components=[];
vm.runInNewContext(ts.transpileModule(fs.readFileSync(process.env.MW_LAUNCHER_SOURCE||'UI/src/index.tsx','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.React,esModuleInterop:true}}).outputText,{exports:out,require:name=>name==='./component-headings'?{registerComponentHeadings:()=>{}}:name==='react'?react:name==='cs2/api'?{bindValue:(_,key)=>key,useValue:key=>key==='width'?sidebar:JSON.stringify(state),trigger:()=>{}}:name==='cs2/ui'?{Button:'button',Tooltip:'tooltip'}:name==='./launcher-position'?geometry:{},document:{querySelectorAll:()=>neighbours.map(bounds=>({closest:()=>null,getBoundingClientRect:()=>bounds}))},window:{getComputedStyle:()=>({display:'block',visibility:'visible'}),requestAnimationFrame:fn=>{frames.set(++id,fn);return id},cancelAnimationFrame:n=>frames.delete(n),setInterval:fn=>{timers.set(++id,fn);return id},clearInterval:n=>timers.delete(n),addEventListener:(name,fn)=>listeners.set(name,fn),removeEventListener:(name,fn)=>{if(listeners.get(name)===fn)listeners.delete(name)}}});
out.default({append:(_,c)=>components.push(c)});
function flush(){let renders=0;while(dirty||effects.length){if(++renders>55)throw Error('Maximum update depth regression');if(dirty){dirty=false;cursor=0;tree=components[0]();if(tree)tree.ref.current=node;}const jobs=effects;effects=[];jobs.forEach(fn=>fn());}return renders;}
function frame(){const jobs=[...frames.values()];frames.clear();jobs.forEach(fn=>fn());flush();}
function poll(){[...timers.values()].forEach(fn=>fn());}
if(process.argv.includes('--expect-recursion')){frozen=true;try{flush();}catch(error){if(error.message==='Maximum update depth regression'){console.log('PASS: original launcher reproduces maximum update depth with frozen developer-overlay bounds');process.exit(0);}throw error;}throw Error('Expected original launcher recursion was not reproduced');}
flush();assert.equal(updates,0,'Mount must not synchronously measure and update state');assert.equal(frames.size,1,'One deferred mount measurement');frame();assert.equal(tree.props.style.marginLeft,'64px');assert.equal(frames.size,0,'Own offset render does not queue another measurement');assert.equal(timers.size,1);
// A developer overlay can temporarily return an unchanged rect after each move.
frozen=true;for(let i=0;i<80;i++){poll();poll();listeners.get('resize')();assert.equal(frames.size,1,'Resize/timer bursts coalesce');const before=updates;frame();assert.ok(updates<=before+1,'At most one state update per external measurement');assert.equal(frames.size,0,'Frozen native bounds cannot create an autonomous update loop');}
hidden=true;const before=updates;poll();frame();assert.equal(updates,before,'Hidden UI measurements do not move the launcher');
hidden=false;frozen=false;neighbours=[];poll();frame();assert.equal(tree.props.style.marginLeft,'0px','Returning game UI reclaims the original slot');
state.visible=true;dirty=true;flush();frame();assert.equal(tree.props.children.props.children.props.selected,true,'Window toggle survives developer transition');
poll();assert.equal(frames.size,1);const cleanups=hooks.filter(h=>h&&typeof h.cleanup==='function');cleanups.forEach(h=>h.cleanup());assert.equal(frames.size,0);assert.equal(timers.size,0);assert.equal(listeners.size,0,'Unmount removes pending frames, timers and resize handlers');
console.log('PASS: actual launcher hook lifecycle; developer-menu frozen/hidden layout, 80 transitions, coalescing, remount and cleanup without update recursion.');
