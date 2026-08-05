// Override the root layout's `ssr = false` for this route so the remote
// function executes during SSR and the result is serialised into the HTML.
export const ssr = true;
