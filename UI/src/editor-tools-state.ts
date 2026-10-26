import {bindValue} from "cs2/api";

// Share one native subscription across the window and Replacer.
export const editorToolsState = bindValue<string>("mwEditorTools", "state", "{}");
