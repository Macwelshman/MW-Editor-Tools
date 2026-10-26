const fs=require('fs'),vm=require('vm'),assert=require('assert'),ts=require('../UI/node_modules/typescript');
const api={};vm.runInNewContext(ts.transpileModule(fs.readFileSync('UI/src/row-reveal.ts','utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS}}).outputText,{exports:api});
let frame=0,queue=[],next=0,canceled=new Set(),calls=[];
const request=f=>{queue.push({id:++next,f});return next},cancel=id=>canceled.add(id);
const parent={clientWidth:250,scrollHeight:1000,get clientHeight(){return frame<16?frame*10:frame<23?160:220},parentElement:null};
const box={getClientRects:()=>frame<8?[]:[{}],getBoundingClientRect:()=>({top:500,left:0,width:200,height:25}),parentElement:parent,scrollIntoView:()=>calls.push(frame)};
const row={querySelector:()=>box,scrollIntoView:()=>{throw Error('expanded item body must not be scrolled instead of header')}};
api.scheduleRowReveal(()=>row,request,cancel);
while(queue.length){const batch=queue;queue=[];frame++;for(const e of batch)if(!canceled.has(e.id))e.f();assert(frame<100,'bounded reveal');}
assert.equal(calls.length,2);assert(calls[0]>=19,'wait for initial expansion to settle');assert(calls[1]>=31,'second positioning follows later ancestor size change');
frame=0;queue=[];calls=[];box.getClientRects=()=>[];api.scheduleRowReveal(()=>row,request,cancel);while(queue.length){const e=queue.shift();frame++;e.f();}assert.equal(frame,60,'hidden rows have a bounded wait');assert.equal(calls.length,0);
queue=[];box.getClientRects=()=>[{}];const stop=api.scheduleRowReveal(()=>row,request,cancel);stop();while(queue.length){const e=queue.shift();e.f();}assert.equal(calls.length,0,'changing selection cancels old reveal');
console.log('PASS: delayed mount, animated ancestor layout, header-only scrolling, final positioning, bounded hidden wait and selection cancellation.');
