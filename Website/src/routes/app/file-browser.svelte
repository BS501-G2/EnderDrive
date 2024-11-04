<script lang="ts">
	import { useClientContext } from '$lib/client/client';
	import type { FileResource } from '$lib/client/client-server-side-request';
	import { useAppContext } from '$lib/client/contexts/app';
	import {
		createFileBrowserContext,
		FileBrowserResolveType,
		type FileBrowserOptions,
		type FileBrowserResolve
	} from '$lib/client/contexts/file-browser';
	import Separator from '$lib/client/ui/separator.svelte';
	import FileBrowserPath from './file-browser-path.svelte';
	import FileManagerActionHost from './file-manager-action-host.svelte';
	import { loremIpsum } from 'lorem-ipsum';

	const { isMobile } = useAppContext();
	const { showDetails, isLoading, files, actions, top } = createFileBrowserContext();
	const {
		functions: { getFiles: scanFolder, getFileAccesses, getFileStars, whoAmI, getFile, getFiles }
	} = useClientContext();
	const { resolve, onFileId }: FileBrowserOptions = $props();

	async function load(resolve: FileBrowserResolve): Promise<FileResource[]> {
		const { userId } = await whoAmI();
		if (userId == null) {
			throw Error('Not logged in');
		}

		switch (resolve[0]) {
			case FileBrowserResolveType.File: {
				const [, fileId] = resolve;

				return await scanFolder(fileId || undefined);
			}

			case FileBrowserResolveType.Shared: {
				const files = await Promise.all(
					(await getFileAccesses()).map(async (fileAccess) => {
						const file = await getFile(fileAccess.fileId);

						return file;
					})
				);

				return files;
			}

			case FileBrowserResolveType.Starred: {
				const files = getFileStars(void 0, userId).then((fileStars) =>
					Promise.all(
						fileStars.map(async (fileStar): Promise<FileResource> => {
							const file = await getFile(fileStar.fileId);

							return file;
						})
					)
				);

				return files;
			}

			case FileBrowserResolveType.Trash: {
				const files = await getFiles();
				break;
			}
		}
	}
</script>

<div class="file-browser">
	<div class="top">
		{#each $top as { id, snippet } (id)}
			{@render snippet()}
		{/each}
	</div>

	<Separator horizontal />

	<div class="middle">
		{loremIpsum({ count: 100 })}
	</div>
</div>

<FileBrowserPath />
<FileManagerActionHost {actions} />

<style lang="scss">
	@use '../../global.scss' as *;

	div.file-browser {
		flex-grow: 1;

		min-width: 0;
		min-height: 0;

		> div.top {
			flex-direction: row;
		}

		> div.middle {
			flex-grow: 1;

			overflow: hidden auto;

			min-height: 0;
		}

		> div.bottom {

		}
	}
</style>
