type PlayerProfile = {
	name?: string | null;
	given_name?: string | null;
	family_name?: string | null;
	email?: string | null;
	phone_number?: string | null;
};

type PlayerAutofillDetails = {
	name: string;
	email: string;
	cellNo: string;
};

const SELF_PLAYER_OPTION_VALUE = "me";

const normalizeValue = (value?: string | null) => value?.trim() ?? "";

const getPlayerName = (profile?: PlayerProfile | null) => {
	const name = normalizeValue(profile?.name);
	if (name) return name;

	return [normalizeValue(profile?.given_name), normalizeValue(profile?.family_name)]
		.filter(Boolean)
		.join(" ");
};

const getSelfPlayerOption = (profile?: PlayerProfile | null) => {
	const details = getSelfPlayerDetails(profile);

	if (details == null) {
		return null;
	}

	return {
		value: SELF_PLAYER_OPTION_VALUE,
		label: "Me",
	};
};

const getSelfPlayerDetails = (profile?: PlayerProfile | null): PlayerAutofillDetails | null => {
	const details = {
		name: getPlayerName(profile),
		email: normalizeValue(profile?.email),
		cellNo: normalizeValue(profile?.phone_number),
	};

	if (Object.values(details).every((value) => value.length === 0)) {
		return null;
	}

	return details;
};

export {
	SELF_PLAYER_OPTION_VALUE,
	getSelfPlayerDetails,
	getSelfPlayerOption,
	type PlayerAutofillDetails,
	type PlayerProfile,
};
