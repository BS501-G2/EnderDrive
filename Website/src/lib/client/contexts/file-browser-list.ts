import { getContext, setContext } from 'svelte';
import type { FileEntry } from './file-browser';
import { writable, type Writable } from 'svelte/store';

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

		selectedFileIds
	});

	return { files, context };
}

export function useFileBrowserListContext() {
	return getContext<FileBrowserListContext>(contextName);
}
