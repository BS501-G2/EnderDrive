import { getContext, setContext, type Snippet } from 'svelte';
import { writable, type Writable } from 'svelte/store';

const contextName = `${Date.now()}`;

export function useDashboardContext() {
	return getContext<ReturnType<typeof createDashboardContext>['context']>(contextName);
}

export function createDashboardContext() {
	const mobileButtons: Writable<{ id: number; snippet: Snippet }[]> = writable([]);
	const mobileTopLeft: Writable<{ id: number; snippet: Snippet }[]> = writable([]);
	const mobileTopRight: Writable<{ id: number; snippet: Snippet }[]> = writable([]);
	const mobileBottom: Writable<{ id: number; snippet: Snippet }[]> = writable([]);

	const desktopSide: Writable<{ id: number; snippet: Snippet }[]> = writable([]);
	const desktopTopLeft: Writable<{ id: number; snippet: Snippet }[]> = writable([]);

	const desktopTopMiddle: Writable<{ id: number; snippet: Snippet }[]> = writable([]);
	const desktopTopRight: Writable<{ id: number; snippet: Snippet }[]> = writable([]);

	const context = setContext(contextName, {
		pushMobileButton: (snippet: Snippet) => {
			const id = Math.random();
			mobileButtons.update((value) => [...value, { id, snippet }]);

			return () => mobileButtons.update((value) => value.filter((value) => value.id !== id));
		},

		pushMobileTopLeft: (snippet: Snippet) => {
			const id = Math.random();
			mobileTopLeft.update((value) => [...value, { id, snippet }]);

			return () => mobileTopLeft.update((value) => value.filter((value) => value.id !== id));
		},

		pushMobileTopRight: (snippet: Snippet) => {
			const id = Math.random();
			mobileTopRight.update((value) => [...value, { id, snippet }]);

			return () => mobileTopRight.update((value) => value.filter((value) => value.id !== id));
		},

		pushMobileBottom: (snippet: Snippet) => {
			const id = Math.random();
			mobileBottom.update((value) => [...value, { id, snippet }]);

			return () => mobileBottom.update((value) => value.filter((value) => value.id !== id));
		},

		pushDesktopSide: (snippet: Snippet) => {
			const id = Math.random();
			desktopSide.update((value) => [...value, { id, snippet }]);

			return () => desktopSide.update((value) => value.filter((value) => value.id !== id));
		},

		pushDesktopTopLeft: (snippet: Snippet) => {
			const id = Math.random();
			desktopTopLeft.update((value) => [...value, { id, snippet }]);

			return () => desktopTopLeft.update((value) => value.filter((value) => value.id !== id));
		},

		pushDesktopTopMiddle: (snippet: Snippet) => {
			const id = Math.random();
			desktopTopMiddle.update((value) => [...value, { id, snippet }]);

			return () => desktopTopMiddle.update((value) => value.filter((value) => value.id !== id));
		},

		pushDesktopTopRight: (snippet: Snippet) => {
			const id = Math.random();
			desktopTopRight.update((value) => [...value, { id, snippet }]);

			return () => desktopTopRight.update((value) => value.filter((value) => value.id !== id));
		}
	});

	return {
		mobileButtons,
		mobileTopLeft,
		mobileTopRight,
		mobileBottom,
		desktopSide,
		desktopTopLeft,
		desktopTopMiddle,
		desktopTopRight,
		context
	};
}
