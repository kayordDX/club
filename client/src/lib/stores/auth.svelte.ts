import { UserManager, type UserManagerSettings, User } from "oidc-client-ts";
import { PUBLIC_IDENTITY_URL, PUBLIC_APP_URL, PUBLIC_API_URL } from "$env/static/public";

class Auth {
	private userManager: UserManager;

	#user = $state<User | null>(null);
	#isLoading = $state(true);
	#isSyncing = $state(false);

	constructor(settings: UserManagerSettings) {
		this.userManager = new UserManager(settings);
		this.init();
	}

	private async init() {
		try {
			if (!this.userManager) return;
			const user = await this.userManager.getUser();
			this.#user = user;
			this.setupEventListeners();
		} catch (error) {
			console.error("OIDC Initialization failed:", error);
		} finally {
			this.#isLoading = false;
		}
	}

	public async syncUser(force = false) {
		if (!this.accessToken || this.#isSyncing) return;

		this.#isSyncing = true;
		try {
			// Replace with your actual .NET API URL
			const response = await fetch(`${PUBLIC_API_URL}/account/sync`, {
				method: "POST",
				headers: {
					Authorization: `Bearer ${this.accessToken}`,
					"Content-Type": "application/json",
				},
				body: JSON.stringify({ force }),
			});

			if (!response.ok) {
				console.error("User sync failed", await response.text());
			}
		} catch (err) {
			console.error("Network error during JIT sync:", err);
		} finally {
			this.#isSyncing = false;
		}
	}

	private setupEventListeners() {
		this.userManager.events.addUserLoaded(async (user) => {
			this.#user = user;
			await this.syncUser();
		});

		this.userManager.events.addUserUnloaded(() => {
			this.#user = null;
		});

		this.userManager.events.addAccessTokenExpiring(() => {
			console.log("Token expiring soon...");
		});

		this.userManager.events.addAccessTokenExpired(() => {
			this.#user = null;
		});

		this.userManager.events.addSilentRenewError((error) => {
			console.error("Silent renew error:", error);
			// this.logout(); // Optional: force logout on renewal failure
		});
	}

	get user() {
		return this.#user;
	}
	get isLoading() {
		return this.#isLoading;
	}
	get isAuthenticated() {
		return !!this.#user && !this.#user.expired;
	}
	get accessToken() {
		return this.#user?.access_token;
	}

	login = async () => {
		await this.userManager.signinRedirect();
	};

	logout = async () => {
		console.log("logging out", this.userManager);
		await this.userManager.signoutRedirect();
	};

	async handleCallback() {
		try {
			const user = await this.userManager.signinRedirectCallback();
			this.#user = user;
			return user;
		} catch (error) {
			console.error("Callback error:", error);
			throw error;
		}
	}
}

export const auth = new Auth({
	authority: PUBLIC_IDENTITY_URL,
	client_id: "public-client",
	redirect_uri: `${PUBLIC_APP_URL}/callback`,
	post_logout_redirect_uri: `${PUBLIC_APP_URL}/`,
	response_type: "code",
	scope: "openid profile email offline_access",
	automaticSilentRenew: true,
	prompt: "select_account",
});
