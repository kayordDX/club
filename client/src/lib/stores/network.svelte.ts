import { browser } from "$app/environment";

class Network {
	#isOnline = $state(false);

	public constructor() {
		const updateState = () => {
			this.#isOnline = navigator?.onLine;
		};
		$effect.root(() => {
			$effect(() => {
				if (browser) {
					window.addEventListener("online", updateState);
					window.addEventListener("offline", updateState);
				}
				return () => {
					if (browser) {
						window.removeEventListener("online", updateState);
						window.removeEventListener("offline", updateState);
					}
				};
			});
			updateState();
		});
	}

	public isOnline() {
		return this.#isOnline;
	}
}

export const networkInformation = new Network();
