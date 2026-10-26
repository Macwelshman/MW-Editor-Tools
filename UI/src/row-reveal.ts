// Reveal the name/header, not the entire expanded item. Native component and page
// updates can arrive on different frames; wait for ancestor layout to settle.
export function scheduleRowReveal(read:()=>HTMLElement|null,request:(fn:FrameRequestCallback)=>number=requestAnimationFrame,cancel:(id:number)=>void=cancelAnimationFrame,reveal?:(element:HTMLElement)=>void){
 let frame=0,attempts=0,last="",stable=0,scrolled=false,stopped=false;
 const tick=()=>{
  if(stopped)return;
  const row=read();if(!row)return;
  const target=(row.querySelector(".mw-viewport-selected-box")||row) as HTMLElement;
  const visible=target.getClientRects().length>0;
  let snapshot="";
  if(visible){
   const rect=target.getBoundingClientRect();
   if(rect.width>0&&rect.height>0){
    snapshot=[rect.top,rect.left,rect.width,rect.height].join("|");
    let parent=target.parentElement;
    for(let i=0;parent&&i<24;i++,parent=parent.parentElement)snapshot+=`|${parent.clientHeight},${parent.clientWidth},${parent.scrollHeight}`;
   }
  }
  stable=snapshot&&snapshot===last?stable+1:0;last=snapshot;
  if(snapshot&&stable>=(scrolled?8:3)){
   if(reveal)reveal(target);
   else try{target.scrollIntoView?.({block:"nearest",inline:"nearest"});}catch{try{target.scrollIntoView?.(false);}catch{}}
   if(scrolled)return;scrolled=true;stable=0;
  }
  if(++attempts<60)frame=request(tick);
 };
 frame=request(tick);
 return()=>{stopped=true;cancel(frame);};
}
