import { getContext, setContext } from 'svelte';
import type { FileEntry } from './file-browser';
import { writable, type Writable } from 'svelte/store';

const contextName = 'File Browser List Context';

export type FileBrowserListContext = ReturnType<typeof createFileBrowserListContext>['context'];

export function createFileBrowserListContext() {
	const files: Writable<{ id: number; entry: FileEntry; element: HTMLElement, selected: Writable<boolean> }[]> = writable([]);

	const context = setContext(contextName, {
		pushFile: (entry: FileEntry, element: HTMLElement, selected: Writable<boolean>) => {
			const id = Math.random();

			files.update((value) => [...value, { id, entry, element, selected }]);

			return () => files.update((value) => value.filter((file) => file.id !== id));
		}
	});

	return { files, context };
}

export function useFileBrowserListContext() {
	return getContext<FileBrowserListContext>(contextName);
}
