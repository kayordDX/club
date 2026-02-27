import { defineConfig } from "@playwright/test";

export default defineConfig({
  webServer: [
  	{
  		// Start Postgres
  		command: "docker compose up postgres",
  		port: 5432,
  		cwd: "../",
  		timeout: 120000,
  		reuseExistingServer: true,
  	},
  	{
  		// Start Redis
  		command: "docker compose up redis",
  		port: 6379,
  		cwd: "../",
  		timeout: 120000,
  		reuseExistingServer: true,
  	},
  	{
  		// Backend API
  		command: "dotnet run --project online.csproj --no-restore",
  		port: 5000,
  		cwd: "../api/src/online",
  		timeout: 120000,
  		reuseExistingServer: true,
  	},
  	{
  		command: "pnpm run build && pnpm run preview",
  		port: 4173,
  	}
	],
	testDir: "e2e",
	use: {
		baseURL: "http://localhost:4173",
	},
});
