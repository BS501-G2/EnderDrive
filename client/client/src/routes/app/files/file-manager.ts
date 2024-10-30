import type {
	FileAccessResource,
	FileLogResource,
	FileResource,
	FileSnapshotResource,
	UserResource
} from '@rizzzi/enderdrive-lib/server';
import type { FileAccessLevel, ScanFolderSortType } from '@rizzzi/enderdrive-lib/shared';
import type { Readable, Writable } from 'svelte/store';

export interface FileManagerNewProps {
	element: HTMLElement;

	presetFiles?: File[];

	onDismiss: () => void;

	uploadNewFiles: (files: File[]) => void;
	createNewFolder: (name: string) => void;
}

export type FileManagerAction = {
	name: string;
	icon: string;

	type: 'new' | 'modify' | 'arrange';

	action: (event: MouseEvent) => Promise<void>;
};
export enum FileManagerViewMode {
	Grid,
	List
}

export interface FileManagerSelection {
	saved: FileResource[];

	type: 'shift' | 'ctrl' | 'normal';

	x: number;
	y: number;
	w: number;
	h: number;

	boxX: number;
	boxY: number;
	boxW: number;
	boxH: number;
	boxS: number;
}

export type SourceEvent = MouseEvent | TouchEvent;

export type FileManagerOnFileIdCallback = (event: SourceEvent, fileId: number | null) => void;
export type FileManagerOnPageCallback = (
	event: SourceEvent,
	page: 'files' | 'shared' | 'trashed' | 'starred'
) => void;
export type FileManagerOnNewCallback = (event: SourceEvent, presetFiles?: File[]) => void;
export type FileManagerOnViewCallback = (event: SourceEvent) => void;
export type FileManagerOnClipboardCallback = (
	event: SourceEvent,
	files: FileResource[] | null,
	cut: boolean
) => void;
export type FileManagerOnSortCallback = (
	event: SourceEvent,
	sort: ScanFolderSortType,
	desc: boolean
) => void;

export type FileManagerProps = {
	refresh: Writable<() => void>;

	onFileId: FileManagerOnFileIdCallback;
	onPage: FileManagerOnPageCallback;
	onSort: FileManagerOnSortCallback;

	sort: Readable<[sort: ScanFolderSortType, desc: boolean]>;
} & (
	| {
			page: 'files';

			onNew: FileManagerOnNewCallback;
			onClipboard: FileManagerOnClipboardCallback;

			fileId: Readable<number | null>;
			clipboard: Readable<[files: FileResource[], cut: boolean] | null>;
	  }
	| {
			page: 'shared' | 'starred' | 'trash';
	  }
);

export type FileManagerResolved =
	| {
			status: 'loading';
	  }
	| {
			status: 'error';
			error: Error;
	  }
	| ({
			status: 'success';
			me: UserResource;
	  } & (
			| ({
					page: 'files';

					file: FileResource;
					filePathChain: FileResource[];
					myAccess: {
						level: FileAccessLevel;
						access: FileAccessResource | null;
					};
					accesses: FileAccessResource[];
					isStarred: boolean;
					logs: FileLogResource[];
					selection: Writable<FileResource[]>;
			  } & (
					| {
							type: 'file';

							viruses: string[];
							snapshots: FileSnapshotResource[];
					  }
					| {
							type: 'folder';

							files: FileResource[];
					  }
			  ))
			| {
					page: 'shared' | 'starred' | 'trash';

					files: FileResource[];
					selection: Writable<FileResource[]>;
			  }
	  ));

export interface FileManagerContext {
	refreshKey: Writable<number>;

	resolved: Writable<FileManagerResolved>;

	viewDialog: Writable<[element: HTMLElement] | null>;
	accessDialogs: Writable<[file: FileResource] | null>;

	listViewMode: Writable<FileManagerViewMode>;
	showSideBar: Writable<boolean>;
	addressBarMenu: Writable<[element: HTMLElement, file: FileResource] | null>;
}

export type FileManagerActionGenerator = () => FileManagerAction | null;

export const FileManagerPropsName = 'fm-props';
export const FileManagerContextName = 'fm-context';
