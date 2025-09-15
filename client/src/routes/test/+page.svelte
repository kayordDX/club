<script lang="ts">
	import { Button, ThemeSelector } from "@kayord/ui";
	import { PUBLIC_API_URL } from "$env/static/public";
	import { createRegister } from "$lib/api";

	const handleLoginWithGoogle = () => {
		window.location.href = `${PUBLIC_API_URL}/account/login/google?returnUrl=http://localhost:5173`;
	};

	const testMutation = createRegister();
	const testRegister = async () => {
		await $testMutation.mutateAsync({
			data: {
				email: "test@example.com",
				firstName: "Test",
				lastName: "User",
				password: "password123",
			},
		});
	};
</script>

<div>+page</div>
<button onclick={handleLoginWithGoogle}>Login</button>

<Button>Test</Button>
<ThemeSelector />
