<script lang="ts">
	import { Button, Card, Input, Loader } from "@kayord/ui";
	import { Form } from "@kayord/ui/form";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import { z } from "zod";
	import { defaults, superForm } from "sveltekit-superforms";
	import { zod4 } from "sveltekit-superforms/adapters";

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

	const form = superForm(defaults(zod4(schema)), {
		SPA: true,
		validators: zod4(schema),
		onUpdate({ form }) {
			if (form.valid) {
				console.log(form.data);
			}
		},
	});

	const { form: formData, enhance } = form;

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
			<form method="POST" use:enhance class="flex flex-col gap-2">
				<Card.Content class="flex flex-col items-center">
					<Form.Field {form} name="email" class="w-full">
						<Form.Control>
							{#snippet children({ props })}
								<Form.Label>Email</Form.Label>
								<Input {...props} type="email" bind:value={$formData.email} />
							{/snippet}
						</Form.Control>
						<Form.FieldErrors />
					</Form.Field>
					<Form.Field {form} name="password" class="w-full">
						<Form.Control>
							{#snippet children({ props })}
								<Form.Label>Password</Form.Label>
								<Input {...props} type="password" bind:value={$formData.password} />
							{/snippet}
						</Form.Control>
						<Form.FieldErrors />
					</Form.Field>
					<Form.Field {form} name="confirmPassword" class="w-full">
						<Form.Control>
							{#snippet children({ props })}
								<Form.Label>Confirm Password</Form.Label>
								<Input {...props} type="password" bind:value={$formData.confirmPassword} />
							{/snippet}
						</Form.Control>
						<Form.FieldErrors />
					</Form.Field>
				</Card.Content>
				<Card.Footer class="flex flex-col items-center gap-2">
					<Button type="submit" disabled={isLoading} class="w-full">
						<Loader class="mr-0" {isLoading} />
						Set New Password
					</Button>
					<Button variant="link" href="/login">Back to Login</Button>
				</Card.Footer>
			</form>
		</Card.Root>
		<p class="text-muted-foreground text-xs">
			By signing in, you agree to our Terms of Service and Privacy Policy
		</p>
	</div>
</div>
