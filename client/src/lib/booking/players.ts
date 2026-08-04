/**
 * Shared helpers for the player forms used by the slot (booking creation) and
 * booking edit pages.
 */

export type PlayerDraft = {
	name: string;
	cellNo: string;
	email: string;
	contractId: string;
};

type ProfileLike = {
	name?: string | null;
	email?: string | null;
	phone_number?: string | null;
};

/** Creates `count` empty player drafts. */
export const createPlayers = (count: number): PlayerDraft[] =>
	Array.from({ length: count }, () => ({
		name: "",
		cellNo: "",
		email: "",
		contractId: "",
	}));

/**
 * Fills a player with the signed-in user's profile (the "Me" button).
 * Mirrors the existing slot page behaviour: fields the profile doesn't have
 * are cleared to an empty string.
 */
export const applyProfileToPlayer = (player: PlayerDraft, profile?: ProfileLike | null): PlayerDraft => ({
	...player,
	name: profile?.name ?? "",
	email: profile?.email ?? "",
	cellNo: profile?.phone_number ?? "",
});

/**
 * Propagates the first player's contract to every other player whose contract
 * has not been set yet. Players that already have a contract are left untouched,
 * so the first player's selection never overrides an existing contract.
 */
export const applyFirstPlayerContract = (players: PlayerDraft[]): PlayerDraft[] => {
	const firstContractId = players[0]?.contractId;
	if (!firstContractId || players.length < 2) return players;

	return players.map((player, index) => {
		if (index === 0 || player.contractId) return player;
		return { ...player, contractId: firstContractId };
	});
};
