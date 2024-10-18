<script lang="ts">
	import Icon from '$lib/ui/icon.svelte';

	import { Button } from '@rizzzi/svelte-commons';

	const { create }: { create: () => void } = $props();
</script>

<div class="head">
	<b class="title">Bulletin</b>

	<Button onClick={create}>
		<div class="new-button">
			<Icon icon="plus" />
			<p>Add new</p>
		</div>
	</Button>
</div>

<style lang="scss">
	div.head {
		flex-grow: 1;

		display: flex;
		flex-direction: row;

		align-items: center;

		gap: 8px;

		> b.title {
			flex-grow: 1;
		}

		div.new-button {
			display: flex;
			flex-direction: row;
			align-items: center;
			line-height: 1em;

			gap: 8px;
			padding: 4px 8px;
		}
	}
</style>
