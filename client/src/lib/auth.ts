import { getContext, setContext } from "svelte";
import type { SessionUser } from "$lib/types";

const USER_KEY = Symbol("session-user");
const ROLES_KEY = Symbol("facility-roles");

export function setUserContext(user: SessionUser | undefined) {
	setContext(USER_KEY, user);
}

export function useUser(): SessionUser | undefined {
	return getContext<SessionUser | undefined>(USER_KEY);
}

export function setRolesContext(roles: string[]) {
	setContext(ROLES_KEY, roles);
}

export function useRoles(): string[] {
	return getContext<string[]>(ROLES_KEY) ?? [];
}

export function hasRoles(...roles: string[]): boolean {
	const userRoles = useRoles();
	return roles.some((r) => userRoles.includes(r));
}
