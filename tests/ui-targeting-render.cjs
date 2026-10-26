const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const ui=path.resolve(__dirname,'../UI'),base=require(path.join(ui,'node_modules/react'));
const {renderToStaticMarkup}=require(path.join(ui,'node_modules/react-dom/server'));
const ts=require(path.join(ui,'node_modules/typescript'));
let rows={asset:'House',rows:[],clipboard:'Clipboard empty',hasRow:false,canPaste:false},target={available:true,active:false,status:'Ready'};
let actions=[],sent=[],selectChange;
const React={...base,createElement:(type,props,...children)=>{if(type==='select')throw Error('Browser-only select is unsafe in Cohtml');return base.createElement(type,props,...children)}};
const out={};const button=({children,onSelect,theme,selected,...props})=>{actions.push({label:children,onSelect,disabled:props.disabled});return base.createElement('button',props,children)};
vm.runInNewContext(ts.transpileModule(fs.readFileSync(path.join(ui,'src/prop-replacer.tsx'),'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.React,esModuleInterop:true}}).outputText,{
 exports:out,require:name=>name==='react'?React:name==='cs2/api'?{
 bindValue:(group,key)=>({group,key}),useValue:binding=>JSON.stringify(binding.group==='mwPropReplacer'?rows:{targeting:target}),
 trigger:(group,key,value)=>sent.push({group,key,value})}:name==='./editor-tools-state'?{editorToolsState:{group:'mwEditorTools',key:'state'}}:name==='cs2/modding'?{getModule:()=>({button:'native'})}:name==='cs2/ui'?{Button:button,Tooltip:({children})=>children,Dropdown:({content,children})=>base.createElement('div',null,children,content),DropdownToggle:({children,...props})=>base.createElement('button',props,children),DropdownItem:({children,value,onChange})=>{if(value==='markers')selectChange=()=>onChange(value);return base.createElement('div',{'data-filter-option':value},children)}}:(()=>{throw Error(name)})()
});
let checks=0;const check=(value,name)=>{assert(value,name);checks++};
check(!fs.readFileSync(path.join(ui,'src/index.tsx'),'utf8').includes('bindValue<string>("mwEditorTools"')&&!fs.readFileSync(path.join(ui,'src/prop-replacer.tsx'),'utf8').includes('bindValue<string>("mwEditorTools"'),'window and Replacer reuse shared state subscription');
const render=()=>{actions=[];selectChange=null;return renderToStaticMarkup(base.createElement(out.PropReplacerContent,{targeting:target}))};
check(fs.readFileSync(path.join(ui,'src/index.tsx'),'utf8').includes('<PropReplacerContent targeting={state.targeting}/>'),'targeting comes from the window snapshot');
let html=render();check(actions.some(a=>a.label==='Start replacing'&&!a.disabled),'targeting opt-in available');check(!html.includes('Element type')&&html.includes('Start replacing to select elements')&&!html.includes('Undo:'),'inactive replacing shows workflow guidance without inactive filters');
actions.find(a=>a.label==='Start replacing').onSelect();check(sent.pop().value==='targetToggle','explicit activation command');
target={...target,active:true,filter:'all',owner:'House',hover:'Prop: Bench',selected:'Prop: Bench',hoverIsSelected:false,outlineStatus:'Outline prototype: gold hover, cyan selected.',feedback:'Anarchy protection prevents Undo capture.',status:'Cyan selected; gold hover.'};rows={...rows,rows:[{index:0,type:'Prop',value:'Old row'}]};
html=render();check(html.includes('Selected:')&&html.includes('Hover:'),'same-name distinct elements retain both labels');check(html.includes('Anarchy protection'),'eligibility reason visible');check(html.includes('Outline prototype: gold hover, cyan selected.'),'prototype rendering status visible separately from Undo feedback');
check(html.includes('Replacing active.')&&html.includes('Element type')&&!html.includes('Start replacing</button>'),'start replacing enables targeting directly');
actions.find(a=>a.label==='Stop replacing').onSelect();check(sent.pop().value==='targetToggle','stop replacing returns through existing native tool cleanup');
check(!html.includes('Old row'),'target mode has one clear selection source rather than a competing row list');
check(html.includes('Areas')&&!html.includes('Spaces'),'space category is presented as Areas');
check((html.match(/data-filter-option=/g)||[]).length===9,'all supported categories available without nine toggle buttons');
selectChange();check(sent.pop().value==='targetFilter|markers','filter uses guarded shared command');
actions.find(a=>a.label==='Clear selection').onSelect();check(sent.pop().value==='targetClear','explicit selection clear command');
check(!actions.find(a=>a.label==='Copy').disabled,'target enables Copy without a component row');actions.find(a=>a.label==='Copy').onSelect();let cmd=sent.pop();check(cmd.group==='mwPropReplacer'&&cmd.value==='copyTarget','Copy routes through exact target resolver');
check(actions.find(a=>a.label==='Replace').disabled,'empty clipboard cannot replace target');
rows={...rows,clipboard:'Object: Bench'};render();check(!actions.find(a=>a.label==='Replace').disabled,'target plus clipboard enables guarded replacement');actions.find(a=>a.label==='Replace').onSelect();check(sent.pop().value==='pasteTarget','Replace uses target command');actions.find(a=>a.label==='Replace all').onSelect();check(sent.pop().value==='batchTarget','Replace all uses existing target batch resolver');
target={...target,selected:'',hover:'',feedback:''};render();check(actions.find(a=>a.label==='Copy').disabled&&actions.find(a=>a.label==='Replace').disabled,'cleared target cannot fall back to old row');
target={...target,active:false,available:false,selected:'stale',feedback:'stale',status:'Return to the native Select tool before targeting.'};html=render();check(!html.includes('stale')&&!html.includes('Clear selection'),'inactive mode hides stale target feedback');check(actions.find(a=>a.label==='Start replacing').disabled,'unavailable targeting cannot activate');check(html.includes('Return to the native Select tool before targeting.')&&!html.includes('Element type'),'disabled start button has visible reason');
rows={...rows,hasRow:true,canPaste:true};render();actions.find(a=>a.label==='Copy').onSelect();check(sent.pop().value==='copy','native row Copy retains original command');actions.find(a=>a.label==='Replace').onSelect();check(sent.pop().value==='paste','native row Replace retains original command');
rows={...rows,pasteBlocked:'Only compatible surface-to-surface replacement is supported. Other area types remain unavailable.'};target={...target,active:true,selected:'Surface: Tiles',hover:''};html=render();
check(html.includes(rows.pasteBlocked),'blocked unsupported area clipboard explains the native crash restriction');
check(actions.find(a=>a.label==='Replace').disabled&&actions.find(a=>a.label==='Replace all').disabled,'unsupported area clipboard disables single and batch viewport replacement');
check(!actions.find(a=>a.label==='Copy').disabled&&!actions.find(a=>a.label==='Clear Clipboard').disabled,'surface copy and clipboard clear remain available');
target={...target,active:false};render();check(actions.find(a=>a.label==='Replace').disabled&&actions.find(a=>a.label==='Replace all').disabled,'unsupported area block also applies to browser-list replacement');
const system=fs.readFileSync(path.resolve(ui,'../src/Modules/PropReplacer/PropReplacerSystem.cs'),'utf8');
const replace=system.slice(system.indexOf('private void Replace(Row row,bool batch)'),system.indexOf('private void PasteHover'));
check(replace.indexOf('if(PasteBlockReason.Length>0)throw')>=0&&replace.indexOf('if(PasteBlockReason.Length>0)throw')<replace.indexOf('BeforeWrite?.Invoke()')&&replace.indexOf('if(PasteBlockReason.Length>0)throw')<replace.indexOf('PrefabSnapshot.Take'),'all replacement entry points refuse before history invalidation and source mutation');
target={...target,active:true,selected:'',hover:'Surface: Tiles'};rows={...rows,pasteBlocked:''};render();check(actions.find(a=>a.label==='Copy').disabled&&actions.find(a=>a.label==='Replace').disabled,'panel buttons require a clicked selection instead of using a transient hover');
console.log(`PASS: ${checks} targeting UI and authoritative clipboard dispatch assertions.`);
