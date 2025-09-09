<main
	class="flex min-h-screen items-center justify-center bg-gradient-to-br from-blue-500 via-purple-500 to-pink-500"
>
	<h1
		class="animate-bounce text-6xl font-extrabold text-white drop-shadow-lg transition-transform duration-700 ease-in-out md:text-8xl"
		style="animation: pulse 2s infinite alternate;"
	>
		Online
	</h1>
</main>

<style>
	@keyframes pulse {
		0% {
			transform: scale(1);
			text-shadow:
				0 0 20px #fff,
				0 0 40px #a78bfa;
		}
		100% {
			transform: scale(1.08) rotate(-2deg);
			text-shadow:
				0 0 40px #fff,
				0 0 80px #f472b6;
		}
	}
</style>
