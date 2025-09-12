<script lang="ts">
	const handleLoginWithGoogle = () => {
		window.location.href =
			'http://localhost:5017/account/login/google?returnUrl=http://localhost:5173/test';
	};
</script>

<div>+page</div>
<button onclick={handleLoginWithGoogle}>Login</button>
