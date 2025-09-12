<script lang="ts">
	import { Button, ThemeSelector } from "@kayord/ui";
	import { createTest, createRefresh, createRegister, createGoogleLoginCallback } from "$lib/api";

	const handleLoginWithGoogle = () => {
		window.location.href =
			"http://localhost:5017/account/login/google?returnUrl=http://localhost:5173";
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
