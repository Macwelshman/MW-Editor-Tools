import React from "react";
import {trigger} from "cs2/api";

// Keep an optional tab's render failure inside the mod window.
export class TabErrorBoundary extends React.Component<React.PropsWithChildren<{}>, {failed:boolean}> {
    state = {failed:false};
    static getDerivedStateFromError() { return {failed:true}; }
    componentDidCatch(error:Error) {
        trigger("mwEditorTools", "frontendDiagnostic", JSON.stringify({tabError:String(error.message).slice(0,500)}));
    }
    render() {
        return this.state.failed ? <p role="alert">This tab could not be displayed. Switch tabs or close the window. Please report the error in UI.log.</p> : this.props.children;
    }
}
