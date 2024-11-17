<script
  lang="ts"
>
  import { useServerContext } from '$lib/client/client';
  import type { FileProperties } from '$lib/client/contexts/file-browser';
  import Icon from '$lib/client/ui/icon.svelte';
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
  import Separator from '$lib/client/ui/separator.svelte';
  import { fly } from 'svelte/transition';
  import FileBrowserPropertiesDetails from './file-browser-properties-details.svelte';

  const {
    selectedFileIds
  }: {
    selectedFileIds: string[];
  } =
    $props();
  const {
    getFile,
    scanFile,
    getMainFileContent,
    getLatestFileSnapshot
  } =
    useServerContext();

  let promises: Promise<
    FileProperties[]
  > =
    $state(
      null as never
    );

  $effect(
    () => {
      promises =
        (() =>
          Promise.all(
            selectedFileIds.map(
              async (
                fileId
              ): Promise<FileProperties> => {
                const file =
                  await getFile(
                    fileId
                  );
                const fileContent =
                  await getMainFileContent(
                    file.id
                  );
                const fileSnapshot =
                  await getLatestFileSnapshot(
                    file.id,
                    fileContent.id
                  );
                const viruses =
                  fileSnapshot !=
                  null
                    ? await scanFile(
                        file.id,
                        fileContent.id,
                        fileSnapshot.id
                      )
                    : null;

                return {
                  file,
                  viruses
                };
              }
            )
          ))();
    }
  );
</script>

<div
  class="properties"
  transition:fly={{
    x: 16,
    duration: 150
  }}
>
  {#if promises != null}
    {#await promises}
      <div
        class="loading"
      >
        <LoadingSpinner
          size="3rem"
        />
      </div>
    {:then files}
      {#if files.length > 0}
        <div
          class="header"
        >
          <div
            class="preview"
          >
            <Icon
              icon="file"
              size="72px"
            />
          </div>

          <p
            class="title"
          >
            {files[0]
              ?.file
              .name}
          </p>
        </div>

        <Separator
          horizontal
        />

        <div
          class="details"
        >
          <FileBrowserPropertiesDetails
            {files}
          />
        </div>
      {:else}
        <div
          class="empty"
        >
          <Icon
            icon="file"
            size="72px"
          />
          <p
          >
            Select
            any
            file
            to
            examine
          </p>
        </div>
      {/if}
    {/await}
  {/if}
</div>

<style
  lang="scss"
>
  @use '../../../global.scss'
    as *;

  div.loading {
    flex-grow: 1;

    flex-direction: row;
    align-items: center;
    justify-content: center;
  }

  div.properties {
    flex-grow: 1;
  }

  div.header {
    gap: 32px;
    padding: 32px;

    > div.preview {
      align-items: center;
      justify-content: center;
    }

    > p.title {
      overflow: hidden;

      font-size: 1.2em;
      font-weight: bolder;
      text-align: center;
      word-wrap: break-word;
    }
  }

  div.empty {
    gap: 16px;
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
