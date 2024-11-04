import { type Snippet, getContext, setContext } from 'svelte';
import { derived, writable, type Writable } from 'svelte/store';

export enum ViewMode {
	None = 0,
	Mobile = 1,
	Desktop = 2,
	LimitedDesktop = 3
}

export enum WindowMode {
	Normal = 0b00,
	CustomBar = 1 << 1,
	Fullscreen = 1 << 2,
	Minimal = 1 << 3
}

const appContextName = `${Date.now()}`;

export function useAppContext() {
	return getContext<ReturnType<typeof createAppContext>['context']>(appContextName);
}

export function createAppContext() {
	const viewMode: Writable<ViewMode> = writable(ViewMode.None);
	const windowMode: Writable<WindowMode> = writable(WindowMode.Normal);
	const overlay: Writable<[id: number, snippet: Snippet, dim: boolean][]> = writable([]);
	const titleStack: Writable<{ id: number; title: string }[]> = writable([]);

	const context = setContext(appContextName, {
		pushOverlayContent: (view: Snippet, dim: boolean) => {
			const id = Math.random();

			overlay.update((overlay) => [...overlay, [id, view, dim]]);

			return () =>
				overlay.update((overlay) => (overlay = overlay.filter((value) => value[0] != id)));
		},

		pushTitle: (title: string) => {
			const id = Math.random();

			titleStack.update((value) => [...value, { id, title }]);

			return () => titleStack.update((value) => value.filter((value) => value.id !== id));
		},

		viewMode: derived(viewMode, (value) => value),
		windowMode: derived(windowMode, (value) => value),

		isDesktop: derived(
			viewMode,
			(mode) => mode === ViewMode.Desktop || mode === ViewMode.LimitedDesktop
		),
		isLimitedDesktop: derived(viewMode, (mode) => mode === ViewMode.LimitedDesktop),
		isMobile: derived(viewMode, (mode) => mode === ViewMode.Mobile),

		isNormal: derived(windowMode, (value) => value === 0),
		isCustomBar: derived(windowMode, (value) => (value & WindowMode.CustomBar) != 0),
		isFullscreen: derived(windowMode, (value) => (value & WindowMode.Fullscreen) != 0),
		isMinimal: derived(windowMode, (value) => (value & WindowMode.Minimal) != 0),

		currentTitle: derived(titleStack, (value) => value.at(-1))
	});

	return { viewMode, windowMode, overlay, titleStack, context };
}
