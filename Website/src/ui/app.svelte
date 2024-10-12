<script lang="ts" module>
	import { onMount, setContext, type Snippet } from 'svelte';
	import type { Readable } from 'svelte/motion';
	import { derived, get, writable, type Writable } from 'svelte/store';

	export const VIEW_MODE_NONE = 0b000;
	export const VIEW_MODE_MOBILE = 0b001;
	export const VIEW_MODE_DESKTOP = 0b010;
	export const VIEW_MODE_LIMITED_DESKTOP = 0b110;

	export type ViewMode =
		| typeof VIEW_MODE_NONE
		| typeof VIEW_MODE_MOBILE
		| typeof VIEW_MODE_DESKTOP
		| typeof VIEW_MODE_LIMITED_DESKTOP;

	export const WINDOW_MODE_NORMAL = 0b001;
	export const WINDOW_MODE_CUSTOM_BAR = 0b010;
	export const WINDOW_MODE_FULLSCREEN = 0b100;

	export type WindowMode =
		| typeof WINDOW_MODE_NORMAL
		| typeof WINDOW_MODE_CUSTOM_BAR
		| typeof WINDOW_MODE_FULLSCREEN;

	export const viewMode = writable<ViewMode>(VIEW_MODE_NONE);
	export const windowMode = writable<WindowMode>(WINDOW_MODE_NORMAL);

	function triggerUpdateViewMode(window: Window) {
		let newMode: ViewMode = get(viewMode);

		if (window.innerWidth >= 1280) {
			newMode = VIEW_MODE_DESKTOP;
		} else if (window.innerWidth >= 768) {
			newMode = VIEW_MODE_LIMITED_DESKTOP;
		} else if (window.innerWidth < 768) {
			newMode = VIEW_MODE_MOBILE;
		} else {
			newMode = VIEW_MODE_NONE;
		}

		if (newMode !== get(viewMode)) {
			viewMode.set(newMode);
		}
	}

	function triggerUpdateWindowMode(window: Window) {
		let newMode: WindowMode = get(windowMode);

		if (window.matchMedia('(display-mode: window-controls-overlay)').matches) {
			newMode = WINDOW_MODE_CUSTOM_BAR;
		} else if (window.matchMedia('(display-mode: fullscreen)').matches) {
			newMode = WINDOW_MODE_FULLSCREEN;
		} else {
			newMode = WINDOW_MODE_NORMAL;
		}

		if (newMode !== get(windowMode)) {
			windowMode.set(newMode);
		}
	}

	export type TopbarContentPosition = 'left' | 'center' | 'right';

	export interface AppContext {
		pushSidebarContent: (view: Snippet) => () => void;
		pushTopbarContent: (view: Snippet, position: TopbarContentPosition) => () => void;
		pushMainContent: (view: Snippet) => () => void;

		viewMode: Readable<number>;
		windowMode: Readable<number>;

		isDesktop(): boolean;
		isLimitedDesktop(): boolean;
		isMobile(): boolean;
	}

	export const AppContextName = 'app';
</script>

<script lang="ts">
	let sidebarContents: Snippet[] = $state([]);
	let topBarContents: { snippet: Snippet; position: TopbarContentPosition }[] = $state([]);
	let mainContent: Snippet[] = $state([]);

	const {}: AppContext = setContext<AppContext>(AppContextName, {
		pushSidebarContent: (view) => {
			sidebarContents = [...sidebarContents, view];

			return () => {
				sidebarContents = sidebarContents.filter((value) => value !== view);
			};
		},

		pushTopbarContent: (view, position) => {
			topBarContents = [...topBarContents, { snippet: view, position }];

			return () => {
				topBarContents = topBarContents.filter((value) => value.snippet !== view);
			};
		},

		pushMainContent: (view) => {
			mainContent = [...mainContent, view];

			return () => {
				mainContent = mainContent.filter((value) => value !== view);
			};
		},

		viewMode: derived(viewMode, (value) => value),
		windowMode: derived(windowMode, (value) => value),

		isDesktop: () => $viewMode === VIEW_MODE_DESKTOP || $viewMode === VIEW_MODE_LIMITED_DESKTOP,
		isLimitedDesktop: () => $viewMode === VIEW_MODE_LIMITED_DESKTOP,
		isMobile: () => $viewMode === VIEW_MODE_MOBILE
	});

	const { children }: { children: Snippet } = $props();

	onMount(() => {
		triggerUpdateViewMode(window);
		triggerUpdateWindowMode(window);
	});
</script>

<svelte:window
	onresize={() => {
		triggerUpdateViewMode(window);
		triggerUpdateWindowMode(window);
	}}
/>

{@render children()}
