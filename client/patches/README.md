# TanStack Svelte store identity patch

`@tanstack/svelte-store@0.12.1` uses deep `$state` for the selected slice in
`useSelector`. This proxies plain objects and arrays owned by the store, so its
comparison callback receives a proxy and the original value on later store
notifications. Browser traces identify `useSelector` → `defaultCompare` as the
source of Svelte's `state_proxy_equality_mismatch` warnings during form updates.

The patch uses `$state.raw` in both the source and published runtime. Selected
values retain their identity, while store notifications still trigger rendering
when the selected value is replaced. It does not suppress warnings or change
comparison semantics.

pnpm applies this version-scoped patch during install. When upgrading the package,
check whether upstream uses identity-preserving state and remove the patch if it
does. Run `pnpm test:unit --run`: the store identity harness covers array identity,
unrelated updates and replacements, and the browser setup rejects proxy-identity
warnings across component tests.
