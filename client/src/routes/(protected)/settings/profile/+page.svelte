<script lang="ts">
	import { UserIcon } from "@lucide/svelte";
	import PageHeading from "../PageHeading.svelte";
	import { Avatar, Card, Table } from "@kayord/ui";
	import { auth } from "$lib/stores/auth.svelte";
	import { getInitials } from "$lib/util";
</script>

<div class="m-4">
	<PageHeading title="Profile" description="Manage your user profile." icon={UserIcon} />

	<Card.Root class="mt-6">
		<Card.Header class="flex items-center gap-2">
			<Avatar.Root>
				<Avatar.Image src={auth.user?.profile.picture} alt="profile" />
				<Avatar.Fallback class="bg-primary text-primary-foreground">
					{getInitials(`${auth.user?.profile.name}`)}
				</Avatar.Fallback>
			</Avatar.Root>
			<div class="flex flex-col justify-center">
				<Card.Title>Profile Information</Card.Title>
				<Card.Description>Current information of logged in user</Card.Description>
			</div>
		</Card.Header>
		<Card.Content>
			<Table.Root>
				<Table.Body>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Name</Table.Cell>
						<Table.Cell class="text-end">{auth.user?.profile.name}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">First Name</Table.Cell>
						<Table.Cell class="text-end">{auth.user?.profile.given_name}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Last Name</Table.Cell>
						<Table.Cell class="text-end">{auth.user?.profile.family_name}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Email</Table.Cell>
						<Table.Cell class="text-end">{auth.user?.profile.email}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Email Verified</Table.Cell>
						<Table.Cell class="text-end">{auth.user?.profile.email_verified}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Phone Number</Table.Cell>
						<Table.Cell class="text-end">{auth.user?.profile.phone_number}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Phone Number Verified</Table.Cell>
						<Table.Cell class="text-end">{auth.user?.profile.phone_number_verified}</Table.Cell>
					</Table.Row>
				</Table.Body>
			</Table.Root>
		</Card.Content>
	</Card.Root>
</div>
