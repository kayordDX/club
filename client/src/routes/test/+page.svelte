<script lang="ts">
	import { Button, ThemeSelector } from "@kayord/ui";
	const handleLoginWithGoogle = () => {
		window.location.href =
			"http://localhost:5017/account/login/google?returnUrl=http://localhost:5173";
	};
</script>

<div>+page</div>
<button onclick={handleLoginWithGoogle}>Login</button>

<Button>Test</Button>
<ThemeSelector />
