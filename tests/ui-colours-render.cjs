const fs=require('fs'),path=require('path'),vm=require('vm');
const ui=path.resolve(__dirname,'../UI'),React=require(path.join(ui,'node_modules/react'));
const {renderToStaticMarkup}=require(path.join(ui,'node_modules/react-dom/server')),ts=require(path.join(ui,'node_modules/typescript'));
let state={available:true,visible:true,hasTarget:true,hasColors:true,target:'Mesh',targets:['Mesh','Second'],targetIndex:0,group:'A',groupCount:2,groupIndex:0,pairs:'#FF000080 #00FF00FF',channel2:'#0000FFFF',canApply:true,canUndo:true,canPaste:true,generated:1,total:1};
let paletteOverride;let effects=[];let stringHook=0;const handlers={},calls=[],cache={};
const button=({children,onSelect,theme,selected,variant,focusKey,...props})=>{if(typeof children==='string')handlers[children]=onSelect;return React.createElement('button',props,children);};
const hooks={...React,useState:initial=>[typeof initial==='string' && paletteOverride!==undefined && stringHook++===0 ? paletteOverride : initial===''?(stringHook++===0?state.pairs:state.channel2):initial,()=>{}],useEffect:effect=>effects.push(effect),useRef:()=>({current:undefined})};
function load(file){if(cache[file])return cache[file];const out={};const code=ts.transpileModule(fs.readFileSync(path.join(ui,'src/colours',file),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.React,esModuleInterop:true}}).outputText;
 vm.runInNewContext(code,{exports:out,require:name=>{
  if(name==='react')return hooks;
  if(name==='cs2/api')return{bindValue:(group,key)=>({group,key}),useValue:()=>JSON.stringify(state),trigger:(...args)=>calls.push(args)};
  if(name==='cs2/modding')return{getModule:()=>button};
  if(name==='cs2/ui')return{Button:button,Scrollable:({children,...props})=>React.createElement('div',{className:props.className},children),Tooltip:({children})=>children};
  if(name==='./color-picker')return{ColorPicker:()=>React.createElement('div',null,'Picker')};
  if(name==='./palette')return load('palette.ts');if(name==='./native-panel')return load('native-panel.tsx');
  if(name==='./window.scss')return{};throw Error(name);
 }});return cache[file]=out;
}
const component=load('window.tsx').ColourVariationsContent;
let checks=0;const check=(b,m)=>{if(!b)throw Error(m);checks++;};
const render=()=>{effects=[];stringHook=0;return renderToStaticMarkup(React.createElement(component));};
let html=render();
check(!html.includes('draggable-panel')&&!html.includes('launcher'),'Integrated content creates no second window or launcher');
check(!html.includes('Colours follow the selected item automatically.')&&!html.includes('Use current selection')&&html.includes('Choose target mesh'),'Selection and multi-mesh choice retained');
check(html.includes('Copy')&&html.includes('Paste')&&html.includes('Undo last edit'),'Copy paste and own edit Undo retained');
check(html.includes('Channel 0')&&html.includes('Channel 1')&&html.includes('Channel 2')&&html.includes('#FF000080'),'All channel lists and alpha value retained');
check(html.includes('Previous group')&&html.includes('Next group')&&html.includes('Replace group')&&html.includes('Append to group'),'Group and append controls retained');
check(html.includes('1 combinations generated')&&html.includes('1 total variations'),'Combination summary retained');
handlers['Generate combinations']();const preview=JSON.parse(calls.at(-1)[2]);
check(calls.at(-1)[0]==='mwEditorToolsColors'&&preview.action==='preview'&&preview.pairs===state.pairs&&preview.channel2===state.channel2,'Distinct binding sends complete paired palette');
handlers['Apply combinations']();check(JSON.parse(calls.at(-1)[2]).action==='apply','Apply command retained');
handlers['Undo last edit']();check(JSON.parse(calls.at(-1)[2]).action==='undo','Colour Undo command retained');
state={...state,confirmPaste:true};html=render();check(html.includes('Confirm paste')&&html.includes('replace ALL Colour Properties'),'Full paste confirmation retained');
state={...state,changed:true};html=render();check(/<button[^>]*disabled=""[^>]*>Generate combinations/.test(html),'Stale selection blocks generated palette');
state={...state,changed:false,blockedReason:'Wait for saving',canApply:false,canUndo:false,canPaste:false};html=render();
check(html.includes('Wait for saving')&&/<button[^>]*disabled=""[^>]*>Apply combinations/.test(html)&&/<button[^>]*disabled=""[^>]*>Undo last edit/.test(html),'Saving or recovery interlock visible and mutation actions disabled');
state={...state,hasTarget:false,hasColors:false,canUndo:false};html=render();check(!html.includes('Channel 0')&&html.includes('Select an object or mesh in Object Info.'),'No compatible target has actionable capture guidance');
state={available:false};html=render();check(html.includes('Waiting for Colour Variations'),'Unavailable module has visible status');
for(const count of [0,1,2,3,4,7]) {
 state={available:true,visible:true,hasTarget:true,hasColors:true,target:'Mesh',pairs:Array(count).fill('#FF000080 #00FF00FF').join('\n'),channel2:Array(count).fill('#0000FFFF').join('\n'),canApply:false};
 html=render();
 check((html.match(/class="mwet-colors-loaded mwet-colors-loaded-scroll"/g)||[]).length===(count>3?3:0),'Only lists exceeding three swatches use a scroll viewport: '+count);
 check((html.match(/class="mwet-colors-loaded mwet-colors-loaded-static"/g)||[]).length===(count<=3?3:0),'Short lists use natural height: '+count);
}
check(html.includes('Copy Colour Properties')&&html.includes('Reload colours from asset')&&!html.includes('Copy Color Properties'),'Visible colour spelling consistent');
console.log('PASS: '+checks+' migrated Colours content/command checks; native interaction pending.');
state={available:true,visible:true,changed:true,selectionRevision:41,paletteRevision:8,hasTarget:true,hasColors:true,target:'Source',pairs:'',channel2:'',generated:0};
render();const before=calls.length;effects.forEach(effect=>effect());
check(calls.slice(before).some(c=>JSON.parse(c[2]).action==='follow'&&JSON.parse(c[2]).index===41),'Clean selection follows with current selection revision');
state={...state,generated:2};html=render();const beforeDraft=calls.length;effects.forEach(effect=>effect());
check(!calls.slice(beforeDraft).some(c=>JSON.parse(c[2]).action==='follow'),'Generated draft prevents automatic capture');
check(html.includes('Discard edits and follow selection')&&html.includes('Source'),'Selection change names draft and asks before discard');
handlers['Discard edits and follow selection']();check(JSON.parse(calls.at(-1)[2]).action==='follow'&&JSON.parse(calls.at(-1)[2]).index===41,'Explicit discard follows exact observed selection');
check(html.includes('Copy colour settings')&&html.includes('Paste colour settings'),'Copy and paste describe colour settings');
console.log('PASS: automatic selection and draft confirmation checks.');state={...state,generated:0};paletteOverride='#ABCDEF00 #123456FF';html=render();const beforeEdits=calls.length;effects.forEach(effect=>effect());
check(html.includes('Discard edits and follow selection')&&!calls.slice(beforeEdits).some(c=>JSON.parse(c[2]).action==='follow'),'Ungenerated colour edits also block automatic capture');
paletteOverride=undefined;
console.log('PASS: ungenerated draft protection check.');
state={available:true,hasTarget:false,hasColors:false,status:'Select an object or mesh so its Object Info panel is active first.'};html=render();check(html.includes('Select an object or mesh in Object Info.')&&!html.includes('Apply combinations')&&!html.includes('Target mesh'),'One compact empty-state prompt without disabled actions');


