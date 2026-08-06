<script lang="ts">
	import { accountCredentialDisable, accountCredentialDisableToken } from "$lib/api/remote/account.remote";
	import { createAppForm, FieldError, Form, isInvalid } from "$lib/components/Form";
	import { getError } from "$lib/types";
	import { Button, Dialog, Field, InputOTP, Item } from "@kayord/ui";
	import { CircleCheckIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { z } from "zod";

	type Props = {
		isTwoFactorEnabled: boolean;
	};

	let { isTwoFactorEnabled }: Props = $props();

	const getDisable2FAToken = async () => {
		try {
			await accountCredentialDisableToken();
		} catch (error) {
			toast.error(getError(error).message);
		}
	};

	const schema = z.object({
		token: z.string().length(6, "Token must be 6 characters long"),
	});

	let open = $state(false);

	const form = createAppForm(() => ({
		defaultValues: {
			token: "",
		},
		validators: {
			onChange: schema,
		},
		onSubmit: async ({ value }) => {
			try {
				await accountCredentialDisable({ token: value.token });
				toast.success("TOTP disabled successfully");
				open = false;
			} catch (error) {
				toast.error(getError(error).message);
			}
		},
	}));
</script>

<Item.Root variant="muted">
	<Item.Content>
		<Item.Title>Two-Factor Authentication</Item.Title>
		<Item.Description>Add an extra layer of security to your account</Item.Description>
	</Item.Content>
	<Item.Actions>
		{#if isTwoFactorEnabled}
			<CircleCheckIcon class="text-green-500 dark:text-green-300" />
			<Dialog.Root bind:open>
				<Dialog.Trigger>
					<Button variant="destructive">Disable TOTP</Button>
				</Dialog.Trigger>
				<Dialog.Content>
					<Form {form}>
						<div class="flex flex-col gap-2">
							<Dialog.Header>
								<Dialog.Title>Disable TOTP?</Dialog.Title>
								<Dialog.Description>Please enter code received from email to disable TOTP.</Dialog.Description>
							</Dialog.Header>
							<div class="flex flex-col items-center gap-2">
								<form.AppField name="token">
									{#snippet children(field)}
										<Field.Field data-invalid={isInvalid(field)}>
											<Field.Label for={field.name}>Token</Field.Label>
											<InputOTP.Root
												maxlength={6}
												id={field.name}
												value={field.state.value}
												oninput={(e) => field.handleChange((e.target as HTMLInputElement).value)}
												aria-invalid={isInvalid(field)}
												onblur={() => field.handleBlur()}
											>
												{#snippet children({ cells })}
													<InputOTP.Group>
														{#each cells as cell (cell)}
															<InputOTP.Slot {cell} />
														{/each}
													</InputOTP.Group>
												{/snippet}
											</InputOTP.Root>
											<FieldError {field} />
										</Field.Field>
									{/snippet}
								</form.AppField>
							</div>
							<Dialog.Footer class="flex justify-between">
								<Button variant="link" onclick={getDisable2FAToken}>Resend Code</Button>
								<Button variant="destructive" type="submit">Disable TOTP</Button>
							</Dialog.Footer>
						</div>
					</Form>
				</Dialog.Content>
			</Dialog.Root>
		{:else}
			<Button href="/auth/login?action=CONFIGURE_TOTP" variant="outline">Configure TOTP</Button>
		{/if}
	</Item.Actions>
</Item.Root>
