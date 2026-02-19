<script lang="ts">
	import { Button, Card, Input, Loader } from "@kayord/ui";
	import { Form } from "@kayord/ui/form";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import { z } from "zod";
	import { defaults, superForm } from "sveltekit-superforms";
	import { zod4 } from "sveltekit-superforms/adapters";

	const schema = z.object({
		email: z.email(),
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
				<Card.Title>Forgot Password?</Card.Title>
				<Card.Description>
					Enter your email and we'll send you instructions to reset your password
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
				</Card.Content>
				<Card.Footer class="flex flex-col items-center gap-2">
					<Button type="submit" disabled={isLoading} class="w-full">
						<Loader class="mr-0" {isLoading} />
						Reset Password
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
