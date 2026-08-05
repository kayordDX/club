<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import { Collapsible, Sidebar } from "@kayord/ui";
	import { BookIcon, BuildingIcon, ChevronLeftIcon, ChevronRightIcon, CreditCardIcon, StoreIcon } from "@lucide/svelte";

	const slug = $derived(page.params.slug ?? "");
	const id = $derived(page.params.id ?? "");
	const pathname = $derived(page.url.pathname);

	const facilityHref = $derived(resolve(`/outlet/${slug}/${id}`));
	const bookingsHref = $derived(resolve(`/outlet/${slug}/${id}/admin/bookings`));
	const paymentsHref = $derived(resolve(`/outlet/${slug}/${id}/admin/payments`));
	const outletHref = $derived(resolve(`/outlet/${slug}/${id}/admin/outlet`));
	const outletRulesHref = $derived(resolve(`/outlet/${slug}/${id}/admin/outlet/rules`));
	const facilityAdminHref = $derived(resolve(`/outlet/${slug}/${id}/admin/facility`));
	const facilityRulesHref = $derived(resolve(`/outlet/${slug}/${id}/admin/facility/rules`));

	const within = (href: string) => pathname === href || pathname.startsWith(`${href}/`);

	// Auto-expand a section when the active route is inside it; manual toggling still works.
	let outletOpen = $state(false);
	let facilityOpen = $state(false);
	$effect(() => {
		if (within(outletHref)) outletOpen = true;
	});
	$effect(() => {
		if (within(facilityAdminHref)) facilityOpen = true;
	});
</script>

<Sidebar.Root variant="sidebar">
	<Sidebar.Header class="p-0">
		<div class="border-border bg-card/50 flex h-14 items-center justify-between border-b p-2 backdrop-blur-sm">
			<LogoButton />
		</div>
	</Sidebar.Header>
	<Sidebar.Content class="px-1">
		<Sidebar.Group class="gap-1">
			<Sidebar.GroupLabel>Management</Sidebar.GroupLabel>
			<Sidebar.Menu class="gap-1">
				<Sidebar.MenuItem>
					<Sidebar.MenuButton isActive={within(bookingsHref)}>
						{#snippet child({ props })}
							<a href={bookingsHref} {...props}>
								<BookIcon />
								<span>Bookings</span>
							</a>
						{/snippet}
					</Sidebar.MenuButton>
				</Sidebar.MenuItem>

				<Sidebar.MenuItem>
					<Sidebar.MenuButton isActive={within(paymentsHref)}>
						{#snippet child({ props })}
							<a href={paymentsHref} {...props}>
								<CreditCardIcon />
								<span>Payments</span>
							</a>
						{/snippet}
					</Sidebar.MenuButton>
				</Sidebar.MenuItem>

				<Sidebar.MenuItem>
					<Collapsible.Root bind:open={outletOpen}>
						<Sidebar.MenuButton isActive={within(outletHref)}>
							{#snippet child({ props })}
								<a href={outletHref} {...props}>
									<StoreIcon />
									<span>Outlet</span>
								</a>
							{/snippet}
						</Sidebar.MenuButton>
						<Collapsible.Trigger>
							{#snippet child({ props })}
								<Sidebar.MenuAction {...props} aria-label="Toggle outlet menu">
									<ChevronRightIcon class={["transition-transform", outletOpen && "rotate-90"]} />
								</Sidebar.MenuAction>
							{/snippet}
						</Collapsible.Trigger>
						<Collapsible.Content>
							<Sidebar.MenuSub>
								<Sidebar.MenuSubItem>
									<Sidebar.MenuSubButton isActive={within(outletRulesHref)}>
										{#snippet child({ props })}
											<a href={outletRulesHref} {...props}>
												<span>Rules</span>
											</a>
										{/snippet}
									</Sidebar.MenuSubButton>
								</Sidebar.MenuSubItem>
							</Sidebar.MenuSub>
						</Collapsible.Content>
					</Collapsible.Root>
				</Sidebar.MenuItem>

				<Sidebar.MenuItem>
					<Collapsible.Root bind:open={facilityOpen}>
						<Sidebar.MenuButton isActive={within(facilityAdminHref)}>
							{#snippet child({ props })}
								<a href={facilityAdminHref} {...props}>
									<BuildingIcon />
									<span>Facility</span>
								</a>
							{/snippet}
						</Sidebar.MenuButton>
						<Collapsible.Trigger>
							{#snippet child({ props })}
								<Sidebar.MenuAction {...props} aria-label="Toggle facility menu">
									<ChevronRightIcon class={["transition-transform", facilityOpen && "rotate-90"]} />
								</Sidebar.MenuAction>
							{/snippet}
						</Collapsible.Trigger>
						<Collapsible.Content>
							<Sidebar.MenuSub>
								<Sidebar.MenuSubItem>
									<Sidebar.MenuSubButton isActive={within(facilityRulesHref)}>
										{#snippet child({ props })}
											<a href={facilityRulesHref} {...props}>
												<span>Rules</span>
											</a>
										{/snippet}
									</Sidebar.MenuSubButton>
								</Sidebar.MenuSubItem>
							</Sidebar.MenuSub>
						</Collapsible.Content>
					</Collapsible.Root>
				</Sidebar.MenuItem>
			</Sidebar.Menu>
		</Sidebar.Group>

		<Sidebar.Group class="mt-auto border-t pt-2">
			<Sidebar.Menu>
				<Sidebar.MenuItem>
					<Sidebar.MenuButton>
						{#snippet child({ props })}
							<a href={facilityHref} {...props}>
								<ChevronLeftIcon />
								<span>Back to facility</span>
							</a>
						{/snippet}
					</Sidebar.MenuButton>
				</Sidebar.MenuItem>
			</Sidebar.Menu>
		</Sidebar.Group>
	</Sidebar.Content>
</Sidebar.Root>
