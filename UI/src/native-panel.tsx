import React, { useEffect, useRef, useState } from "react";
import { getModule } from "cs2/modding";
import { PanelProps } from "cs2/ui";

type WindowProps = Omit<PanelProps, "draggable"> & {
    initialPosition?: { x: number; y: number };
    onPositionChange?: (position: { x: number; y: number }) => void;
};

// Preserve native dragging and focus handling; persist position through the mod backend.
export function NativePanel({ header, children, onPositionChange, ...props }: WindowProps) {
    const host = useRef<HTMLDivElement>(null);
    // Initial coordinates apply only when this panel mounts (or Reset remounts it).
    // Changing sections or refreshing backend state must not reset native dragging.
    const initialPosition = useRef(props.initialPosition);
    const [positionReady, setPositionReady] = useState(false);
    useEffect(() => {
        // Native positioning runs after mounting. Hide both initial frames so
        // the unsaved starting coordinates never become visible.
        let frame = requestAnimationFrame(() => {
            frame = requestAnimationFrame(() => setPositionReady(true));
        });
        return () => cancelAnimationFrame(frame);
    }, []);
    const savePosition = useRef(onPositionChange);
    savePosition.current = onPositionChange;
    useEffect(() => {
        const panel = host.current?.querySelector<HTMLElement>(".draggable-panel");
        if (!panel) return;
        let timer: ReturnType<typeof setTimeout> | undefined;
        let pending = false;
        const save = () => {
            if (!pending) return;
            pending = false;
            const rect = panel.getBoundingClientRect();
            if (!rect.width || !rect.height) return;
            const normalize = (offset: number, space: number) => space > 0 ? Math.max(0, Math.min(1, offset / space)) : 0;
            savePosition.current?.({ x: normalize(rect.left, window.innerWidth - rect.width), y: normalize(rect.top, window.innerHeight - rect.height) });
        };
        // Same observation approach as the color variation mod: save after native style updates.
        const observer = new MutationObserver(() => {
            if (panel.style.visibility === "hidden") return;
            pending = true;
            clearTimeout(timer);
            timer = setTimeout(save, 300);
        });
        observer.observe(panel, { attributes: true, attributeFilter: ["style"] });
        window.addEventListener("mouseup", save);
        return () => {
            observer.disconnect();
            clearTimeout(timer);
            window.removeEventListener("mouseup", save);
            save();
        };
    }, []);
    const DraggablePanel = getModule("game-ui/common/panel/draggable-panel/draggable-panel.tsx", "DraggablePanel") as React.ComponentType<WindowProps>;
    const TitleBar = getModule("game-ui/common/panel/panel-title-bar.tsx", "PanelTitleBar") as React.ComponentType<React.PropsWithChildren<{}>>;
    const ContentNavigation = getModule("game-ui/common/focus/auto-navigation-scope.tsx", "AutoNavigationScope") as
        React.ComponentType<React.PropsWithChildren<{ focusKey: string }>>;
    const editorTheme = getModule("game-ui/editor/themes/editor-panel.module.scss", "classes");
    // Disable the native opening transition to avoid a visible centre-to-position jump.
    return <div ref={host} style={{ position:"fixed",left:0,top:0,width:"100%",height:"100%",zIndex:100,
        opacity:positionReady?1:0,pointerEvents:"none" }}><DraggablePanel {...props} initialPosition={initialPosition.current} transition={null}
        style={{...props.style,pointerEvents:positionReady?"auto":"none"}}
        theme={editorTheme} header={<TitleBar>{header}</TitleBar>}>
        <ContentNavigation focusKey="mwEditorTools.content">{children}</ContentNavigation>
    </DraggablePanel></div>;
}

