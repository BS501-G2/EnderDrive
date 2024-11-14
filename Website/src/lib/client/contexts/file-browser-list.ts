import { getContext, setContext } from 'svelte';
import type { FileEntry } from './file-browser';
import { derived, writable, type Writable } from 'svelte/store';

const contextName = 'File Browser List Context';

export type FileBrowserListContext = ReturnType<typeof createFileBrowserListContext>['context'];

export function createFileBrowserListContext() {
	const files: Writable<{ id: number; entry: FileEntry; element: HTMLElement }[]> = writable([]);
	const selectedFileIds: Writable<string[]> = writable([]);

	const context = setContext(contextName, {
		pushFile: (entry: FileEntry, element: HTMLElement) => {
			const id = Math.random();

			files.update((value) => [...value, { id, entry, element }]);

			return () => files.update((value) => value.filter((file) => file.id !== id));
		},

		selectFile: (id: string) => {
			selectedFileIds.update((value) => [...value, id]);
		},

		deselectFile: (id: string) => {
			selectedFileIds.update((value) => value.filter((value) => value !== id));
		},

		selectedFileIds
	});

	return { files, selectedFileIds, context };
}

export function useFileBrowserListContext() {
	return getContext<FileBrowserListContext>(contextName);
}
