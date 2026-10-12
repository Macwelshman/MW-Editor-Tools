import React from "react";
import { getModule } from "cs2/modding";
import { Button } from "cs2/ui";
export function EditorButton(props: React.ComponentProps<typeof Button>) {
    const Component = getModule("game-ui/editor/widgets/button/button.tsx", "EditorButton") as React.ComponentType<React.ComponentProps<typeof Button>>;
    return <Component {...props} />;
}

export function RadioOption({ label, checked, onSelect }: { label: string; checked: boolean; onSelect: () => void }) {
    return <Button className={"mwet-colors-radio-option" + (checked ? " mwet-colors-radio-selected" : "")}
        role="radio" aria-label={label} aria-checked={checked} selected={checked} onSelect={onSelect}>
        <span className="mwet-colors-radio-indicator" aria-hidden="true"><span /></span>
        <span>{label}</span>
    </Button>;
}

