<script lang="ts">
	import { Button, Card, Loader } from "@kayord/ui";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import { z } from "zod";
	import { Form, createAppForm } from "$lib/components/Form";

	const schema = z.object({
		email: z.email(),
	});

	const form = createAppForm(() => ({
		defaultValues: {
			email: "",
		},
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
				<Card.Title>Forgot Password?</Card.Title>
				<Card.Description>
					Enter your email and we'll send you instructions to reset your password
				</Card.Description>
			</Card.Header>
			<Form {form}>
				<Card.Content class="flex flex-col items-center">
					<form.AppField name="email">
						{#snippet children(field)}
							<field.Input label="Email" />
						{/snippet}
					</form.AppField>
				</Card.Content>
				<Card.Footer class="flex flex-col items-center gap-2">
					<Button type="submit" disabled={isLoading} class="w-full">
						<Loader class="mr-0" {isLoading} />
						Reset Password
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
