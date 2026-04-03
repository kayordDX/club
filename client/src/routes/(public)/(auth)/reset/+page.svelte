<script lang="ts">
	import { Button, Card, Loader } from "@kayord/ui";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import { z } from "zod";
	import { Form, createAppForm } from "$lib/components/Form";

	const schema = z
		.object({
			email: z.email(),
			password: z.string().min(8, "Password must be at least 8 characters long"),
			confirmPassword: z.string(),
		})
		.refine((data) => data.password === data.confirmPassword, {
			message: "Passwords don't match",
			path: ["confirmPassword"],
		});

	const form = createAppForm(() => ({
		validators: {
			onChange: schema,
		},
		onSubmit: async ({ value }) => {
			console.log(value);
		},
	}));

	let isLoading = $state(false);
</script>

<div class="flex h-screen w-full flex-col items-center">
	<div class="flex h-full max-w-xl flex-col items-center justify-center gap-6">
		<LogoButton />
		<Card.Root class="w-full">
			<Card.Header>
				<Card.Title>Reset Password</Card.Title>
				<Card.Description>
					Please enter your current password and choose a new password to update your account
					security.
				</Card.Description>
			</Card.Header>
			<Form {form}>
				<Card.Content class="flex flex-col items-center">
					<form.AppField name="email">
						{#snippet children(field)}
							<field.Input label="Email" />
						{/snippet}
					</form.AppField>
					<form.AppField name="password">
						{#snippet children(field)}
							<field.Input label="Password" type="password" />
						{/snippet}
					</form.AppField>
					<form.AppField name="confirmPassword">
						{#snippet children(field)}
							<field.Input label="Confirm Password" type="password" />
						{/snippet}
					</form.AppField>
				</Card.Content>
				<Card.Footer class="flex flex-col items-center gap-2">
					<Button type="submit" disabled={isLoading} class="w-full">
						<Loader class="mr-0" {isLoading} />
						Set New Password
					</Button>
					<Button variant="link" href="/login">Back to Login</Button>
				</Card.Footer>
			</Form>
		</Card.Root>
		<p class="text-muted-foreground text-xs">
			By signing in, you agree to our Terms of Service and Privacy Policy
		</p>
	</div>
</div>
