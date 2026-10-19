const fs=require("fs"),path=require("path"),ui=path.resolve(__dirname,"../UI");
require("./ui-panel-resize.cjs");
const css=fs.readFileSync(path.join(ui,'src/style.scss'),'utf8');
const bodyRule=css.match(/\.mw-tools-body\s*\{([^}]+)\}/)[1];
if(!/\bheight:\s*auto\s*;/.test(bodyRule)||!/\bmax-height:\s*85vh\s*;/.test(bodyRule))throw Error('Shared body must fit current content within the viewport cap');
if(!/\.mw-tools-section\s*\{[^}]*min-height:\s*0\s*;[^}]*overflow-y:\s*auto/.test(css))throw Error('Long content must scroll inside the content-sized window');
console.log('PASS: content-sized shared height and internal overflow regression checks.');
