import { writable, type Writable } from 'svelte/store';
import type { IconOptions } from '../ui/icon.svelte';
import { getContext, setContext } from 'svelte';

const contextName = 'Admin Context';

export type AdminContext = ReturnType<typeof createAdminContext>['context'];

export function useAdminContext() {
	return getContext<AdminContext>(contextName);
}

export function createAdminContext() {
	const tabs: Writable<
		{
			id: number;
			callback: () => { path: string; label: string; icon: IconOptions };
		}[]
	> = writable([]);

	const context = setContext(contextName, {
		pushTab: (callback: () => { path: string; label: string; icon: IconOptions }) => {
			const id = Math.random();

			tabs.update((tabs) => [...tabs, { id, callback }]);

			return () => tabs.update((tabs) => tabs.filter((tab) => tab.id !== id));
		}
	});

	return { context };
}
