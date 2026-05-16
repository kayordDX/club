<script lang="ts">
	import { UserIcon } from "@lucide/svelte";
	import PageHeading from "../PageHeading.svelte";
	import { Avatar, Button, Card, Item, Table } from "@kayord/ui";
	import { auth } from "$lib/stores/auth.svelte";
	import { getInitials } from "$lib/util";
	import { createAccountCredential } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import TwoFactor from "./TwoFactor.svelte";

	const query = createAccountCredential();
</script>

<div class="m-4 flex flex-col gap-4">
	<PageHeading title="Profile" description="Manage your user profile." icon={UserIcon} />

	<Card.Root>
		<Card.Header class="flex items-center gap-2">
			<Avatar.Root>
				<Avatar.Image src={auth.user?.profile.picture} alt="profile" />
				<Avatar.Fallback class="bg-primary text-primary-foreground">
					{getInitials(`${auth.user?.profile.name}`)}
				</Avatar.Fallback>
			</Avatar.Root>
			<div class="flex w-full justify-between">
				<div class="flex flex-col justify-center">
					<Card.Title>Profile Information</Card.Title>
					<Card.Description>Current information of logged in user</Card.Description>
				</div>
				<Button onclick={() => auth.keycloakAction("UPDATE_PROFILE")} variant="outline">
					Update Profile
				</Button>
			</div>
		</Card.Header>
		<Card.Content>
			<Table.Root class="bg-muted/40 overflow-hidden rounded-md">
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

	<Item.Root variant="muted">
		<Item.Content>
			<Item.Title>Password</Item.Title>
			<Item.Description>Change current password</Item.Description>
		</Item.Content>
		<Item.Actions>
			<Button onclick={() => auth.keycloakAction("UPDATE_PASSWORD")} variant="outline">
				Change Password
			</Button>
		</Item.Actions>
	</Item.Root>

	<Query {query} emptyText="No data found">
		<TwoFactor
			isTwoFactorEnabled={query.data?.isTwoFactorEnabled ?? false}
			refetch={query.refetch}
		/>
	</Query>

	<Item.Root variant="muted" class="border-destructive border-2">
		<Item.Content>
			<Item.Title>Danger Zone</Item.Title>
			<Item.Description>Irreversible and destructive actions</Item.Description>
		</Item.Content>
		<Item.Actions>
			<Button onclick={() => auth.keycloakAction("delete_account")} variant="destructive">
				Delete Account
			</Button>
		</Item.Actions>
	</Item.Root>
</div>
