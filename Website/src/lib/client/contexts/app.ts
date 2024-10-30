import { type Snippet, getContext, setContext } from 'svelte';
import type { Readable } from 'svelte/motion';
import { derived, type Writable } from 'svelte/store';

export enum ViewMode {
	None = 0,
	Mobile = 1,
	Desktop = 2,
	LimitedDesktop = 3
}

export enum WindowMode {
	Normal = 0b001,
	CustomBar = 0b010,
	Fullscreen = 0b100
}

export interface AppContext {
	pushOverlayContent: (view: Snippet) => () => void;

	viewMode: Readable<number>;
	windowMode: Readable<number>;

	isDesktop: Readable<boolean>;
	isLimitedDesktop: Readable<boolean>;
	isMobile: Readable<boolean>;
}

const appContextName = `${Date.now()}`;

export function useAppContext() {
	return getContext<AppContext>(appContextName);
}

export function createAppContext(
	viewMode: Writable<ViewMode>,
	windowMode: Writable<WindowMode>,
	overlay: Writable<[id: number, snippet: Snippet][]>
) {
	return setContext<AppContext>(appContextName, {
		pushOverlayContent: (view) => {
			const id = Math.random();

			overlay.update((overlay) => [...overlay, [id, view]]);

			return () =>
				overlay.update((overlay) => (overlay = overlay.filter((value) => value[0] != id)));
		},

		viewMode: derived(viewMode, (value) => value),
		windowMode: derived(windowMode, (value) => value),

		isDesktop: derived(
			viewMode,
			(mode) => mode === ViewMode.Desktop || mode === ViewMode.LimitedDesktop
		),
		isLimitedDesktop: derived(viewMode, (mode) => mode === ViewMode.LimitedDesktop),
		isMobile: derived(viewMode, (mode) => mode === ViewMode.Mobile)
	});
}
