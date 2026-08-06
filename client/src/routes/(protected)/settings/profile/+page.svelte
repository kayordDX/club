<script lang="ts">
	import { UserIcon } from "@lucide/svelte";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Avatar, Button, Card, Item, Table } from "@kayord/ui";
	import { useUser } from "$lib/auth";
	import { getInitials } from "$lib/util";
	import { accountCredential } from "$lib/api/remote/account.remote";
	import TwoFactor from "./TwoFactor.svelte";

	const user = useUser();
	const credentials = await accountCredential();
</script>

<div class="m-4 flex flex-col gap-4">
	<PageHeading title="Profile" description="Manage your user profile." icon={UserIcon} />

	<Card.Root>
		<Card.Header class="flex items-center gap-2">
			<Avatar.Root>
				<Avatar.Image src={user?.picture} alt="profile" />
				<Avatar.Fallback class="bg-primary text-primary-foreground">
					{getInitials(`${user?.name}`)}
				</Avatar.Fallback>
			</Avatar.Root>
			<div class="flex w-full justify-between">
				<div class="flex flex-col justify-center">
					<Card.Title>Profile Information</Card.Title>
					<Card.Description>Current information of logged in user</Card.Description>
				</div>
				<Button href="/auth/login?action=UPDATE_PROFILE" variant="outline">Update Profile</Button>
			</div>
		</Card.Header>
		<Card.Content>
			<Table.Root class="bg-muted/40 overflow-hidden rounded-md">
				<Table.Body>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Name</Table.Cell>
						<Table.Cell class="text-end">{user?.name}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">First Name</Table.Cell>
						<Table.Cell class="text-end">{user?.firstName}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Last Name</Table.Cell>
						<Table.Cell class="text-end">{user?.lastName}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Email</Table.Cell>
						<Table.Cell class="text-end">{user?.email}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Email Verified</Table.Cell>
						<Table.Cell class="text-end">{user?.email_verified ? "Yes" : "No"}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Phone Number</Table.Cell>
						<Table.Cell class="text-end">{user?.phone_number ?? "—"}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Phone Number Verified</Table.Cell>
						<Table.Cell class="text-end">{user?.phone_number_verified ? "Yes" : "No"}</Table.Cell>
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
			<Button href="/auth/login?action=UPDATE_PASSWORD" variant="outline">Change Password</Button>
		</Item.Actions>
	</Item.Root>

	<Item.Root variant="muted">
		<Item.Content>
			<Item.Title>Passkey</Item.Title>
			<Item.Description>Configure passkey for passwordless authentication</Item.Description>
		</Item.Content>
		<Item.Actions>
			<Button href="/auth/login?action=webauthn-register-passwordless" variant="outline">Configure Passkey</Button>
		</Item.Actions>
	</Item.Root>

	<TwoFactor isTwoFactorEnabled={credentials?.isTwoFactorEnabled ?? false} />

	<Item.Root variant="muted" class="border-destructive border-2">
		<Item.Content>
			<Item.Title>Danger Zone</Item.Title>
			<Item.Description>Irreversible and destructive actions</Item.Description>
		</Item.Content>
		<Item.Actions>
			<Button href="/auth/login?action=delete_account" variant="destructive">Delete Account</Button>
		</Item.Actions>
	</Item.Root>
</div>
