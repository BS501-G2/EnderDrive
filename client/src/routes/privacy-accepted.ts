import { persisted } from "svelte-persisted-store";

export const privacyAccepted = persisted('privacy-accepted', false);
