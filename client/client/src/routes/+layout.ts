import { goto } from '$app/navigation';
import type { RequestEvent } from '@sveltejs/kit';
import { persisted } from 'svelte-persisted-store';
import { get } from 'svelte/store';

export async function load({ url }: RequestEvent) {
	const a = persisted('privacy-accepted', false);
	const agreementPath = '/agreement';

	if (get(a)) {
		if (url.pathname === agreementPath) {
			await goto('/');
		}
	} else {
		if (url.pathname !== agreementPath) {
			await goto('/agreement');
		}
	}
}
