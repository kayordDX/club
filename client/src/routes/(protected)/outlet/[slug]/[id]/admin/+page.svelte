<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import { Loader } from "@kayord/ui";

	$effect(() => {
		goto(resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings`), {
			replaceState: true,
		});
	});
</script>

<div class="flex items-center justify-center p-8">
	<Loader class="text-primary size-6" />
</div>
