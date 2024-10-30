import { getContext, setContext, type Snippet } from 'svelte';
import { writable, type Writable } from 'svelte/store';

export type TopbarContentPosition = 'left' | 'center' | 'right';

export interface DashboardContext {
	pushSideContent: (view: Snippet) => () => void;
	pushTopContent: (view: Snippet, position: TopbarContentPosition) => () => void;
	pushContent: (view: Snippet) => () => void;
}

const contextName = `${Date.now()}`;

export function useDashboardContext() {
	return getContext<DashboardContext>(contextName);
}

export function createDashboardContext() {
	const side: Writable<Snippet[]> = writable([]);
	const mainTop: Writable<{ snippet: Snippet; position: TopbarContentPosition }[]> = writable([]);
	const mainContent: Writable<Snippet[]> = writable([]);

	const context = setContext<DashboardContext>(contextName, {
		pushSideContent: (view) => {
			side.update((side) => [...side, view]);

			return () => side.update((side) => side.filter((value) => value !== view));
		},

		pushTopContent: (view, position) => {
			mainTop.update((mainTop) => [...mainTop, { snippet: view, position }]);

			return () =>
				mainTop.update((mainTop) => mainTop.filter((mainTop) => mainTop.snippet !== view));
		},

		pushContent: (view) => {
			mainContent.update((mainContent) => [...mainContent, view]);

			return () =>
				mainContent.update((mainContent) => mainContent.filter((value) => value !== view));
		}
	});

	return { side, mainTop, mainContent, context };
}
